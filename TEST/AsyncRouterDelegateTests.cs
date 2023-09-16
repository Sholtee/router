/********************************************************************************
* AsyncRouterDelegateTests.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Properties;

    [TestFixture]
    public class AsyncRouterDelegateTests
    {
        public static IEnumerable<(Action<string, AsyncRouterBuilder>, object?)> Cases
        {
            get
            {
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => Task.FromResult(1986)), 1986);
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => Task.CompletedTask), null);
                yield return ((route, builder) => builder.AddRoute(route, handler: (paramz, userData) => 1986), 1986);
            }
        }

        [Test]
        public async Task DelegateShouldRoute([ValueSource(nameof(Cases))] (Action<string, AsyncRouterBuilder> Registrar, object? Epxected) @case)
        {
            AsyncRouterBuilder builder = AsyncRouterBuilder.Create(handler: (_, _) => false, DefaultConverters.Instance);
            @case.Registrar("/cica", builder);

            RouterAsync router = builder.Build();

            Assert.That(await router(null, "/"), Is.False);
            Assert.That(await router(null, "/cica"), Is.EqualTo(@case.Epxected));
        }

        [Test]
        public void DelegateShouldThrowOnUnregisteredRoute()
        {
            RouterAsync router = AsyncRouterBuilder.Create().Build();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await router(null, "/cica"), Resources.ROUTE_NOT_REGISTERED);
        }
    }
}