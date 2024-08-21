/********************************************************************************
* AsyncRouterBuilderAddRouteExtensionsTests.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Router.Extensions.Tests
{
    [TestFixture]
    public class AsyncRouterBuilderAddRouteExtensionsTests
    {
        private class MyService
        {
            public void MyHandler() { }
        }

        [Test]
        public void AddRouteShouldBeNullChecked()
        {
            AsyncRouterBuilder bldr = AsyncRouterBuilder.Create();

            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute(route: "/", handler: null!));
            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute(route: RouteTemplate.Parse("/"), handler: null!));
        }
    }
}