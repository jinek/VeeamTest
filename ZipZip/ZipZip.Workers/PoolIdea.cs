using System;
using System.Diagnostics;
using System.Threading;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal static class PoolIdea
    {
        public static void ThreadSafeAccessToPool(bool waitThreadOrderMatters,
            bool releaseThreadOrderMatters,
            int order,//todo: эти три параметра потом переделать в один. Возможно стоит инициализировать инстанс через статичные методы передавая order когда нужно.Кстати, можно что б класс сам из коллекции вытаскивал.. 
            ReadWriteLock readLockForWaiting,
            Func<(bool shouldWait,bool releaseWaiter)> waitCondition,
            Locker additionalModifyWaitersLock,
            CollectionResetEvents waitersToWaitCollection,
            ReadWriteLock writeLockForReleasing,
            CollectionResetEvents waitersToReleaseCollection//todo: вместо этого можно сделать func, которая будет возвращать нужный ManualResetEvent
        )
        {
            //тут выполняем проверку можно ли сделать определённое действие с пулом и если нельзя, то усыпляем текущий поток, помещая его в  список ожидания        

            bool waitConditionWait = true;
            
            while (waitConditionWait)
            {
                (bool shouldWait, bool releaseWaiter) = waitCondition();

                waitConditionWait = shouldWait;

                ManualResetEvent manualResetEvent=null;
                
                if (shouldWait)
                {
                    manualResetEvent = new ManualResetEvent(false);
                    using (readLockForWaiting.WriteLock()) //todo: в итоге толдько и нужен WriteLock
                    {
                        using (additionalModifyWaitersLock.Lock()) //todo: этот лок вроде не нужен получается
                        {
                            waitersToWaitCollection.Enqueue(waitThreadOrderMatters ? order : (int?) null,
                                manualResetEvent);
                        }
                    }
                }

                if (releaseWaiter)
                {
                    using (writeLockForReleasing.WriteLock())
                    {
                        ManualResetEvent manualResetEventWaiter =
                            waitersToReleaseCollection.DequeueOrNull(releaseThreadOrderMatters ? order : (int?) null);
                        manualResetEventWaiter?.Set();
                    }
                }

                manualResetEvent?.WaitOne();
            }
        }
    }
}