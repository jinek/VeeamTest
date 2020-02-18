using System;
using System.Threading;

namespace ZipZip.Threading.PrimitiveThreadLockers
{
    public class ThreadLocker
    {
        private readonly object _lockObject = new object();

        public LockerRelease Lock()
        {
            return new LockerRelease(_lockObject);
        }

        public struct LockerRelease : IDisposable
        {
            private readonly object _lockObject;

            internal LockerRelease(object lockObject)
            {
                _lockObject = lockObject;
                Monitor.Enter(_lockObject);
            }

            public void Dispose()
            {
                Monitor.Exit(_lockObject);
            }
        }
    }
}