/********************************************************************************
* Routing.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Router.Perf
{
    [MemoryDiagnoser]
    public class Routing
    {
        [Params(1, 2, 5, 10)]
        public int SegmentCount { get; set; }

        [Params(true, false)]
        public bool HasParams { get; set; }

        public string Input { get; set; } = null!;

        public Router Router { get; set; } = null!;

        [GlobalSetup(Target = nameof(Route))]
        public void SetupRoute()
        {
            RouterBuilder bldr = new((_, _) => true, new Dictionary<string, ConverterFactory>
            {
                { "str", _ => StringConverter }
            });

            int paramIndex = 0;

            bldr.AddRoute
            (
                "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segemnt", SegmentCount)
                        .Select((segment, i) => HasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment)
                ),
                (_, _, _) => true
            );

            Router = bldr.Build();

            Input = "/" + string.Join("/", Enumerable.Repeat("segemnt", SegmentCount));

            static bool StringConverter(string input, out object? val)
            {
                val = input;
                return true;
            }
        }

        [Benchmark]
        public void Route() => Router(null, Input);
    }
}
