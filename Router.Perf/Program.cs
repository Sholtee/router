/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Solti.Utils.Router.Perf
{
    class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run
        (
            args
#if DEBUG
            , new DebugInProcessConfig()
#endif
        );
    }
}
