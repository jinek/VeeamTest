using System;
using System.Threading;

namespace ZipZip.Workers
{
    internal static class PoolIdea
    {
        public static void ThreadSafeAccessToPool(in WaitersCollection.AccessWaiter accessWaiter,
            Func<(bool shouldWait,bool releaseWaiter)> waitCondition,
            WaitersCollection waitersToWaitWaitersCollection,
            WaitersCollection waitersToReleaseWaitersCollection
        )
        {
            bool waitConditionWait = true;
            
            while (waitConditionWait)
            {
                bool releaseWaiter;
                WaitHandle manualResetEvent=null;
                
                using (waitersToWaitWaitersCollection.LockOtherThreadsAccessingThisCollection())
                {
                    bool shouldWait;
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