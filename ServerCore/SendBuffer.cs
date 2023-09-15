using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class SendBufferHelper
    {
        // ThreadLocal = 나의 스레드에서만 고유하게 사용가능한 전역변수
        public static ThreadLocal<SendBuffer>  CurrentBuffer = new ThreadLocal<SendBuffer>(()=> { return null; });

        // 임시용 큰 버퍼 사이즈라 (chunk라고 이름지음)
        public static int ChunkSize { get; set; } = 4096 * 100;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if(CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    // SendBuffer는 Recv와 다르게 사용자마다 (스레드마다) 패킷을 아직 받았을 수도 있고
    // 못 받았을 수도 있기 때문에 SendBuffer을 Clean 하여 재활용하기 어려움.. 1회용임
    public class SendBuffer
    {
        // [U][][][][][][][][][]
        byte[] _buffer;
        int _usedSize = 0;

        public int FreeSize{get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        //처음 SendBuffer을 가져와서 데이터 넣어주기 위해 가져오는 Segment
        public ArraySegment<byte> Open(int reserveSize)
        {
            if (reserveSize > FreeSize)
                return null;

            //작업 요청할 영역
            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        //Open에서 준 SendBuffer에서 데이터를 다 넣은 후 실제 사용한 양을 받으면
        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            // 실제 사용한 양만큼의 segment를 주어서 SendBuffer을 사용하도록 해줌
            return segment;
        }
    }
}
