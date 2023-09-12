using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;
        /*Action<Socket> _onAcceptHandler;*/
        Func<Session> _sessionFactory;


        public void Init(IPEndPoint endPoint, Func<Session>  sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;

            //문지기 교육
            _listenSocket.Bind(endPoint);
            //영업시작 최대갯수
            _listenSocket.Listen(10);

            //SocketAsyncEventArgs 쓰는 이유는 AcceptAsync()함수를 활용하여 비동기 방식으로 처리할때
            //바로 Accept가 되지않고 넘어갔을때 나중에 Accept성공되고 이벤트로 성공된 순간에 처리를 나중에 해주기 위함
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //Completed 는 이벤트 헨들러 이므로 델리게이트를 추가해줌 
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            //최초 등록
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            // 만약 재등록 됐을때 다시 Accept 받을려면 깨끗한 상태에서 시작해야함
            args.AcceptSocket = null;

            // pending 을 뱉어줌
            bool pending = _listenSocket.AcceptAsync(args);

            // Accept가 바로 되었을때는 pending 이 false라서 바로 처리해줌
            // Accept가 바로 안되었을때는 true 라서 OnAcceptComplete가 이벤트 발생될떄 처리됨
            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                GameSession session = new GameSession();
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            //Accept 끝나서 처리가 완료되고 또 다음 Accept를 위해 또 등록
            RegisterAccept(args);
        }

        public Socket Accept() 
        {
            //Accept, Recv, Send 같은 입출력 계열의 함수는 블록킹함수 이므로
            //비동기로 변환해줘야 여러 사용자중 한명이 블록킹되어도 문제없도록 함
            _listenSocket.AcceptAsync();    //sync = 동기, Async = 비동기
            return _listenSocket.Accept();    
        }
    }
}
