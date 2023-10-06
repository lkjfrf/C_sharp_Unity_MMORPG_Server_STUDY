using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    class PacketFormat
    {
        //{0} 패킷이름
        //{1} 멤버 변수들
        //{2} 멤버변수의 Read
        //{3} 멤버변수의 Write
        public static string packetFormat =
@"
class {0}
{{
    {1}

    public struct SkillInfo
    {{
        public int id;
        public short level;
        public float duration;

        public bool Write(Span<byte> s, ref ushort count)
        {{
            bool success = true;
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), id);
            count += sizeof(int);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), level);
            count += sizeof(short);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), duration);
            count += sizeof(float);
            return success;
        }}

        public void Read(ReadOnlySpan<byte> s, ref ushort count)
        {{
            id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
            count += sizeof(int);
            level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
            count += sizeof(short);
            duration = BitConverter.ToSingle (s.Slice(count, s.Length - count));
            count += sizeof(float);
        }}
    }}

    public void Read(ArraySegment<byte> segment)
    {{
        ushort count = 0;
            
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        {2}
    }}

    public ArraySegment<byte> Write()
    {{
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte> (segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), (ushort)PacketID.{0});
        count += sizeof(ushort);
        
        {3}

        // size는 가장 마지막에 알게 되므로 마지막에 추가
        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }}
}}
";
        //{0} 변수 형식
        //{1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";


        //{0} 변수 이름
        //{1} TO~ 변수형식
        //{2} 변수 형식
        public static string readFormat =
@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});";

        //{0} 변수 이름
        public static string readStringFormat =
@"
 ushort {0}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            this.{0}= Encoding.Unicode.GetString(s.Slice(count, {0}Len));
            count += {0}Len;
";

        //{0} 변수 이름
        //{1} 변수 형식
        public static string writeFormat =
@"
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), this.{0});
            count += sizeof({1});  
";

        //{0} 변수 이름
        public static string writeStringFromat=
@"
 ushort {0}Len = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, segment.Array, 
                segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Len);
            count += sizeof(ushort);
            count += {0}Len;
";


        //{0} 리스트 이름 [대문자]
        //{1} 리스트 이름 [소문자]
        //{2} 멤버 변수들
        //{3} 멤버변수의 Read
        //{4} 멤버변수의 Write
        public static string memberListFormat =
@"
 public struct {0}
{{
    {2}

    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        {3}
    }}

    public bool Write(Span<byte> s, ref ushort count)
    {{
        bool success = true;
        {4}
        return success;
    }}
}}

public List<{0}> {1}s = new List<{0}>();
";

        //{0} 리스트 이름을 [대문자]
        //{1} 리스트 이름을 [소문자]
        public static string readListFormat =
@"
this.{1}s.Clear();
ushort {1}Len = BitConverter.ToUInt16 (s.Slice(count, s.Length - count));
count += sizeof(ushort);
for (int i = 0;i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}
";

        //{0} 리스트 이름을 [대문자]
        //{1} 리스트 이름을 [소문자]
        public static string writeListFormat =
@"
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length- count), (ushort)this.{1}s.Count);
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s)
    success &= {1}.Write(s, ref count);
";
    }
}
