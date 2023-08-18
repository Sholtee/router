/********************************************************************************
* RouterBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Primitives;

    [TestFixture]
    public class RouterBuilderTests
    {
        private static object? DummyHandler(IReadOnlyDictionary<string, object?> paramz, object? userData, string path) => null;

        [TestCase("")]
        [TestCase("/")]
        [TestCase("/cica")]
        [TestCase("/{param:int}/cica")]
        public void AddRouteShouldThrowOnDuplicateRegistration(string route)
        {
            RouterBuilder builder = new((_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(route, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(route, DummyHandler));
        }

        [TestCase("/{param:int}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int}", "/cica/{param2:int}")]
        [TestCase("/{param:int:x}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int:X}", "/cica/{param2:int}")]
        public void AddRouteShouldThrowOnDuplicateRegistration(string a, string b)
        {
            RouterBuilder builder = new((_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(a, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(b, DummyHandler));
        }

        [Test]
        public void AddRouteShouldThrowOnMissingConverter()
        {
            RouterBuilder builder = new((_, _) => { Assert.Fail(); return null; }, new Dictionary<string, ConverterFactory>(0));

            Assert.Throws<ArgumentException>(() => builder.AddRoute("/{param:int}/cica", DummyHandler));
        }

        [Test]
        public void CtorShouldThrowOnNull() => Assert.Throws<ArgumentNullException>(() => new RouterBuilder(null!));

        public static IEnumerable<object?[]> NullCases
        {
            get
            {
                yield return new object?[] { null, null };
                yield return new object?[] { "path", null };
                yield return new object?[] { null, (RequestHandler)((_, _, _) => false) };
            }
        }

        [TestCaseSource(nameof(NullCases))]
        public void AddRouteShouldThrowOnNull(string route, RequestHandler handler)
        {
            RouterBuilder builder = new((_, _) => { Assert.Fail(); return null; }, new Dictionary<string, ConverterFactory>(0));

            Assert.Throws<ArgumentNullException>(() => builder.AddRoute(route, handler));
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
            RouterBuilder builder = new((_, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            routes.ForEach((route, _) => builder.AddRoute(route, DummyHandler));
            builder.Build();
        }
    }
}