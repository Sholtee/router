/********************************************************************************
* StaticDictionary.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Router.Perf
{
    using Internals;
    using Primitives;

    [MemoryDiagnoser]
    public class StaticDictionary
    {
        [Params(1, 2, 5, 10, 20, 30)]
        public int ItemCount { get; set; }

        private string[] Keys = null!;

        private readonly Random Random = new();

        private Dictionary<string, object?> RegularDictInst = null!;

        [GlobalSetup(Target = nameof(RegularDict))]
        public void SetupRegularDict()
        {
            RegularDictInst = new Dictionary<string, object?>(ItemCount);
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                RegularDictInst[Keys[i] = Path.GetRandomFileName()] = null;
            }           
        }

        [Benchmark(Baseline = true)]
        public object? RegularDict() => RegularDictInst[Keys[Random.Next(ItemCount)]];

        private Internals.StaticDictionary StaticDictInst = null!;

        [GlobalSetup(Target = nameof(StaticDict))]
        public void SetupStaticDict()
        {
            StaticDictionaryBuilder bldr = new();
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                bldr.RegisterKey(Keys[i] = Path.GetRandomFileName());
            }

            DelegateCompiler compiler = new();
            StaticDictInst = bldr.CreateFactory(compiler).Invoke();
            compiler.Compile();

            for (int i = 0; i < ItemCount; i++)
            {
                StaticDictInst.Add(Keys[i], null);
            }
        }

        [Benchmark]
        public object? StaticDict() => StaticDictInst[Keys[Random.Next(ItemCount)]];
    }
}
