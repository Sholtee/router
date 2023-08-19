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
        private static bool WrappedParser(string input, out object? value)
        {
            value = null;
            return false;
        }

        private static bool IntParser(string input, out object? value)
        {
            value = null;
            return false;
        }

        private static bool StrParser(string input, out object? value)
        {
            value = null;
            return false;
        }

        public static IEnumerable<(string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<ConverterFactory>, Mock<ConverterFactory>, Mock<RouteParser>> Assert)> Cases
        {
            get
            {
                yield return
                (
                    "/",
                    new RouteSegment[0] { }, 
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica",
                    new RouteSegment[] 
                    {
                        new RouteSegment("cica", null)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int}/kutya",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser),
                        new RouteSegment("kutya", null)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int:}",
                    new RouteSegment[] 
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/pre-{param:int}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", WrappedParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, mockParser) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                        mockParser.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "", (TryConvert) IntParser);
                    }
                );
                yield return
                (
                    "/cica/{param:int}-su",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", WrappedParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, mockParser) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                        mockParser.Protected().Verify<TryConvert>("Wrap", Times.Once(), "", "-su", (TryConvert) IntParser);
                    }
                );
                yield return
                (
                    "/cica/pre-{param:int}-su",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", WrappedParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, mockParser) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                        mockParser.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "-su", (TryConvert) IntParser);
                    }
                );
                yield return
                (
                    "/cica/pre-{param:int}-su/kutya",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", WrappedParser),
                        new RouteSegment("kutya", null)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, mockParser) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                        mockParser.Protected().Verify<TryConvert>("Wrap", Times.Once(), "pre-", "-su", (TryConvert) IntParser);
                    }
                );
                yield return
                (
                    "/cica/{param:int:x}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke("x"), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int}/mica/{param2:str}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser),
                        new RouteSegment("mica", null),
                        new RouteSegment("param2", StrParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int}/{param2:str}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", IntParser),
                        new RouteSegment("param2", StrParser)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory, _) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void ParseShouldBreakDownTheInputToProperSegments((string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<ConverterFactory>, Mock<ConverterFactory>, Mock<RouteParser>> Assert) @case)
        {
            Mock<ConverterFactory> mockIntConverterFactory = new(MockBehavior.Strict);
            mockIntConverterFactory
                .Setup(x => x.Invoke(It.IsAny<string?>()))
                .Returns(IntParser);

            Mock<ConverterFactory> mockStrConverterFactory = new(MockBehavior.Strict);
            mockStrConverterFactory
                .Setup(x => x.Invoke(It.IsAny<string?>()))
                .Returns(StrParser);

            Mock<RouteParser> mockParser = new
            (
                MockBehavior.Strict,
                new Dictionary<string, ConverterFactory> 
                {
                    { "int", mockIntConverterFactory.Object },
                    { "str", mockStrConverterFactory.Object }
                }
            );
            mockParser
                .Protected()
                .Setup<TryConvert>("Wrap", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<TryConvert>())
                .Returns(WrappedParser);

            Assert.That(mockParser.Object.Parse(@case.Route).SequenceEqual(@case.Parsed));
            @case.Assert(mockIntConverterFactory, mockStrConverterFactory, mockParser);
        }

        [Test]
        public void ParseShouldThrowOnMissingConverter([Values("{param}", "{param:}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnMissingParameterName([Values("{}", "{:int}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.CANNOT_BE_NULL);

        [Test]
        public void ParseShouldThrowOnDuplicateParameterName([Values("{param:int}/{param:int}", "{param:int}/pre-{param:int}", "{param:int}/segment/{param:int}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(DefaultConverters.Instance).Parse(input).ToList(), Resources.DUPLICATE_PARAMETER);

        [Test]
        public void ParseShouldThrowOnNonregisteredConverter([Values("{param:cica}")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.CONVERTER_NOT_FOUND);

        [Test]
        public void ParseShouldThrowOnMultipleParameters([Values("{param:int}{param2:int}", "pre-{param:int}-{param2:int}", "{param:int}-{param2:int}-su", "pre-{param:int}-{param2:int}-su")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.TOO_MANY_PARAM_DESCRIPTOR);

        [TestCase("pre-{param:int}",   null, "pre-16",  true)]
        [TestCase("pre-{param:int:x}", "x",  "pre-16",  true)]
        [TestCase("pre-{param:int}",   null, "prex-16", false)]
        [TestCase("pre-{param:int}",   null, "16",      false)]

        [TestCase("{param:int}-suf",   null, "16-suf",  true)]
        [TestCase("{param:int:x}-suf", "x",  "16-suf",  true)]
        [TestCase("{param:int}-suf",   null, "16-sufx", false)]
        [TestCase("{param:int}-suf",   null, "16",      false)]

        [TestCase("pre-{param:int}-suf",   null, "pre-16-suf",  true)]
        [TestCase("pre-{param:int:x}-suf", "x",  "pre-16-suf",  true)]
        [TestCase("pre-{param:int}-suf",   null, "prex-16-suf", false)]
        [TestCase("pre-{param:int}-suf",   null, "pre-16-sufx", false)]
        [TestCase("pre-{param:int}-suf",   null, "16",          false)]
        public void WrapperShouldValidate(string input, string? userData, string test, bool shouldCallOriginal)
        {
            object? ret;

            Mock<TryConvert> intParser = new(MockBehavior.Strict);
            intParser
                .Setup(x => x.Invoke("16", out ret))
                .Returns(true);

            Mock<ConverterFactory> intParserFactory = new(MockBehavior.Strict);
            intParserFactory
                .Setup(x => x.Invoke(userData))
                .Returns(intParser.Object);

            RouteParser parser = new(new Dictionary<string, ConverterFactory>
            {
                { "int", intParserFactory.Object }
            });

            Assert.That(parser.Parse(input).Single().Converter!.Invoke(test, out ret), Is.EqualTo(shouldCallOriginal));

            if (shouldCallOriginal)
                intParser.Verify(x => x.Invoke("16", out ret), Times.Once);
        }
    }
}