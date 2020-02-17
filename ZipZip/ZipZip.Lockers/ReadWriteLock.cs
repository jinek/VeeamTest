using System;
using System.Threading;

namespace ZipZip.Lockers
{
    /// <summary>
    ///     https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock
    /// </summary>
    public class ReadWriteLock
    {
        private readonly Locker _r = new Locker();
        private volatile uint b;
        private SemaphoreSlim _g = new SemaphoreSlim(1, 1);

        public IDisposable ReadLock()
        {
            return new ReadLocker(this);
        }

        public IDisposable WriteLock()
        {
            //return _g.Lock();
            return new WriteLocker(this);
        }

        private class WriteLocker : IDisposable
        {//topo: эти disposable надо сделать структурой, что бы сборщк мусора не работал
            private readonly ReadWriteLock _parentReadWriteLock;

            public WriteLocker(ReadWriteLock parentReadWriteLock)
            {
                _parentReadWriteLock = parentReadWriteLock;
                _parentReadWriteLock._g.Wait();
            }

            private void ReleaseUnmanagedResources()
            {

                _parentReadWriteLock._g.Release();
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~WriteLocker()
            {
                ReleaseUnmanagedResources();
            }
        }

        private class ReadLocker : IDisposable
        {
            private readonly ReadWriteLock _parentLock;
            

            public ReadLocker(ReadWriteLock parentLock)
            {
                _parentLock = parentLock;
                using (_parentLock._r.Lock())
                {
                    if (++_parentLock.b == 1)
                    {

                        _parentLock._g.Wait();
                        //_parentLock._lockedG = _parentLock._g.Lock();
                    }
                }
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            private void ReleaseUnmanagedResources()
            {
                using (_parentLock._r.Lock())
                {
                    if (--_parentLock.b == 0)
                    {
                        _parentLock._g.Release();
                        
                        //_parentLock._lockedG.Dispose();
                    }
                }
            }

            ~ReadLocker()
            {
                ReleaseUnmanagedResources();
            }
        }
    }
}