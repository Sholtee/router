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

        [GlobalSetup(Target = nameof(RegularDictGet))]
        public void SetupRegularDictGet()
        {
            RegularDictInst = new Dictionary<string, object?>(ItemCount);
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                RegularDictInst[Keys[i] = Path.GetRandomFileName()] = null;
            }           
        }

        [Benchmark]
        public object? RegularDictGet() => RegularDictInst[Keys[Random.Next(ItemCount)]];

        [GlobalSetup(Target = nameof(RegularDictAdd))]
        public void SetupRegularDictAdd()
        {
            RegularDictInst = new Dictionary<string, object?>(ItemCount);
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                Keys[i] = Path.GetRandomFileName();
            }
        }

        [Benchmark]
        public void RegularDictAdd() => RegularDictInst[Keys[Random.Next(ItemCount)]] = null;

        private Internals.StaticDictionary StaticDictInst = null!;

        [GlobalSetup(Target = nameof(StaticDictGet))]
        public void SetupStaticDictGet()
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
        public object? StaticDictGet() => StaticDictInst[Keys[Random.Next(ItemCount)]];

        [GlobalSetup(Target = nameof(StaticDictAdd))]
        public void SetupStaticDictAdd()
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
        }

        [Benchmark]
        public void StaticDictAdd() => StaticDictInst.Add(Keys[Random.Next(ItemCount)], null);
    }
}
