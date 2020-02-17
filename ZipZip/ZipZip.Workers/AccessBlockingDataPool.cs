using System;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataPool<T>
    {
        private readonly Locker _additionalPopLocksLock = new Locker();

        private readonly Locker _additionalPushLocksLock = new Locker();

        private readonly FastConcurrentDictionary<int, T> _bag;

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
            if (!_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and receive {nameof(order)}");

            T item = default;

            return PoolIdea.ThreadSafeAccessToPool(true,
                false,
                order,
                _waitingAddLock,
                () => !_bag.TryRemove(order, out item),
                _additionalPushLocksLock,
                _pushLocks,
                _waitingPopLock,
                () => item,
                _popLocks);
        }

        public T Pop(out int order)
        {
            if (_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and provide {nameof(order)}");

            (T returnedItem, int returnedOrder) = PoolIdea.ThreadSafeAccessToPool<(T, int)>(false,
                false,
                -1,
                _waitingAddLock,
                () => _bag.Count == 0,
                _additionalPushLocksLock,
                _pushLocks,
                _waitingPopLock,
                () =>
                {
                    T item = _bag.RemoveFirst(out int orderLocal);
                    return (item, orderLocal);
                }, _popLocks);

            order = returnedOrder;
            return returnedItem;
        }

        //todo: сделать AggressiveInline

        public void Add(T item, int order)
        {
            PoolIdea.ThreadSafeAccessToPool<object>(false,
                _orderMatters,
                order,
                _waitingPopLock,
                () => _bag.Count >= _bag.InitialCapacity,
                _additionalPopLocksLock,
                _popLocks,
                _waitingAddLock,
                () =>
                {
                    _bag.Add(order, item);
                    return null;
                }, _pushLocks);
        }
    }
}