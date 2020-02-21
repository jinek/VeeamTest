using System;
using System.Collections.Generic;
using System.Threading;

namespace ZipZip.Threading
{
    /// <summary>
    ///     Not thread-safe
    /// </summary>
    public class ThreadManager<TProcessAbortedException> where TProcessAbortedException : Exception
    {
        private readonly List<Thread> _threads = new List<Thread>();

        public void WaitAllToFinish()
        {
            foreach (Thread thread in _threads)
                try
                {
                    //not effective solution, but is done locally, no affect to the rest of the code
                    while(!thread.Join(1000))
                        thread.Interrupt();
                }
                catch (ThreadInterruptedException)
                {
                    throw new InvalidOperationException(
                        "Currently we allow to dispose manager only after everything is has finished");
                }
        }

        public void RunThread(Action action)
        {
            var thread = new Thread(o =>
            {
                try
                {
                    action();
                }
                catch (TProcessAbortedException)
                {
                    //similar to ThreadAbortException
                }
            })
            {
                IsBackground = true
            };
            _threads.Add(thread);
            thread.Start();
        }
    }
}