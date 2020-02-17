using System;
using System.Threading;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal static class PoolIdea
    {
        public static T ThreadSafeAccessToPool<T>(bool waitThreadOrderMatters,
            bool releaseThreadOrderMatters,
            int order,//todo: эти три параметра потом переделать в один. Возможно стоит инициализировать инстанс через статичные методы передавая order когда нужно.Кстати, можно что б класс сам из коллекции вытаскивал.. 
            ReadWriteLock readLockForWaiting,
            Func<bool> waitCondition,
            Locker additionalModifyWaitersLock,
            CollectionResetEvents waitersToWaitCollection,
            ReadWriteLock writeLockForReleasing,
            Func<T> returnFunction,
            CollectionResetEvents waitersToReleaseCollection//todo: вместо этого можно сделать func, которая будет возвращать нужный ManualResetEvent
        )
        {
            //тут выполняем проверку можно ли сделать определённое действие с пулом и если нельзя, то усыпляем текущий поток, помещая его в  список ожидания        
            using (readLockForWaiting.ReadLock())
            {
                while (waitCondition())
                {
                    var manualResetEvent = new ManualResetEvent(false);
                    using (additionalModifyWaitersLock.Lock())
                    {
                        waitersToWaitCollection.Enqueue(waitThreadOrderMatters?order:(int?)null,manualResetEvent);
                    }

                    manualResetEvent.WaitOne();
                }
            }

            // Тут собственно делаем это самое определённое действе с пулом
            using (writeLockForReleasing.WriteLock())
            {
                T result = returnFunction();
                ManualResetEvent manualResetEvent = waitersToReleaseCollection.DequeueOrNullOnlyIfOrderIsNull(releaseThreadOrderMatters ? order : (int?)null);
                manualResetEvent?.Set();
                return result;
            }            
        }
    }
}