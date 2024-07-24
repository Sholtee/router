/********************************************************************************
* StaticDictionary.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
#if !DEBUG
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using Properties;

    /// <summary>
    /// Dictionary having predefined keys
    /// </summary>
    internal sealed class StaticDictionary(IReadOnlyList<string> keys, LookupDelegate<StaticDictionary.ValueWrapper> lookup) : IReadOnlyDictionary<string, object?>, IParamAccessByInternalId
    {
        public struct ValueWrapper
        {
            public bool Assigned;
            public object? Value;
        }

        #if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        #endif
        private readonly ValueWrapper[] FValues = new ValueWrapper[keys.Count];

        public object? this[string key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!TryGetValue(key, out object? value))
                    throw new KeyNotFoundException(key);

                return value;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (string key in keys)
                {
                    if (lookup(FValues, key).Assigned)
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
            ref ValueWrapper val = ref lookup(FValues, key);
            if (Unsafe.IsNullRef(ref val) || !val.Assigned)
            {
                value = null;
                return false;
            }

            value = val.Value;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public object? this[int internalId]
        {
            get
            {
                if (internalId >= 0 && internalId < FValues.Length)
                {
                    ValueWrapper val = FValues[internalId];
                    if (val.Assigned)
                        return val.Value;
                }
                throw new ArgumentException(Resources.INVALID_ID, nameof(internalId));
            }
            set
            {
                if (internalId < 0 || internalId >= FValues.Length)
                    throw new ArgumentException(Resources.INVALID_ID, nameof(internalId));

                ref ValueWrapper val = ref FValues[internalId];
                val.Value = value;

                if (!val.Assigned)
                {
                    val.Assigned = true;
                    Count++;
                }
            }
        }
    }
}