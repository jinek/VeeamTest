using System;

namespace ZipZip.Lockers
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock
    /// </summary>
    public class ReadWriteLock
    {
        public IDisposable ReadLock()
        {
            throw new NotImplementedException();
        }

        public IDisposable WriteLock()
        {
            throw new NotImplementedException();
        }
    }
}