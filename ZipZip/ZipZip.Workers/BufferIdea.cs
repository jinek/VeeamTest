using System;
using System.Threading;

namespace ZipZip.Workers
{
    internal static class BufferIdea
    {
        public static void ThreadSafeAccessToBuffer(in WaitersCollection.WaitersMode waitersMode,
            Func<(bool shouldWait,bool releaseWaiter)> waitCondition,
            WaitersCollection waitersToWaitWaitersCollection,
            WaitersCollection waitersToReleaseWaitersCollection
        )
        {
            bool waitConditionLastResult = true;
            
            while (waitConditionLastResult)
            {
                bool releaseWaiter;
                WaitHandle manualResetEvent=null;
                
                using (waitersToWaitWaitersCollection.LockOtherThreadsAccessingThisCollection())
                {
                    bool shouldWait;
                    (shouldWait, releaseWaiter) = waitCondition();
                    
                    waitConditionLastResult = shouldWait;

                    if (shouldWait)
                    {
                        manualResetEvent = waitersMode.CreateWaiter(waitersToWaitWaitersCollection);
                    }
                }

                if (releaseWaiter)
                {
                    waitersMode.ReleaseWaiter(waitersToReleaseWaitersCollection);
                }

                manualResetEvent?.WaitOne();
            }
        }
    }
}