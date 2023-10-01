/********************************************************************************
* StaticDictionaryBuilder.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.Router.Internals
{
    internal sealed class StaticDictionaryBuilder
    {
        private readonly LookupBuilder<StaticDictionary.ValueWrapper> FLookupBuilder = new(StringComparer.Ordinal);

        public bool RegisterKey(string key) => FLookupBuilder.CreateSlot(key);

        public StaticDictionaryFactory CreateFactory()
        {
            List<string> keys = new(FLookupBuilder.Slots);
            LookupDelegate<StaticDictionary.ValueWrapper> lookup = FLookupBuilder.Build(out int arSize);

            Debug.Assert(arSize == keys.Count, "Size mismatch");

            return CreateDict;

            StaticDictionary CreateDict() => new(keys, lookup);
        }
    }
}
