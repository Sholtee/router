/********************************************************************************
* StaticDictionary.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
#if !DEBUG
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    /// <summary>
    /// Dictionary having predefined keys
    /// </summary>
    internal sealed class StaticDictionary: IReadOnlyDictionary<string, object?>
    {
        public struct ValueWrapper
        {
            public bool Assigned;
            public object? Value;
        }

#if !DEBUG  // inspecting all keys can be confusing
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
        private readonly IReadOnlyList<string> FKeys;

        private readonly LookupDelegate<ValueWrapper> FLookup;
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
        private readonly ValueWrapper[] FValues;

        public StaticDictionary(IReadOnlyList<string> keys, LookupDelegate<ValueWrapper> lookup)
        {
            FKeys   = keys;
            FLookup = lookup;
            FValues = new ValueWrapper[keys.Count];
        }

        public object? this[string key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref ValueWrapper val = ref FLookup(FValues, key);
                if (!val.Assigned)
                    throw new KeyNotFoundException(key);

                return val.Value;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (string key in FKeys)
                {
                    if (FLookup(FValues, key).Assigned)
                        yield return key;
                }
            }
        }

        public IEnumerable<object?> Values
        {
            get
            {
                foreach (string key in Keys)
                {
                    yield return this[key];
                }
            }
        }

        public int Count { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, object? value)
        {
            ref ValueWrapper val = ref FLookup(FValues, key);
            val.Value = value;

            if (!val.Assigned)
            {
                val.Assigned = true;
                Count++;
            }
        }

        public bool ContainsKey(string key) => TryGetValue(key, out _);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (string key in Keys)
            {
                yield return new KeyValuePair<string, object?>(key, this[key]);
            }
        }

        public bool TryGetValue(string key, out object? value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
