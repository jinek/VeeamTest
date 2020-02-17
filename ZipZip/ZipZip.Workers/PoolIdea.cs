using System;
using System.Threading;

namespace ZipZip.Workers
{
    internal static class PoolIdea
    {
        public static void ThreadSafeAccessToPool(WaitersCollection.AccessWaiter accessWaiter,
            Func<(bool shouldWait,bool releaseWaiter)> waitCondition,
            WaitersCollection waitersToWaitWaitersCollection,
            WaitersCollection waitersToReleaseWaitersCollection
        )
        {
            bool waitConditionWait = true;
            
            while (waitConditionWait)
            {
                bool shouldWait;
                bool releaseWaiter;

                WaitHandle manualResetEvent=null;
                
                using (waitersToWaitWaitersCollection.LockOtherThreadsAccessingThisCollection())
                {
                    (shouldWait, releaseWaiter) = waitCondition();
                    
                    waitConditionWait = shouldWait;

                    if (shouldWait)
                    {
                        manualResetEvent = accessWaiter.CreateWaiter(waitersToWaitWaitersCollection);
                    }
                }

                if (releaseWaiter)
                {
                    accessWaiter.ReleaseWaiter(waitersToReleaseWaitersCollection);
                }

                manualResetEvent?.WaitOne();
            }
        }
    }
}