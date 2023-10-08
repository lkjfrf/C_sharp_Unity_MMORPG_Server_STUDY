using System;
using System.Collections.Generic;
using System.Text;
using Server;

namespace Server
{
    class GameRoom
    {
        List<ClientSession> _sessions = new List<ClientSession>();
        object _lock = new object();

        public void Broadcast(ClientSession session, string chat)
        {
            // 이때는 그냥 새로운 packet 만드는 부분이니까 멀티스레드 신경 X
            S_Chat packet = new S_Chat();
            packet.playerId = session.SessionId;
            packet.chat = chat;
            ArraySegment<byte> segment = packet.Write();

            lock(_lock)
            {
                foreach (ClientSession s in _sessions)
                    s.Send(segment); 
            }
        }

        public void Enter(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Add(session);
                session.Room = this;
            }
        }

        public void Leave(ClientSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
            }
        }

    }
}
