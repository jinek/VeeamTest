using System.Collections.Generic;
using System.Linq;
using ZipZip.Threading.PrimitiveThreadLockers;

namespace ZipZip.Threading
{
    public class SimpleConcurrentDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        
        private readonly ReadWriteThreadLocker _threadLocker = new ReadWriteThreadLocker();

        public SimpleConcurrentDictionary(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
        }

        private int InitialCapacity { get; }

        public bool TryRemove(TKey order, out TValue item)
        {
            item = default;
            using (_threadLocker.ReadLock())
            {
                if (!_dictionary.ContainsKey(order))
                    return false;
            }

            using (_threadLocker.WriteLock())
            {
                if (!_dictionary.TryGetValue(order, out item)) return false;

                _dictionary.Remove(order);

                return true;
            }
        }

        public bool TryRemoveFirst(out TKey order, out TValue value)
        {
            using (_threadLocker.WriteLock())
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
            using (_threadLocker.WriteLock())
            {
                if (!_dictionary.ContainsKey(order))
                    _dictionary.Add(order, item);

                return _dictionary.Count < InitialCapacity;
            }
        }
    }
}