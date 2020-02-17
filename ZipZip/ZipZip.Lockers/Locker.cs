using System;
using System.Threading;

namespace ZipZip.Lockers
{
    public class Locker
    {
        private readonly object _lockObject = new object();

        public IDisposable Lock()
        {
            return new LockerRelease(_lockObject);
        }

        private class LockerRelease : IDisposable
        {
            private readonly object _lockObject;

            public LockerRelease(object lockObject)
            {
                _lockObject = lockObject;
                Monitor.Enter(_lockObject);
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            private void ReleaseUnmanagedResources()
            {
                Monitor.Exit(_lockObject);
            }

            ~LockerRelease()
            {
                ReleaseUnmanagedResources();
            }
        }
    }
}