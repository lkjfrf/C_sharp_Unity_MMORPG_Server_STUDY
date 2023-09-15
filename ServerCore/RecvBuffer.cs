using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class RecvBuffer
    {
        // [R][][][W][][][][][][] 10 byte 커서라고 생각
        ArraySegment<byte> _buffer;
        int _readPos;
        int _writePos;
        
        public RecvBuffer(int bufferSize) 
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize],0 ,bufferSize);
        }

        //현재 데이터의 크기
        public int DataSize { get { return _writePos - _readPos; } }

        //유효범위 
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        //현재까지 받은 데이터의 위치가 어디부터 어디까지 유효한지 => R ~ W 의 배열값 참조
        public ArraySegment<byte> ReadSegment
        {
            // 버퍼의 시작위치 / ArraySegment 가 참조했던 위치 + readPos = 실제메모리 상의 R  위치 / 데이터크기
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        // buffer에 다음 패킷이 도착했을때 어디부터 어디까지가 유효한지 => W~끝까지
        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        // 정리를 계속 한번씩 해줘야 버퍼가 안부족함
        // 1. [R][][][][][W][][][][] 5바이트 도착함
        // 2. [][][][][][RW][][][][] 5바이트 처리함
        // 3. [RW][][][][][][][][][] 정리한번해서 RW위치 땡겨줌
        public void Clean()
        {
            int dataSize = DataSize;
            
            if (dataSize == 0)
            {
                //RW가 같이 있는상태 데이터 사이즈 0 그럼 그냥 복사 없이 커서위치만 리셋
                _readPos = _writePos = 0;
            }else
            {
                //남은 찌꺼기가 있으면 시작 위치로 복사
                //복사할 Array 주소  / R의 실제위치  / 보낼 Array 주소 / 보낼 실제위치 / 보낼 크기
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

       // 서버가 버퍼를 읽고 처리를 완료했을때 -> R의 위치가 바뀔때
       public bool OnRead(int numOfBytes)
       {
            if (numOfBytes > DataSize)
                return false;   // 비정상적인 상황
            _readPos += numOfBytes;
            return true;
       }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;
            _writePos += numOfBytes;
            return true;
        }
    }
}
