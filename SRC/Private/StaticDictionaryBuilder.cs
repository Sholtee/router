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
    using Primitives;

    internal sealed class StaticDictionaryBuilder
    {
        private readonly LookupBuilder<StaticDictionary.ValueWrapper> FLookupBuilder = new(StringComparison.Ordinal);

        public bool RegisterKey(string key) => FLookupBuilder.CreateSlot(key);

        public StaticDictionaryFactory CreateFactory(DelegateCompiler compiler, out IReadOnlyDictionary<string, int> shortcuts)
        {
            List<string> keys = new(FLookupBuilder.Slots);
            LookupDelegate<StaticDictionary.ValueWrapper> lookup = FLookupBuilder.Build(compiler, out shortcuts);

            Debug.Assert(shortcuts.Count == keys.Count, "Size mismatch");

            return CreateDict;

            StaticDictionary CreateDict() => new(keys, lookup);
        }
    }
}
