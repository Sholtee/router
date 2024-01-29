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
    internal sealed class StaticDictionary: IReadOnlyDictionary<string, object?>, IElementAccessByInternalId
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

        public object? GetElementByInternalId(int internalId)
        {
            if (internalId >= 0 && internalId < FValues.Length)
            {
                ValueWrapper val = FValues[internalId];
                if (val.Assigned)
                    return val.Value;
            }
            throw new ArgumentException(Resources.INVALID_ID, nameof(internalId));
        }

        public void SetElementByInternalId(int internalId, object? value)
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