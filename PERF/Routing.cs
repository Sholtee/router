﻿/********************************************************************************
* Routing.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
            RouterBuilder bldr = new(handler: (_, _) => true);

            int paramIndex = 0;

            bldr.AddRoute
            (
                "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segment", SegmentCount)
                        .Select((segment, i) => HasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment)
                ),
                handler: (_, _) => true
            );

            Router = bldr.Build();

            Input = "/" + string.Join("/", Enumerable.Repeat("segment", SegmentCount));
        }

        [Benchmark]
        public void Route() => Router(null, Input);
    }
}