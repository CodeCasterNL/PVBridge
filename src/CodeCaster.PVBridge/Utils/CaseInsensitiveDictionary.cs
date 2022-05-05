using System;
using System.Collections.Generic;

namespace CodeCaster.PVBridge.Utils
{
    public class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public CaseInsensitiveDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public CaseInsensitiveDictionary(IDictionary<string, TValue> dictionary)
            : this()
        {
            foreach (var (key, value) in dictionary)
            {
                this[key] = value;
            }
        }
    }
}
