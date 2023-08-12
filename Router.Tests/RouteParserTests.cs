/********************************************************************************
* RouteParserTests.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    internal class RouteParserTests
    {
        private static bool IntParser(string input, string? userData, out object? value)
        {
            value = null;
            return false;
        }

        private static bool StrParser(string input, string? userData, out object? value)
        {
            value = null;
            return false;
        }

        public static IEnumerable<(string Route, IEnumerable<RouteSegment> Parsed)> Cases
        {
            get
            {
                yield return ("/", new RouteSegment[0] {  });
                yield return ("/cica", new RouteSegment[] { new RouteSegment("cica", null, null) });
                yield return ("/cica/{param:int}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null) });
                yield return ("/cica/{param:int:}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null) });
                yield return ("/cica/pre-{param:int}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", RouteParser.Wrap("pre", "", IntParser, StringComparison.OrdinalIgnoreCase), null) });
                yield return ("/cica/{param:int}-su", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", RouteParser.Wrap("", "su", IntParser, StringComparison.OrdinalIgnoreCase), null) });
                yield return ("/cica/pre-{param:int}-su", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", RouteParser.Wrap("pre", "su", IntParser, StringComparison.OrdinalIgnoreCase), null) });
                yield return ("/cica/{param:int:x}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, "x") });
                yield return ("/cica/{param:int}/mica/{param2:str}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null), new RouteSegment("mica", null, null), new RouteSegment("param2", StrParser, null) });
                yield return ("/cica/{param:int}/{param2:str}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null), new RouteSegment("param2", StrParser, null) });

            }
        }

        [TestCaseSource(nameof(Cases))]
        public void ParseShouldBreakDownTheInputToProperSegments((string Route, IEnumerable<RouteSegment> Parsed) @case) => Assert.That
        (
            new RouteParser
            (
                new Dictionary<string, TryConvert>
                {
                    { "int", IntParser },
                    { "str", StrParser }
                }
            ).Parse(@case.Route).SequenceEqual(@case.Parsed)
        );

        [Test]
        public void ParseShouldThrowOnMissingConverter([Values("{param}", "{param:}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnMissingParameterName([Values("{}", "{:int}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnNonregisteredConverter([Values("{param:cica}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CONVERTER_NOT_FOUND);

        [Test]
        public void ParseShouldThrowOnMultipleParameters([Values("{param:int}{param2:int}", "pre-{param:int}-{param2:int}", "{param:int}-{param2:int}-su", "pre-{param:int}-{param2:int}-su")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.TOO_MANY_PARAM_DESCRIPTOR);
    }
}