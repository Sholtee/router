/********************************************************************************
* PathSplitting.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Router.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class PathSplitting
    {
        [Params(1, 2, 5, 10)]
        public int SegmentCount { get; set; }

        public string Input { get; set; } = null!;

        [GlobalSetup(Target = nameof(Split))]
        public void SetupSplit() => Input = "/" + string.Join("/", Enumerable.Repeat("segemnt", SegmentCount));

        [Benchmark]
        public void Split()
        {
            using PathSplitter enumerator = PathSplitter.Split(Input);
            while (enumerator.MoveNext())
            {
                _ = enumerator.Current;
            }
        }
    }

    [MemoryDiagnoser]
    public class StringFromCharArray
    {
        public static readonly char[] FChars = Enumerable.Repeat('x', 100).ToArray();

        [Params(1, 5, 10, 100)]
        public int Length { get; set; }

        [Benchmark]
        public string CreateString() => new(FChars, 0, Length);
    }

    [MemoryDiagnoser]
    public class Enumerator
    {
        [Params(1, 5, 10, 100)]
        public int Length { get; set; }

        private IEnumerable<int> GetEnum()
        {
            for (int i = 0; i < Length; i++)
            {
                yield return i;
            }
        }

        [Benchmark]
        public void Enumerate()
        {
            foreach (int i in GetEnum()) { }
        }
    }
}
