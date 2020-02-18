using System;
using ZipZip.Threading;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataBuffer<T>
    {
        private readonly SimpleConcurrentDictionary<int, T> _internalBuffer;

        private readonly bool _orderMatters;
        private readonly WaitersCollection _pullWaiters = new WaitersCollection();

        private readonly WaitersCollection _pushWaiters = new WaitersCollection();

        public AccessBlockingDataBuffer(int capacity, bool orderMatters)
        {
            _orderMatters = orderMatters;
            _internalBuffer = new SimpleConcurrentDictionary<int, T>(capacity);
        }
        public T Pull(int order)
        {
            if (!_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pull)} and receive {nameof(order)}");

            T item = default;

            BufferIdea.ThreadSafeAccessToBuffer(WaitersCollection.WaitersMode.CreateWaiterForOrder(order),
                () =>
                {
                    bool shouldWait = !_internalBuffer.TryRemove(order, out item);
                    return (shouldWait, !shouldWait);
                },
                _pushWaiters,
                _pullWaiters);

            return item;
        }

        public T Pull(out int order)
        {
            if (_orderMatters)
                throw new InvalidOperationException(
                    $"Call another overload of {nameof(Pull)} and provide {nameof(order)}");

            T returnItem = default;
            int returnedOrder = default;

            BufferIdea.ThreadSafeAccessToBuffer(WaitersCollection.WaitersMode.OrdersDoesNotMatter(),
                () =>
                {
                    bool shouldWait = !_internalBuffer.TryRemoveFirst(out returnedOrder, out returnItem);
                    return (shouldWait, !shouldWait);
                },
                _pushWaiters,
                _pullWaiters);

            order = returnedOrder;
            return returnItem;
        }

        public void Add(T item, int order)
        {
            WaitersCollection.WaitersMode waitersMode = _orderMatters
                ? WaitersCollection.WaitersMode.ReleaseWaiterByOrder(order)
                : WaitersCollection.WaitersMode.OrdersDoesNotMatter();
            
            BufferIdea.ThreadSafeAccessToBuffer(waitersMode,
                () =>
                {
                    bool shouldWait = !_internalBuffer.AddOrNothingThenAndCheckIfNotFull(order, item);
                    
                    return (shouldWait, true);
                },
                _pullWaiters,
                _pushWaiters);
        }
    }
}