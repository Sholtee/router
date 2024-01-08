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

            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute<MyService>(route: (string) null!, handlerExpr: svc => svc.MyHandler(), SplitOptions.Default));
            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute<MyService>(route: (ParsedRoute) null!, handlerExpr: svc => svc.MyHandler()));

            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute<MyService>(route: "/", handlerExpr: null!, SplitOptions.Default));
            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute<MyService>(route: RouteTemplate.Parse("/"), handlerExpr: null!));

            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute(route: "/", handler: null!));
            Assert.Throws<ArgumentNullException>(() => bldr.AddRoute(route: RouteTemplate.Parse("/"), handler: null!));
        }
    }
}