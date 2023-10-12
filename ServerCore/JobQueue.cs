using System;
using System.Collections.Generic;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        Queue<Action> _jobQueue = new Queue<Action>();
        object _lock = new object();
        bool _flush = false;

        public void Push(Action job)
        {
            //Lock을 풀고 프로그램 실행중에 다른 스레드가 그 작업을 방해하지 않게 하기 위함
            bool flush = false;
            lock (_lock)
            {
                _jobQueue.Enqueue(job);
                if (_flush == false)
                    flush = _flush = true;

            }
                if (flush)
                    Flush();
        }
        
        void Flush()
        {
            while (true)
            {
                Action action = Pop();
                if (action == null)
                    return;

                //  Invoke로 실행하면 멀트스레드 환경에서 어느 스레드가 이 action.Invoke 함수를 실행시킨다고
                //  하더라도 action을 소유한 스레드에게 함수호출을 위임해서 실행하므로 충돌나지 않는다
                action.Invoke();
            }
        }

        Action Pop()
        {
            // Pop 할때도 지금 Push 중인 JobQueue에 접근해서 비워주므로 락을 걸고 접근해야함
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                {
                    _flush = false;
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}
