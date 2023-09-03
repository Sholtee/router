/********************************************************************************
* TemplateCompiler.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Router.Perf
{
    [MemoryDiagnoser]
    public class TemplateCompiler
    {
        [Params(1, 2, 5, 10)]
        public int SegmentCount { get; set; }

        [Params(true, false)]
        public bool HasParams { get; set; }

        public RouteTemplateCompiler Compiler { get; set; } = null!;

        public Dictionary<string, object?> Params { get; set; } = null!;

        [GlobalSetup(Target = nameof(Compile))]
        public void SetupTemplate()
        {
            int paramIndex = 0;

            Compiler = RouteTemplate.CreateCompiler
            (
                "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segment", SegmentCount)
                        .Select((segment, i) => HasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment)
                )
            );

            Params = Enumerable
                .Repeat("paramValue", paramIndex)
                .ToDictionary(_ => $"param{--paramIndex}", val => (object?) val);
        }

        [Benchmark]
        public void Compile() => Compiler(Params);
    }
}
