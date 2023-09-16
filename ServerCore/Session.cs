using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    // 패킷전송용 Session을 따라 구분해서 만들거임
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // override로 패킷용 기능 추가
        // sealed = 다른 클래스가 PacketSession을 상속받아 또 OnRecv를
        // override를 하는걸 막아주는 기능임
        // [size(2)][packetId(2)][.....][size(2)][packetId(2)][.....]....
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            // 몇바이트를 처리했는지 체크할거임
            int processLen = 0;

            while (true)
            {
                //최소한 header는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                //패킷이 완전체로 도착하는지 확인 (Uint16 크기만큼만 긁어서 dataSize를 만들어줌)
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                // 부분적으로만 옴
                if (buffer.Count < dataSize) 
                    break;

                //여기까지 왔으면 패킷 조립 가능 (다 도착한거임)
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte> (buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return 0;
        }

        public abstract void OnRecvPacket(ArraySegment<byte>buffer);
    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;  //flag 기능

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);    
        public abstract void OnDisconnected(EndPoint endPoint) ;

        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);


            RegisterRecv();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            //하나의 session이 Disconnect를 두번 연달아 보낸다면 문제생기므로
            //멀티스레드 환경에서 가능하도록 lock의 exchange를 통하여 멀티스레드 환경에서 
            //Disconnect함수가 한번에 두번들어와도 한명은 그냥return 시켜주도록 만듬
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            // 추상메서드인 OnDisconnected 로 들어가게 되어서 Session을 파생하여 사용하는 다른 클래스들도 Disconnect 되는 순간을 알아 차리고 사용 가능
            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }
            _sendArgs.BufferList = _pendingList;

            // 최종적으로 Send하는 부분
            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);


                        // sendQeue를 비우는 와중에 누군가 추가할 수도 있으므로
                        if (_sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnRecvCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }


        void RegisterRecv()
        {
            _recvBuffer.Clean(); // 혹시라도 커서가 넘어가는걸 방지
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnRecvCompleted(null, _recvArgs);
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 연결이 끊겼을때 0바이트로 전송이 오기도 함
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    //Write 커서 이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        // 만약 버그가 있어서 바이트를 받지 못했을때
                        Disconnect() ;
                    }
                    // 콘텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        // 일단 패킷을 다 처리하지 못했다고 return 오면 에러
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        // 마지막으로 Read커서 이동하고 처리 완료했는데 버퍼에서 나오는 에러처리
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }
        #endregion 
    }
}
