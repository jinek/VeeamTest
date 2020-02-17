using System.Collections.Generic;
using System.Linq;

namespace ZipZip.Lockers
{
    public class FastConcurrentDictionary<TKey, TValue>
    {
        public FastConcurrentDictionary(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
        }

        private readonly ReadWriteLock _bagLocker = new ReadWriteLock();
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public int Count
        {
            get
            {
                using (_bagLocker.ReadLock())
                {
                    return _dictionary.Count;
                }
            }
        }

        public int InitialCapacity { get; }

        public bool TryRemove(TKey order, out TValue item)
        {
            item = default;
            using (_bagLocker.ReadLock())
            {
                if (!_dictionary.ContainsKey(order))
                    return false;
            }

            using (_bagLocker.WriteLock())
            {
                return _dictionary.TryGetValue(order, out item);
            }
        }

        public TValue RemoveFirst(out TKey order)
        {
            //todo:! опасносте! локально нет обоснований пологать, что когда это вызвали коллекция не пуста
            using (_bagLocker.WriteLock())
            {
                order = _dictionary.Keys.First();
                return _dictionary[order];
            }
        }

        public void Add(TKey order, TValue item)
        {
            using (_bagLocker.WriteLock())
            {
                _dictionary.Add(order, item);
            }
        }
    }
}