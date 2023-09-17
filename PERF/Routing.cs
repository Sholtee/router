﻿/********************************************************************************
* Routing.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading.Tasks;

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

        private string CreateTemplate()
        {
            int paramIndex = 0;

            return "/" + string.Join
            (
                "/",
                Enumerable
                    .Repeat("segment", SegmentCount)
                    .Select((segment, i) => HasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment)
            );
        }

        private void SetInput() => Input = "/" + string.Join("/", Enumerable.Repeat("segment", SegmentCount));

        [GlobalSetup(Target = nameof(Route))]
        public void SetupRoute()
        {
            RouterBuilder bldr = new(handler: (_, _) => true);

            bldr.AddRoute(CreateTemplate(), handler: (_, _) => true);

            Router = bldr.Build();

            SetInput();
        }

        [Benchmark]
        public void Route() => Router(null, Input);

        public AsyncRouter AsyncRouter { get; set; } = null!;

        [GlobalSetup(Target = nameof(AsyncRoute))]
        public void SetupAsyncRoute()
        {
            AsyncRouterBuilder bldr = AsyncRouterBuilder.Create(handler: (_, _) => Task.FromResult(true));

            bldr.AddRoute(CreateTemplate(), handler: (_, _) => Task.FromResult(true));

            AsyncRouter = bldr.Build();

            SetInput();
        }

        [Benchmark]
        public async Task<object?> AsyncRoute() => await AsyncRouter(null, Input);
    }
}
