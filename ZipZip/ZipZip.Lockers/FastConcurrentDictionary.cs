using System.Threading;

namespace ZipZip.Lockers
{
    public class FastConcurrentDictionary<TKey,TValue>
    {
        private readonly ReaderWriterLock _bagLock = new ReaderWriterLock();
    }
}