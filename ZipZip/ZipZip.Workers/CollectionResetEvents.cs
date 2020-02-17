using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZipZip.Workers
{
    public class CollectionResetEvents
    {
        private int _negativeIndex = -1;
        
        private readonly Dictionary<int,ManualResetEvent> _dictionary = new Dictionary<int, ManualResetEvent>();
        public void Enqueue(int? order, ManualResetEvent manualResetEvent)
        {
            _dictionary.Add(order ?? _negativeIndex--, manualResetEvent);
        }

        public ManualResetEvent DequeueOrNull(int? order)
        {
            if (order != null)
            {
                return _dictionary.TryGetValue((int) order, out ManualResetEvent result) ? result : null;
            }
            
            if (_dictionary.Count == 0) return null;

            int firstKey = _dictionary.Keys.First();
                
            ManualResetEvent manualResetEvent = _dictionary[firstKey];
            
            _dictionary.Remove(firstKey);
                
            return manualResetEvent;

        }
    }
}