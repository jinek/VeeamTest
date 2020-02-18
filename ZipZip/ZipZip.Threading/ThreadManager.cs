using System;
using System.Collections.Generic;
using System.Threading;

namespace ZipZip.Threading
{
    /// <summary>
    ///     Not thread-safe
    /// </summary>
    public class ThreadManager
    {
        private readonly List<Thread> _threads = new List<Thread>();

        public void WaitAllToFinish()
        {
            foreach (Thread thread in _threads)
                try
                {
                    thread.Join();
                }
                catch (ThreadInterruptedException)
                {
                    throw new InvalidOperationException(
                        "Currently we allow to dispose manager only after everything is has finished");
                }
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
    }
}