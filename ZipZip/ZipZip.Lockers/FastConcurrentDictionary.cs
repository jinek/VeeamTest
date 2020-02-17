using System.Collections.Generic;
using System.Linq;

namespace ZipZip.Lockers
{
    public class FastConcurrentDictionary<TKey, TValue>
    {
        private readonly ReadWriteLock _bagLocker = new ReadWriteLock();
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        
        public FastConcurrentDictionary(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
        }

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

        private int InitialCapacity { get; }

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
                if (!_dictionary.TryGetValue(order, out item)) return false;
                
                _dictionary.Remove(order);
                
                return true;
            }
        }

        public bool TryRemoveFirst(out TKey order, out TValue value)
        {
            using (_bagLocker.WriteLock())
            {
                if (_dictionary.Count == 0)
                {
                    order = default;
                    value = default;
                    return false;
                }

                order = _dictionary.Keys.Min();
                value = _dictionary[order];
                _dictionary.Remove(order);
                return true;
            }
        }

        public bool AddOrNothingThenAndCheckIfNotFull(TKey order, TValue item)
        {
            using (_bagLocker.WriteLock())
            {
                if(!_dictionary.ContainsKey(order))
                    _dictionary.Add(order, item);
                
                return _dictionary.Count < InitialCapacity;
            }
        }
    }
}