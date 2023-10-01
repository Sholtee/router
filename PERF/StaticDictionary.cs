/********************************************************************************
* StaticDictionary.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Router.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class StaticDictionary
    {
        [Params(1, 2, 5, 10, 20, 30)]
        public int ItemCount { get; set; }

        private string[] Keys = null!;

        private int Index;

        private Dictionary<string, object?> RegularDictInst = null!;

        [GlobalSetup(Target = nameof(RegularDict))]
        public void SetupRegularDict()
        {
            Index = 0;
            RegularDictInst = new Dictionary<string, object?>(ItemCount);
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                RegularDictInst[Keys[i] = Path.GetRandomFileName()] = null;
            }           
        }

        [Benchmark(Baseline = true)]
        public object? RegularDict() => RegularDictInst[Keys[Index++ % ItemCount]];

        private Internals.StaticDictionary StaticDictInst = null!;

        [GlobalSetup(Target = nameof(StaticDict))]
        public void SetupStaticDict()
        {
            Index = 0;
            StaticDictionaryBuilder bldr = new();
            Keys = new string[ItemCount];
            for (int i = 0; i < ItemCount; i++)
            {
                bldr.RegisterKey(Keys[i] = Path.GetRandomFileName());
            }

            StaticDictInst = bldr.CreateFactory().Invoke();

            for (int i = 0; i < ItemCount; i++)
            {
                StaticDictInst.Add(Keys[i], null);
            }
        }

        [Benchmark]
        public object? StaticDict() => StaticDictInst[Keys[Index++ % ItemCount]];
    }
}
