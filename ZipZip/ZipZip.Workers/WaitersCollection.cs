using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal class WaitersCollection
    {
        private readonly Locker _locker = new Locker();

        public IDisposable LockOtherThreadsAccessingThisCollection()
        {
            return _locker.Lock();
        }
        
        public readonly struct AccessWaiter
        {
            //эта структура нужно просто для объяснения кода
            
            public static AccessWaiter OrdersDoesNotMatter()
            {
                return new AccessWaiter(false,false, -1);
            }

            public static AccessWaiter ReleaseWaiterByOrder(int order)
            {
                return new AccessWaiter(true,false, order);
            }
            
            public static AccessWaiter CreateWaiterForOrder(int order)
            {
                return new AccessWaiter(false,true, order);
            }
            
            private AccessWaiter(bool orderMattersForReleasingAThread, bool orderMattersForWaiting, int order)
            {
                OrderMattersForReleasingAThread = orderMattersForReleasingAThread;
                OrderMattersForWaiting = orderMattersForWaiting;
                Order = order;
            }

            public bool OrderMattersForReleasingAThread { get; }
            public bool OrderMattersForWaiting { get; }
            public int Order { get; }
            

            public WaitHandle CreateWaiter(WaitersCollection waitersCollection)
            {
                var manualResetEvent = new ManualResetEvent(false);//todo: можно ThreadStatic делать
                
                waitersCollection.CreateWaiter(OrderMattersForWaiting ? Order : (int?) null,manualResetEvent);

                return manualResetEvent;
            }

            public void ReleaseWaiter(WaitersCollection waitersCollection)
            {
                waitersCollection.ReleaseWaiter(OrderMattersForReleasingAThread ? Order : (int?) null);
            }
        }
        
        private readonly Dictionary<int, ManualResetEvent> _dictionary;
        private int _negativeIndex = -1;

        private void CreateWaiter(int? order, ManualResetEvent manualResetEvent)
        {
            using (_locker.Lock())
            {
                _dictionary.Add(order ?? _negativeIndex--, manualResetEvent);
                Count = _dictionary.Count;
                CountChanged();
            }
        }

        private void ReleaseWaiter(int? order)
        {
            using (_locker.Lock())
            {
                if (order != null)
                {
                    bool tryGetValue = _dictionary.TryGetValue((int) order, out ManualResetEvent result);
                    if (!tryGetValue) return;
                    _dictionary.Remove((int) order);
                    result.Set();
                    Count = _dictionary.Count;
                    CountChanged();
                    return;
                }

                if (_dictionary.Count == 0) return;

                int firstKey = _dictionary.Keys.Min();

                ManualResetEvent manualResetEvent = _dictionary[firstKey];

                _dictionary.Remove(firstKey);
                manualResetEvent.Set();
                Count = _dictionary.Count;
                CountChanged();
            }
        }

        public int Count;

        public WaitersCollection()
        {
            _dictionary = new Dictionary<int, ManualResetEvent>();
        }

        public event Action CountChanged;
    }
}