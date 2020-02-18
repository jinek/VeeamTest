using System;
using System.Threading;

namespace ZipZip.Threading
{
    /// <summary>
    ///     https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock
    /// </summary>
    public class ReadWriteThreadLocker
    {
        private readonly SemaphoreSlim _g = new SemaphoreSlim(1, 1);
        private readonly ThreadLocker _r = new ThreadLocker();
        private volatile uint _b;

        public ReadLocker ReadLock()
        {
            return new ReadLocker(this);
        }

        public WriteLocker WriteLock()
        {
            return new WriteLocker(this);
        }

        public struct WriteLocker : IDisposable
        {
            private readonly ReadWriteThreadLocker _parentReadWriteThreadLocker;

            internal WriteLocker(ReadWriteThreadLocker parentReadWriteThreadLocker)
            {
                _parentReadWriteThreadLocker = parentReadWriteThreadLocker;
                _parentReadWriteThreadLocker._g.Wait();
            }

            public void Dispose()
            {
                _parentReadWriteThreadLocker._g.Release();
            }
        }

        public struct ReadLocker : IDisposable
        {
            private readonly ReadWriteThreadLocker _parentThreadLocker;

            internal ReadLocker(ReadWriteThreadLocker parentThreadLocker)
            {
                _parentThreadLocker = parentThreadLocker;
                using (_parentThreadLocker._r.Lock())
                {
                    if (++_parentThreadLocker._b == 1) _parentThreadLocker._g.Wait();
                }
            }

            public void Dispose()
            {
                using (_parentThreadLocker._r.Lock())
                {
                    if (--_parentThreadLocker._b == 0) _parentThreadLocker._g.Release();
                }
            }
        }
    }
}