using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZipZip.Threading.PrimitiveThreadLockers;

namespace ZipZip.Workers.DataBuffer
{
    /// <summary>
    ///     This is a collection of waiters which can be used to release any or specific frozen thread
    /// </summary>
    internal partial class WaitersCollection
    {
        private readonly Dictionary<int, ManualResetEvent> _dictionary = new Dictionary<int, ManualResetEvent>();
        private readonly ThreadLocker _threadLocker = new ThreadLocker();
        
        /// <summary>
        ///     Negative index is used for waiters, who's item's order does not matter.
        ///     For example, we can pull items from input buffer in any order.
        /// </summary>
        private int _negativeIndex = -1;

        /// <summary>
        ///     Is called when there is need to stop worker threads
        /// </summary>
        public bool AbortAllThreads { get; private set; }

        public void AbortWaiters()
        {
            AbortAllThreads = true;
            foreach (KeyValuePair<int, ManualResetEvent> manualResetEventItem in _dictionary)
                manualResetEventItem.Value.Set();
        }

        /// <summary>
        ///     Can be used to define the region where collection will not be changed from other threads
        /// </summary>
        public ThreadLocker.LockerRelease LockOtherThreadsAccessingThisCollection()
        {
            return _threadLocker.Lock();
        }

        
        /// <summary>
        ///     Create a waiter, so it can wait. Later, someone, who access buffer from another side can release this waiter
        /// </summary>
        /// <param name="order">Null if order does not matter</param>
        private void CreateWaiter(int? order, ManualResetEvent manualResetEvent)
        {
            using (_threadLocker.Lock())
            {
                _dictionary.Add(order ?? _negativeIndex--, manualResetEvent);
            }
        }

        /// <summary>
        ///     Release specific thread (who is waiting for specific data of specific order) or any thread
        /// </summary>
        private void ReleaseWaiter(int? order)
        {
            using (_threadLocker.Lock())
            {
                if (order != null)
                {
                    bool tryGetValue = _dictionary.TryGetValue((int) order, out ManualResetEvent result);
                    if (!tryGetValue) return;
                    _dictionary.Remove((int) order);
                    result.Set();
                    return;
                }

                if (_dictionary.Count == 0) return;

                int firstKey = _dictionary.Keys.Min();

                ManualResetEvent manualResetEvent = _dictionary[firstKey];

                _dictionary.Remove(firstKey);

                manualResetEvent.Set();
            }
        }
    }
}