using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Net.WebSockets;

namespace ServerCore
{
    class Program
    {
        static Listener _listener = new Listener(); 
        
        static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                //2. 받는다
                byte[] recvBuff = new byte[1024];
                int recvBytes = clientSocket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"[From Client] {recvData}");

                //3. 보낸다
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                clientSocket.Send(sendBuff);

                //4. 쫒아낸다
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Main(string[] args)
        {

            //DNS (Domain Name System)
            string host = Dns.GetHostName(); // 로컬PC의 호스트 이름
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            _listener.Init(endPoint, OnAcceptHandler);
            Console.WriteLine("Listening ...");

            while (true)
            {
                ;
            }
            
        }
    }
}