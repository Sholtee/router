/********************************************************************************
* PathSplitting.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
            foreach (string _ in PathSplitter.Split(Input)) { }
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
}
