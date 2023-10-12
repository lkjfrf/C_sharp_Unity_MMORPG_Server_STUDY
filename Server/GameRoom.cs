using System;
using System.Collections.Generic;
using System.Text;
using Server;
using ServerCore;

namespace Server
{
    class GameRoom : IJobQueue
    {
        List<ClientSession> _sessions = new List<ClientSession>();
        JobQueue _jobQueue = new JobQueue();

        // 할일들의 주문서들을 JobQueue 안에 집어넣고 순차적으로 pop해서 처리
        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Broadcast(ClientSession session, string chat)
        {
            // 이때는 그냥 새로운 packet 만드는 부분이니까 멀티스레드 신경 X
            S_Chat packet = new S_Chat();
            packet.playerId = session.SessionId;
            packet.chat = $"{chat} I am {packet.playerId}";
            ArraySegment<byte> segment = packet.Write();

            // N^2 상황임
            foreach (ClientSession s in _sessions)
                s.Send(segment);
        }
        public void Enter(ClientSession session)
        {
                _sessions.Add(session);
                session.Room = this;
        }

        public void Leave(ClientSession session)
        {
                _sessions.Remove(session);
        }

    }
}