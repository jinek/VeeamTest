using System;
using System.Diagnostics;
using ZipZip.Threading;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataPool<T>
    {
        private readonly SimpleConcurrentDictionary<int, T> _bag;

        private readonly bool _orderMatters;
        private readonly WaitersCollection _popWaiters = new WaitersCollection();

        private readonly WaitersCollection _pushWaiters = new WaitersCollection();

        public AccessBlockingDataPool(int capacity, bool orderMatters)
        {
            _orderMatters = orderMatters;
            _bag = new SimpleConcurrentDictionary<int, T>(capacity);
        }
        public T Pop(int order)
        {
            if (!_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and receive {nameof(order)}");

            T item = default;


            PoolIdea.ThreadSafeAccessToPool(WaitersCollection.AccessWaiter.CreateWaiterForOrder(order),
                () =>
                {
                    bool shouldWait = !_bag.TryRemove(order, out item);
                    return (shouldWait, !shouldWait);
                },
                _pushWaiters,
                _popWaiters);

            return item;
        }

        public T Pop(out int order)
        {
            if (_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pop)} and provide {nameof(order)}");

            T returnItem = default;
            int returnedOrder = default;

            PoolIdea.ThreadSafeAccessToPool(WaitersCollection.AccessWaiter.OrdersDoesNotMatter(),
                () =>
                {
                    bool shouldWait = !_bag.TryRemoveFirst(out returnedOrder, out returnItem);
                    return (shouldWait, !shouldWait);
                },
                _pushWaiters,
                _popWaiters);

            order = returnedOrder;
            return returnItem;
        }

        public void Add(T item, int order)
        {
            WaitersCollection.AccessWaiter releaseWaiterByOrder = _orderMatters
                ? WaitersCollection.AccessWaiter.ReleaseWaiterByOrder(order)
                : WaitersCollection.AccessWaiter.OrdersDoesNotMatter();
            
            PoolIdea.ThreadSafeAccessToPool(releaseWaiterByOrder,
                () =>
                {
                    bool shouldWait = !_bag.AddOrNothingThenAndCheckIfNotFull(order, item);
                    
                    return (shouldWait, true);
                },
                _popWaiters,
                _pushWaiters);
        }
    }
}