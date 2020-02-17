using System;
using System.Threading;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class PoolIdea
    {
        public static T ThreadSafeAccessToPool<T>(bool waitThreadOrderMatters,
            bool releaseThreadOrderMatters,
            int order,//todo: эти три параметра потом переделать в один. Возможно стоит инициализировать инстанс через статичные методы передавая order когда нужно.Кстати, можно что б класс сам из коллекции вытаскивал.. 
            ReadWriteLock readLockForWaiting,
            Func<bool> waitCondition,
            Locker additionalModifyWaitersLock,
            SomeCollection waitersToWaitCollection,
            ReadWriteLock writeLockForReleasing,
            Func<T> returnFunction,
            SomeCollection waitersToReleaseCollection//todo: вместо этого можно сделать func, которая будет возвращать нужный ManualResetEvent
        )
        {
            //тут выполняем проверку можно ли сделать определённое действие с пулом и если нельзя, то усыпляем текущий поток, помещая его в  список ожидания        
            using (readLockForWaiting.ReadLock())
            {
                while (waitCondition())
                {
                    ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                    using (additionalModifyWaitersLock.Lock())
                    {
                        waitersToWaitCollection.Enqueue(waitThreadOrderMatters?order:null,manualResetEvent);
                    }

                    manualResetEvent.WaitOne();
                }
            }

            // Тут собственно делаем это самое определённое действе с пулом
            using (writeLockForReleasing.WriteLock())
            {
                T result = returnFunction();
                var manualResetEvent = waitersToReleaseCollection.DequeueOrNullIfEmptyForOrderOnly(releaseThreadOrderMatters ? order : null);
                manualResetEvent?.Set();
                return result;
            }            
        }
    }
}