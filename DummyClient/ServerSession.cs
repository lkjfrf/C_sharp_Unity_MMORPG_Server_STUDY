using ServerCore;
using System.Net;
using System.Text;

namespace DummyClient
{
    class PlayerInfoReq
    {
        public long playerId;
        public string name;

        public struct SkillInfo
        {
            public int id;
            public short level;
            public float duration;

            public bool Write(Span<byte> s, ref ushort count)
            {
                bool success = true;
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), id);
                count += sizeof(int);
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), level);
                count += sizeof(short);
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), duration);
                count += sizeof(float);
                return true;
            }

            public void Read(ReadOnlySpan<byte> s, ref ushort count)
            {
                id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
                count += sizeof(int);
                level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
                count += sizeof(short);
                duration = BitConverter.ToSingle (s.Slice(count, s.Length - count));
                count += sizeof(float);
            }
        }

        public List<SkillInfo>skills = new List<SkillInfo>();

        public void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            
            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
            count += sizeof(ushort);
            count += sizeof(ushort);

            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            count += sizeof(long);

            ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
            count += nameLen;

            // Skill List
            ushort skillLen = BitConverter.ToUInt16 (s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            for (int i = 0;i < skillLen; i++)
            {
                SkillInfo skill = new SkillInfo();
                skill.Read(s, ref count);
                skills.Add(skill);
            }

        }

        public ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            ushort count = 0;
            bool success = true;

            Span<byte> s = new Span<byte> (segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort);
            // Slice, Span 둘다 가능한데 Slice로 한번 바꿔봄
            // Span(start, Lenght) => 반환값 slice된 Span 
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), (ushort)PacketID.PlayerInfoReq);
            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), this.playerId);
            count += sizeof(long);  
           

            // Name의 사이즈랑 Name 보내기
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, 
                segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
            count += sizeof(ushort);
            count += nameLen;

            // Skill List
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), (ushort)skills.Count);
            count += sizeof(ushort);
            foreach (SkillInfo skill in skills)
                success &= skill.Write(s, ref count);


            // size는 가장 마지막에 알게 되므로 마지막에 추가
            success &= BitConverter.TryWriteBytes(s, count);

            if (success == false)
                return null;

              return SendBufferHelper.Close(count);
        }
    }

    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,
    }

    class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Transferred bytes: {endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name = "test" };
            packet.skills.Add(new PlayerInfoReq.SkillInfo()
            {
                id = 101, 
                duration = 3.0f,
                level = 1
            });
            packet.skills.Add(new PlayerInfoReq.SkillInfo()
            {
                id = 102,
                duration = 3.0f,
                level = 1
            });
            packet.skills.Add(new PlayerInfoReq.SkillInfo()
            {
                id = 103,
                duration = 3.0f,
                level = 1
            });
            packet.skills.Add(new PlayerInfoReq.SkillInfo()
            {
                id = 104,
                duration = 3.0f,
                level = 1
            });

            //보낸다
            //for (int i = 0; i<5; i++)
            {
                ArraySegment<byte> s = packet.Write();
                if (s != null)
                    Send(s);

            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"Transferred bytes: {endPoint}");

        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvData}");

            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");

        }
    }
}
