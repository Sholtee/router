/********************************************************************************
* RouteParserTests.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using Moq.Protected;
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

        private static bool WrappedParser(string input, string? userData, out object? value)
        {
            value = null;
            return false;
        }

        public static IEnumerable<(string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<RouteParser>>? Assert)> Cases
        {
            get
            {
                yield return ("/", new RouteSegment[0] { }, null);
                yield return ("/cica", new RouteSegment[] { new RouteSegment("cica", null, null) }, null);
                yield return ("/cica/{param:int}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null) }, null);
                yield return ("/cica/{param:int}/kutya", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null), new RouteSegment("kutya", null, null) }, null);
                yield return ("/cica/{param:int:}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null) }, null);
                yield return ("/cica/pre-{param:int}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", WrappedParser, null) }, mock => mock.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "", (TryConvert)IntParser));
                yield return ("/cica/{param:int}-su", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", WrappedParser, null) }, mock => mock.Protected().Verify<TryConvert>("Wrap", Times.Once(), "", "-su", (TryConvert)IntParser));
                yield return ("/cica/pre-{param:int}-su", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", WrappedParser, null) }, mock => mock.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "-su", (TryConvert)IntParser));
                yield return ("/cica/pre-{param:int}-su/kutya", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", WrappedParser, null), new RouteSegment("kutya", null, null) }, mock => mock.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "-su", (TryConvert)IntParser));
                yield return ("/cica/{param:int:x}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, "x") }, null);
                yield return ("/cica/{param:int}/mica/{param2:str}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null), new RouteSegment("mica", null, null), new RouteSegment("param2", StrParser, null) }, null);
                yield return ("/cica/{param:int}/{param2:str}", new RouteSegment[] { new RouteSegment("cica", null, null), new RouteSegment("param", IntParser, null), new RouteSegment("param2", StrParser, null) }, null);
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void ParseShouldBreakDownTheInputToProperSegments((string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<RouteParser>>? Assert) @case)
        {
            Mock<RouteParser> mockParser = new
            (
                MockBehavior.Strict,
                new Dictionary<string, TryConvert>
                {
                    { "int", IntParser },
                    { "str", StrParser }
                },
                StringComparison.OrdinalIgnoreCase
            );
            mockParser
                .Protected()
                .Setup<TryConvert>("Wrap", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<TryConvert>())
                .Returns(WrappedParser);

            Assert.That(mockParser.Object.Parse(@case.Route).SequenceEqual(@case.Parsed));
            @case.Assert?.Invoke(mockParser);
        }

        [Test]
        public void ParseShouldThrowOnMissingConverter([Values("{param}", "{param:}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnMissingParameterName([Values("{}", "{:int}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnDuplicateParameterName([Values("{param:int}/{param:int}", "{param:int}/pre-{param:int}", "{param:int}/segment/{param:int}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert> { { "int", new Mock<TryConvert>().Object } }).Parse(input).ToList(), Resources.DUPLICATE_PARAMETER);

        [Test]
        public void ParseShouldThrowOnNonregisteredConverter([Values("{param:cica}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.CONVERTER_NOT_FOUND);

        [Test]
        public void ParseShouldThrowOnMultipleParameters([Values("{param:int}{param2:int}", "pre-{param:int}-{param2:int}", "{param:int}-{param2:int}-su", "pre-{param:int}-{param2:int}-su")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, TryConvert>(0)).Parse(input).ToList(), Resources.TOO_MANY_PARAM_DESCRIPTOR);

        [TestCase("pre-{param:int}", null, "pre-16", true)]
        [TestCase("pre-{param:int}", "x", "pre-16", true)]
        [TestCase("pre-{param:int}", null, "prex-16", false)]
        [TestCase("pre-{param:int}", null, "16", false)]

        [TestCase("{param:int}-suf", null, "16-suf", true)]
        [TestCase("{param:int}-suf", "x", "16-suf", true)]
        [TestCase("{param:int}-suf", null, "16-sufx", false)]
        [TestCase("{param:int}-suf", null, "16", false)]

        [TestCase("pre-{param:int}-suf", null, "pre-16-suf", true)]
        [TestCase("pre-{param:int}-suf", "x", "pre-16-suf", true)]
        [TestCase("pre-{param:int}-suf", null, "prex-16-suf", false)]
        [TestCase("pre-{param:int}-suf", null, "pre-16-sufx", false)]
        [TestCase("pre-{param:int}-suf", null, "16", false)]
        public void WrapperShouldValidate(string input, string? userData, string test, bool shouldCallOriginal)
        {
            object? ret;

            Mock<TryConvert> intParser = new(MockBehavior.Strict);
            intParser
                .Setup(x => x.Invoke("16", userData, out ret))
                .Returns(true);

            RouteParser parser = new(new Dictionary<string, TryConvert>
            {
                { "int", intParser.Object }
            });

            Assert.That(parser.Parse(input).Single().Converter!.Invoke(test, userData, out ret), Is.EqualTo(shouldCallOriginal));

            if (shouldCallOriginal)
                intParser.Verify(x => x.Invoke("16", userData, out ret), Times.Once);
        }
    }
}