/********************************************************************************
* RouterDelegateTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    [TestFixture]
    public class RouterDelegateStressTests
    {
        [Test]
        public void CarryOutStressTest([Values(1, 5, 10, 100, 500)] int routeCount, [Values(true, false)] bool hasParams)
        {
            RouterBuilder bldr = new();

            for (int i = 0; i < routeCount; i++)
            {
                int paramIndex = 0;
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segment", i)
                        .Select((segment, i) => hasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment + i)
                );

                int capture = i;

                bldr.AddRoute(route, handler: (paramz, _) => $"result{capture}");
            }

            Router router = bldr.Build();

            Parallel.For(0, routeCount, i =>
            {
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable.Repeat("segment", i).Select(static (segment, k) => segment + k)
                );

                Assert.That(router(null, route.AsSpan(), "GET".AsSpan()), Is.EqualTo($"result{i}"));
            });
        }

        [Test]
        public void CarryOutStressTestAsync([Values(1, 5, 10, 100, 500)] int routeCount, [Values(true, false)] bool hasParams)
        {
            AsyncRouterBuilder bldr = AsyncRouterBuilder.Create();

            for (int i = 0; i < routeCount; i++)
            {
                int paramIndex = 0;
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable
                        .Repeat("segment", i)
                        .Select((segment, i) => hasParams && i % 2 == 0 ? $"{{param{paramIndex++}:str}}" : segment + i)
                );

                int capture = i;

                bldr.AddRoute(route, handler: (paramz, _) => $"result{capture}");
            }

            AsyncRouter router = bldr.Build();

            Parallel.For(0, routeCount, i =>
            {
                string route = "/" + string.Join
                (
                    "/",
                    Enumerable.Repeat("segment", i).Select(static (segment, k) => segment + k)
                );

                Assert.That(router(null, route.AsSpan(), "GET".AsSpan()).GetAwaiter().GetResult(), Is.EqualTo($"result{i}"));
            });
        }
    }
}