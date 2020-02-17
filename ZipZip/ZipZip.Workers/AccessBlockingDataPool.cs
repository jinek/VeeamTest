using System;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataPool<T>
    {
        private bool _orderMatters;
        
        private readonly FastConcurrentDictionary<int,T> _bag = new FastConcurrentDictionary<int, T>(/*Добавить capacity и concurrency*/);
        
        private readonly ReadWriteLock _waitingAddLock = new ReadWriteLock();
        private readonly ReadWriteLock _waitingPopLock = new ReadWriteLock();

        public T Pop(int order)
        {
            if(!_orderMatters)
                throw new InvalidOperationException($"Call another overload of {nameof(Pop)} and receive {nameof(order)}");

            T item=null;
            
            return PoolIdea.ThreadSafeAccessToPool<T>(true,
                false,
                order,
                _waitingAddLock,
                () => !_bag.TryRemove(order,out item),
                _additionalPushLocksLock,
                pushLocks,
                _waitingPopLock,
                () => item, 
                _popLocks);
        }
        
        public T Pop(out int order)
        {
            if(_orderMatters)
                throw new InvalidOperationException($"Call another overload of {nameof(Pop)} and provide {nameof(order)}");

            var (returnedItem,returnedOrder) = PoolIdea.ThreadSafeAccessToPool<(T, order)>(false,
                false,
                -1,
                _waitingAddLock,
                () => _bag.Count == 0,
                _additionalPushLocksLock,
                pushLocks,
                _waitingPopLock,
                () =>
                {
                    T item = _bag.First(out order);
                    _bag.RemoveFirst();
                    return (order, item);
                }, _popLocks);

            order = returnedOrder;
            return returnedItem;
        }

        //todo: сделать AggressiveInline

        public void Add(T item, int order)
        {
            PoolIdea.ThreadSafeAccessToPool(false,
                _orderMatters,
                order,
                _waitingPopLock,
                () => _bag.Count >= _bag.Capacity,
                _additionalPopLocksLock,
                _popLocks,
                _waitingAddLock,
                () =>
                {
                    _bag.Add(order, item);
                    return null;
                },_pushLocks);
        }
    }
}