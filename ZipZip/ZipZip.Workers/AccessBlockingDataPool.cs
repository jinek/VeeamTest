using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataPool<T>
    {
        private readonly Locker _additionalPopLocksLock = new Locker();

        private readonly Locker _additionalPushLocksLock = new Locker();

        public readonly FastConcurrentDictionary<int, T> _bag;

        private readonly bool _orderMatters;
        private readonly CollectionResetEvents _popLocks = new CollectionResetEvents();

        private readonly CollectionResetEvents _pushLocks = new CollectionResetEvents();

        private readonly ReadWriteLock _waitingAddLock = new ReadWriteLock();
        private readonly ReadWriteLock _waitingPopLock = new ReadWriteLock();

        public AccessBlockingDataPool(int capacity, bool orderMatters)
        {
            _orderMatters = orderMatters;
            _bag = new FastConcurrentDictionary<int, T>(capacity);
        }

        public T Pop(int order)
        {
            WriteDebug($"Retriving {order}");
            
            if (!_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and receive {nameof(order)}");

            T item = default;

            PoolIdea.ThreadSafeAccessToPool(true,
                false,
                order,
                _waitingAddLock,
                () =>
                {
                    bool shouldWait = !_bag.TryRemove(order, out item);
                    return (shouldWait,!shouldWait);
                },
                _additionalPushLocksLock,
                _pushLocks,
                _waitingPopLock,
                _popLocks);

            return item;
        }

        public T Pop(out int order)
        {
            WriteDebug($"Retrieving any");
            
            if (_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and provide {nameof(order)}");

            T returnItem = default;
            int returnedOrder = default;

            PoolIdea.ThreadSafeAccessToPool(false,
                false,
                -1,
                _waitingAddLock,
                () =>
                {
                    bool shouldWait = !_bag.TryRemoveFirst(out returnedOrder, out returnItem);
                    return (shouldWait,!shouldWait);
                },
                _additionalPushLocksLock,
                _pushLocks,
                _waitingPopLock,
                _popLocks);

            WriteDebug($"Retrived {returnedOrder}");
            
            order = returnedOrder;
            return returnItem;
        }

        //todo: сделать AggressiveInline

        public void Add(T item, int order)
        {
            WriteDebug($"Adding item {order}");
            PoolIdea.ThreadSafeAccessToPool(false,
                _orderMatters,
                order,
                _waitingPopLock,
                () =>
                {
                    bool shouldWait = !_bag.AddOrNothingThenAndCheckIfNotFull(order, item);
                    
                    return (shouldWait,true);
                },
                _additionalPopLocksLock,
                _popLocks,
                _waitingAddLock, 
                _pushLocks);
        }

        public void WriteDebug(string text)
        {
            return;
            string poolName = !_orderMatters?"Input Pool: ": "Output Pool: ";
            
            Debug.WriteLine($"{poolName}: {text}                 {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}