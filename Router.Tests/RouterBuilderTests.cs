﻿/********************************************************************************
* RouterBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Primitives;

    [TestFixture]
    public class RouterBuilderTests
    {
        private static object? DummyHandler(object request, IReadOnlyDictionary<string, object?> paramz, object? userData, string path) => null;

        [TestCase("")]
        [TestCase("/")]
        [TestCase("/cica")]
        [TestCase("/{param:int}/cica")]
        public void AddRouteShouldThrowOnDuplicateRegistration(string route)
        {
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(route, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(route, DummyHandler));          
        }

        [TestCase("/{param:int}/cica", "/{param2:int}/cica")]
        [TestCase("/cica/{param:int}", "/cica/{param2:int}")]
        public void AddRouteShouldThrowOnDuplicateRegistration(string a, string b)
        {
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            Assert.DoesNotThrow(() => builder.AddRoute(a, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(b, DummyHandler));
        }

        [Test]
        public void AddRouteShouldThrowOnMissingConverter()
        {
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, new Dictionary<string, TryConvert>(0));

            Assert.Throws<ArgumentException>(() => builder.AddRoute("/{param:int}/cica", DummyHandler));
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
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, DefaultConverters.Instance);

            routes.ForEach((route, _) => builder.AddRoute(route, DummyHandler));
            builder.Build();
        }
    }
}