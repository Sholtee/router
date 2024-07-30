/********************************************************************************
* StaticDictionary.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private int[] Ids = null!;

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

        private Internals.StaticDictionary<object?> StaticDictInst = null!;
    
        private void SetupStaticDictGet()
        {
            StaticDictionary<object?>.Builder bldr = new();
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                bldr.RegisterKey(Keys[i] = Path.GetRandomFileName());
            }

            DelegateCompiler compiler = new();
            StaticDictInst = bldr.CreateFactory(compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();     
            compiler.Compile();

            Ids = shortcuts.Values.ToArray();

            for (int i = 0; i < ItemCount; i++)
            {
                StaticDictInst[shortcuts[Keys[i]]] = null;
            }
        }

        [GlobalSetup(Target = nameof(StaticDictGetByName))]
        public void SetupStaticDictGetByName() => SetupStaticDictGet();

        [Benchmark]
        public object? StaticDictGetByName() => StaticDictInst[Keys[Random.Next(ItemCount)]];

        [GlobalSetup(Target = nameof(StaticDictGetById))]
        public void SetupStaticDictGetById() => SetupStaticDictGet();

        [Benchmark]
        public object? StaticDictGetById() => StaticDictInst[Ids[Random.Next(ItemCount)]];

        [GlobalSetup(Target = nameof(StaticDictAdd))]
        public void SetupStaticDictAdd()
        {
            StaticDictionary<object?>.Builder bldr = new();
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                bldr.RegisterKey(Keys[i] = Path.GetRandomFileName());
            }
 
            DelegateCompiler compiler = new();
            StaticDictInst = bldr.CreateFactory(compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();

            Ids = shortcuts.Values.ToArray();
            compiler.Compile();
        }

        [Benchmark]
        public void StaticDictAdd() => StaticDictInst[Ids[Random.Next(ItemCount)]] = null;
    }
}
