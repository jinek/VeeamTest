using System;
using ZipZip.Threading;

namespace ZipZip.Workers.DataBuffer
{
    internal class AccessBlockingDataBuffer<T>
    {
        private readonly WaitersCollection _addWaiters = new WaitersCollection();
        private readonly SimpleConcurrentDictionary<int, T> _internalBuffer;
        private readonly bool _orderMatters;
        private readonly WaitersCollection _pullWaiters = new WaitersCollection();

        /// <summary>
        ///     See parameters description.
        ///     For more information see description to <see cref="BufferIdea.ThreadSafeAccessToBuffer" />
        /// </summary>
        /// <param name="capacity">How much data items buffer can contain. Will freeze calling thread when exceeded</param>
        /// <param name="orderMatters">Whether we want to pull data by order.</param>
        public AccessBlockingDataBuffer(int capacity, bool orderMatters)
        {
            _orderMatters = orderMatters;
            _internalBuffer = new SimpleConcurrentDictionary<int, T>(capacity);
        }

        public void AbortAllWaiters()
        {
            _addWaiters.AbortWaiters();
            _pullWaiters.AbortWaiters();
        }

        /// <summary>
        ///     Pulls specific item from buffer. Release a frozen thread which is waiting buffer to become not so overflowed (if
        ///     any)
        /// </summary>
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
                _addWaiters,
                _pullWaiters);

            return item;
        }

        /// <summary>
        ///     Pull any item from buffer. Release a frozen thread which is waiting buffer to become not so overflowed (if any)
        /// </summary>
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
                _addWaiters,
                _pullWaiters);

            order = returnedOrder;
            return returnItem;
        }

        /// <summary>
        ///     Add data item to buffer. Also release a frozen thread which is waiting this data item to be in the buffer
        /// </summary>
        public void Add(T item, int order)
        {
            WaitersCollection.WaitersMode waitersMode = _orderMatters
                ? WaitersCollection.WaitersMode.ReleaseWaiterByOrder(order)
                : WaitersCollection.WaitersMode.OrdersDoesNotMatter();

            bool hasBeenAdded = false;
            
            BufferIdea.ThreadSafeAccessToBuffer(waitersMode,
                () =>
                {
                    bool shouldWait;
                    bool justAddedInThisIteration = false;
                    
                    //First time, adding item to buffer
                    if (!hasBeenAdded)
                    {
                        shouldWait = !_internalBuffer.AddItemAndCheckNotFull(order, item);
                        hasBeenAdded = justAddedInThisIteration = true;
                    }
                    //next time, only checking if we can release this thread to process next item
                    else
                    {
                        shouldWait = _internalBuffer.IsFull;
                    }

                    return (shouldWait, justAddedInThisIteration);
                },
                _pullWaiters,
                _addWaiters);
        }
    }
}