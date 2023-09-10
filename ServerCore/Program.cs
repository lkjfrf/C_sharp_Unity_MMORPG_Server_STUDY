using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ServerCore
{
    class Program
    {
        static void Main(string[] args)
        {
            //DNS (Domain Name System)
            //www.songsonge.com -> 123.123.123.12 이런 식으로 나중에
            //관리할때 IP가 바뀌어도 도매인이 남아있어서 관리 쉬움
            string host = Dns.GetHostName(); // 로컬PC의 호스트 이름
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            //구글같은 사이트는 트래픽관리를 위해 여러 주소가 필요함
            //우리는 그중 첫번째 주소만 쓸거임
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            //문지기 = listenSocket 만들기
            //AddressFamily = ipv4, ipv6 쓸건지에 대한건데 endPoint가 알아서 만들어줌
            //TCP, UDP 선택해야 하는데 TCP쓸거라 Stream + TCP 설정 추가
            Socket listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


            try
            {
                //문지기 교육
                listenSocket.Bind(endPoint);

                //영업 시작
                //backlog : 최대 대기수 -> 10명 넘으면 bind가 fail뜸
                listenSocket.Listen(10);

                //무한루프로 계속 손님받음
                while (true)
                {
                    Console.WriteLine("Listening ...");

                    //1. 손님을 입장시킵니다
                    Socket clientSocket = listenSocket.Accept();

                    //2. 받는다
                    byte[] recvBuff = new byte[1024];
                    int recvBytes = clientSocket.Receive(recvBuff);
                    //(변환할 버퍼, 문자열받기 시작할 위치, 버퍼길이)
                    string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                    Console.WriteLine($"[From Client] {recvData}");

                    //3. 보낸다
                    byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                    clientSocket.Send(sendBuff);

                    //4. 쫒아낸다
                    //(더이상 듣기싫고 말하기도 싫다)
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}