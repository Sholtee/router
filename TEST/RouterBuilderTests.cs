/********************************************************************************
* RouterBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Primitives;

    [TestFixture]
    public class RouterBuilderTests
    {
        private static object? DummyHandler(IReadOnlyDictionary<string, object?> paramz, object? userData) => null;

        [TestCase("")]
        [TestCase("/")]
        [TestCase("/cica")]
        [TestCase("/{param:int}/cica")]
        public void AddRouteShouldThrowOnDuplicateRegistrationWhenMethodsAreSame(string route)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(route, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(route, DummyHandler));
        }

        [TestCase("/{param:int}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int}", "/cica/{param2:int}")]
        public void AddRouteShouldThrowOnDuplicateRegistrationWhenMethodsAreSame(string a, string b)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(a, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(b, DummyHandler));
        }

        [TestCase("/{param:int}/cica", "/{param:int}/cica")]
        [TestCase("/{param:int}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int}", "/cica/{param2:int}")]
        [TestCase("/{param:int:x}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int:X}", "/cica/{param2:int}")]
        public void AddRouteShouldNotThrowOnDuplicateRegistrationWhenMethodsAreDifferent(string a, string b)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(a, DummyHandler, "GET"));
            Assert.DoesNotThrow(() => builder.AddRoute(b, DummyHandler, "POST"));
            Assert.DoesNotThrow(() => builder.Build());
        }

        [Test]
        public void AddRouteShouldThrowOnMissingConverter()
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, new Dictionary<string, ConverterFactory>(0));

            Assert.Throws<ArgumentException>(() => builder.AddRoute("/{param:int}/cica", DummyHandler));
        }

        [Test]
        public void CtorShouldThrowOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RouterBuilder(handler: null!));
            Assert.Throws<ArgumentNullException>(() => new RouterBuilder(handlerExpr: null!));
        }

        public static IEnumerable<object?[]> NullCases
        {
            get
            {
                yield return new object?[] { null, null, null };
                yield return new object?[] { "path", null, new string[] { "GET" } };
                yield return new object?[] { null, (RequestHandler) ((_, _) => false), new string[] { "GET" } };
                yield return new object?[] { "path", (RequestHandler) ((_, _) => false), null };
                yield return new object?[] { "path", (RequestHandler)((_, _) => false), new string[] { null! } };
            }
        }

        [TestCaseSource(nameof(NullCases))]
        public void AddRouteShouldThrowOnNull(string route, RequestHandler handler, string[] methods)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, new Dictionary<string, ConverterFactory>(0));

            ArgumentException? ex = Assert.Catch<ArgumentException>(() => builder.AddRoute(route, handler, methods));
            Assert.NotNull(ex);
        }

        public static IEnumerable<object?[]> NullCasesExpr
        {
            get
            {
                yield return new object?[] { null, null, null };
                yield return new object?[] { "path", null, new string[] { "GET" } };
                yield return new object?[] { null, (Expression<RequestHandler>) ((_, _) => false), new string[] { "GET" } };
                yield return new object?[] { "path", (Expression<RequestHandler>) ((_, _) => false), null };
            }
        }

        [TestCaseSource(nameof(NullCasesExpr))]
        public void AddRouteExprShouldThrowOnNull(string route, Expression<RequestHandler> handler, string[] methods)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, new Dictionary<string, ConverterFactory>(0));

            Assert.Throws<ArgumentNullException>(() => builder.AddRoute(route, handler, methods));
        }

        public static IEnumerable<string[]> Routes
        {
            get
            {
                yield return new string[]
                {
                    "/cica",
                    "/kutya"
                };
                yield return new string[]
                {
                    "/",
                    "/cica",
                    "/kutya"
                };
                yield return new string[]
                {
                    "/{param:int}/cica",
                    "/{param:int}/kutya"
                };
                yield return new string[]
                {
                    "/",
                    "/{param:int}/cica",
                    "/{param:int}/kutya"
                };
                yield return new string[]
                {
                    "/{param:int}/cica/{param2:int}",
                    "/{param:int}/kutya/{param2:int}"
                };
                yield return new string[]
                {
                    "/",
                    "/{param:int}/cica/{param2:int}",
                    "/{param:int}/kutya/{param2:int}"
                };
                yield return new string[]
                {
                    "/",
                    "/cica",
                    "/kutya",
                    "/{param:int}/cica/{param2:int}",
                    "/{param:int}/kutya/{param2:int}"
                };
            }
        }

        [TestCaseSource(nameof(Routes))]
        public void AddRouteShouldBuildTheRouterDelegate(string[] routes)
        {
            RouterBuilder builder = new(handler: (_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            routes.ForEach((route, _) => builder.AddRoute(route, DummyHandler));
            Assert.DoesNotThrow(() => builder.Build());
        }
    }
}