using System;
using System.Collections.Generic;
using System.Threading;

namespace ZipZip.Lockers
{
    public class ThreadManager : IDisposable
    {
        private readonly List<Thread> _threads = new List<Thread>();

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void RunThread(Action action)
        {
            var thread = new Thread(o => action())
            {
                IsBackground = true
            };
            _threads.Add(thread);
            thread.Start();
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (Thread thread in _threads)
            {
                try
                {
                    thread.Abort();
                }
                catch (ThreadStateException)
                {
                    throw new NotImplementedException();
                }

                try
                {
                    thread.Join();
                }
                catch (ThreadInterruptedException)
                {
                    throw new NotImplementedException();
                }
            }
        }

        ~ThreadManager()
        {
            ReleaseUnmanagedResources();
        }
    }
}