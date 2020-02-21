using System;
using System.Threading;

namespace ZipZip.Workers.DataBuffer
{
    internal static class BufferIdea
    {
        /// <summary>
        ///     This method receives two collections: <paramref name="addToThisCollectionWhenWaiting" /> and
        ///     <paramref name="releaseFromThisCollection" />
        ///     This two collections contain wait handlers which can be used to release waiting thread.
        ///     Also we can use this collections to declare new wait handler in case we see caller thead needs to wait
        ///     Same time <paramref name="waitCondition" /> defines: whether we need to wait and whether we can release a specific
        ///     or any thread
        /// </summary>
        /// <param name="waitersMode">See type <see cref="WaitersCollection.WaitersMode" /></param>
        /// <exception cref="ProcessingFinishedException">
        ///     This exception will be called, if any if waiters collections are in state
        ///     of abortion
        /// </exception>
        public static void ThreadSafeAccessToBuffer(in WaitersCollection.WaitersMode waitersMode,
            Func<(bool shouldWait, bool releaseWaiter)> waitCondition,
            WaitersCollection addToThisCollectionWhenWaiting,
            WaitersCollection releaseFromThisCollection
        )
        {
            bool waitConditionLastResult = true;

            while (waitConditionLastResult)
            {
                bool releaseWaiter;
                WaitHandle manualResetEvent = null;

                using (addToThisCollectionWhenWaiting.LockOtherThreadsAccessingThisCollection())
                {
                    bool shouldWait;
                    (shouldWait, releaseWaiter) = waitCondition();

                    waitConditionLastResult = shouldWait;

                    if (shouldWait) manualResetEvent = waitersMode.CreateWaiter(addToThisCollectionWhenWaiting);
                }

                if (releaseWaiter) waitersMode.ReleaseWaiter(releaseFromThisCollection);

                try
                {
                    manualResetEvent?.WaitOne();
                }
                catch (ThreadInterruptedException)
                {
                    throw new ProcessingFinishedException();
                }

                if (addToThisCollectionWhenWaiting.AbortAllThreads || releaseFromThisCollection.AbortAllThreads)
                    throw new ProcessingFinishedException();
            }
        }
    }
}