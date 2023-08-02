﻿/********************************************************************************
* RouterBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Primitives;

    [TestFixture]
    public class RouterBuilderTests
    {
        private static bool IntParser(string input, string? userData, out object? value)
        {
            if (int.TryParse(input, out int val))
            {
                value = val;
                return true;
            }
            value = null;
            return false;
        }

        private static object? DummyHandler(object request, IReadOnlyDictionary<string, object?> paramz, object userData, string path) => null;

        [TestCase("")]
        [TestCase("/")]
        [TestCase("/cica")]
        [TestCase("/param:int/cica")]
        public void AddRouteShouldThrowOnDuplicateRegistration(string route)
        {
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, new Dictionary<string, TryConvert>
            {
                { "int", IntParser }
            });

            Assert.DoesNotThrow(() => builder.AddRoute(route, DummyHandler));
            Assert.Throws<ArgumentException>(() => builder.AddRoute(route, DummyHandler));          
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
                    "/param:int/cica",
                    "/param:int/kutya"
                };
                yield return new string[]
                {
                    "/",
                    "/param:int/cica",
                    "/param:int/kutya"
                };
                yield return new string[]
                {
                    "/param:int/cica/param2:int",
                    "/param:int/kutya/param2:int"
                };
                yield return new string[]
                {
                    "/",
                    "/param:int/cica/param2:int",
                    "/param:int/kutya/param2:int"
                };
                yield return new string[]
                {
                    "/",
                    "/cica",
                    "/kutya",
                    "/param:int/cica/param2:int",
                    "/param:int/kutya/param2:int"
                };
            }
        }

        [TestCaseSource(nameof(Routes))]
        public void AddRouteShouldBuildTheRouterDelegate(string[] routes)
        {
            RouterBuilder<object, object, object?> builder = new((_, _, _) => { Assert.Fail(); return null; }, new Dictionary<string, TryConvert>
            {
                { "int", IntParser }
            });

            routes.ForEach((route, _) => builder.AddRoute(route, DummyHandler));
            builder.Build();
        }
    }
}