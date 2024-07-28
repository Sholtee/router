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
    internal sealed partial class StaticDictionary<TData>: IReadOnlyDictionary<string, TData?>, IParamAccessByInternalId<TData>
    {
        private struct ValueWrapper
        {
            public bool Assigned;
            public TData? Value;
        }

        private delegate ref ValueWrapper LookupDelegate(ValueWrapper[] ar, ReadOnlySpan<char> name);

        private readonly LookupDelegate FLookup;
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
        private readonly IReadOnlyList<string> FKeys;
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
        private readonly ValueWrapper[] FValues;

        private StaticDictionary(IReadOnlyList<string> keys, LookupDelegate lookup)
        {
            FLookup = lookup;
            FKeys = keys;
            FValues = new ValueWrapper[keys.Count];
        }

        public TData? this[string key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!TryGetValue(key, out TData? value))
                    throw new KeyNotFoundException(key);

                return value;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (string key in FKeys)
                {
                    if (FLookup(FValues, key.AsSpan()).Assigned)
                        yield return key;
                }
            }
        }

        public IEnumerable<TData?> Values
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

        public IEnumerator<KeyValuePair<string, TData?>> GetEnumerator()
        {
            foreach (string key in Keys)
            {
                yield return new KeyValuePair<string, TData?>(key, this[key]);
            }
        }

        public bool TryGetValue(ReadOnlySpan<char> key, out TData? value)
        {
            ref ValueWrapper val = ref FLookup(FValues, key);
            if (Unsafe.IsNullRef(ref val) || !val.Assigned)
            {
                value = default;
                return false;
            }

            value = val.Value;
            return true;
        }

        public bool TryGetValue(string key, out TData? value) =>
            TryGetValue(key.AsSpan(), out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TData? this[int internalId]
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