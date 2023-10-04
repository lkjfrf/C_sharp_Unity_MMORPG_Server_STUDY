using System;
using System.Xml;

namespace PacketGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlReaderSettings settings = new XmlReaderSettings()
            {
                //주석무시
                IgnoreComments = true,
                //스페이스바 무시
                IgnoreWhitespace = true
            };

            using (XmlReader r = XmlReader.Create("PDL.xml", settings))
            {
                r.MoveToContent();

                while (r.Read())
                {
                    if (r.Depth== 1)
                        ParsePacket(r);
                    Console.WriteLine(r.Name + " " + r["name"]);
                }

            }
        }
        public static void ParsePacket(XmlReader r)
        {

        }
    }
}