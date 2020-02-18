using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZipZip.Threading.PrimitiveThreadLockers;

namespace ZipZip.Workers
{
    internal class WaitersCollection
    {
        private readonly Dictionary<int, ManualResetEvent> _dictionary = new Dictionary<int, ManualResetEvent>();
        private readonly ThreadLocker _threadLocker = new ThreadLocker();
        private int _negativeIndex = -1;

        public IDisposable LockOtherThreadsAccessingThisCollection()
        {
            return _threadLocker.Lock();
        }

        private void CreateWaiter(int? order, ManualResetEvent manualResetEvent)
        {
            using (_threadLocker.Lock())
            {
                _dictionary.Add(order ?? _negativeIndex--, manualResetEvent);
            }
        }

        private void ReleaseWaiter(int? order)
        {
            using (_threadLocker.Lock())
            {
                if (order != null)
                {
                    bool tryGetValue = _dictionary.TryGetValue((int) order, out ManualResetEvent result);
                    if (!tryGetValue) return;
                    _dictionary.Remove((int) order);
                    result.Set();
                    return;
                }

                if (_dictionary.Count == 0) return;

                int firstKey = _dictionary.Keys.Min();

                ManualResetEvent manualResetEvent = _dictionary[firstKey];

                _dictionary.Remove(firstKey);
                manualResetEvent.Set();
            }
        }

        public readonly struct WaitersMode
        {
            //эта структура нужно просто для объяснения кода

            public static WaitersMode OrdersDoesNotMatter()
            {
                return new WaitersMode(false, false, -1);
            }

            public static WaitersMode ReleaseWaiterByOrder(int order)
            {
                return new WaitersMode(true, false, order);
            }

            public static WaitersMode CreateWaiterForOrder(int order)
            {
                return new WaitersMode(false, true, order);
            }

            private WaitersMode(bool orderMattersForReleasingAThread, bool orderMattersForWaiting, int order)
            {
                OrderMattersForReleasingAThread = orderMattersForReleasingAThread;
                OrderMattersForWaiting = orderMattersForWaiting;
                Order = order;
            }

            public bool OrderMattersForReleasingAThread { get; }
            public bool OrderMattersForWaiting { get; }
            public int Order { get; }

            [ThreadStatic] private static ManualResetEvent ResetEventForThisThread;


            public WaitHandle CreateWaiter(WaitersCollection waitersCollection)
            {
                if (ResetEventForThisThread == null)
                    ResetEventForThisThread = new ManualResetEvent(false);
                else
                    ResetEventForThisThread.Reset();

                waitersCollection.CreateWaiter(OrderMattersForWaiting ? Order : (int?) null, ResetEventForThisThread);

                return ResetEventForThisThread;
            }

            public void ReleaseWaiter(WaitersCollection waitersCollection)
            {
                waitersCollection.ReleaseWaiter(OrderMattersForReleasingAThread ? Order : (int?) null);
            }
        }
    }
}