using System;
using System.Diagnostics;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class AccessBlockingDataPool<T>
    {
        internal readonly FastConcurrentDictionary<int, T> _bag;

        private readonly bool _orderMatters;
        private readonly IZipZipWorker _zipZipWorker;
        private readonly WaitersCollection _popWaiters;

        private readonly WaitersCollection _pushWaiters;

        public AccessBlockingDataPool(int capacity, bool orderMatters,IZipZipWorker zipZipWorker)
        {
            _orderMatters = orderMatters;
            _zipZipWorker = zipZipWorker;
            _bag = new FastConcurrentDictionary<int, T>(capacity);
            _popWaiters = new WaitersCollection();
            _pushWaiters = new WaitersCollection();
            
            _popWaiters.CountChanged+=CountChanged;
            _pushWaiters.CountChanged+=CountChanged;
        }

        private void CountChanged()
        {
            if(_popWaiters.Count + _pushWaiters.Count==13)
                Debugger.Break();
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

        //todo: сделать AggressiveInline

        public void Add(T item, int order)
        {
            WaitersCollection.AccessWaiter releaseWaiterByOrder = _orderMatters
                ? WaitersCollection.AccessWaiter.ReleaseWaiterByOrder(order)
                : WaitersCollection.AccessWaiter.OrdersDoesNotMatter();

            //todo: тут раньше order передавался, норм ли всё теперь?
            PoolIdea.ThreadSafeAccessToPool(releaseWaiterByOrder,
                () =>
                {
                    bool shouldWait = !_bag.AddOrNothingThenAndCheckIfNotFull(order, item);

                    /*if(shouldWait && _orderMatters && ((ZipZipCompress)_zipZipWorker)._inputBuffer.)
                        Debugger.Break();*/
                    
                    return (shouldWait, true);
                },
                _popWaiters,
                _pushWaiters);
        }
    }
}