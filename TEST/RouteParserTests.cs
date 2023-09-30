/********************************************************************************
* RouteParserTests.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    internal class RouteParserTests
    {
        public static IEnumerable<(string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<ConverterFactory>, Mock<ConverterFactory>> Assert)> Cases
        {
            get
            {
                yield return
                (
                    "/",
                    new RouteSegment[0] { }, 
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new IntConverter(null))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new IntConverter(null)),
                        new RouteSegment("kutya", null)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new IntConverter(null))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new ConverterWrapper(new IntConverter(null), "pre-", ""))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int}-su",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", new ConverterWrapper(new IntConverter(null), "", "-su"))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/pre-{param:int}-su",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", new ConverterWrapper(new IntConverter(null), "pre-", "-su"))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/pre-{param:int}-su/kutya",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", new ConverterWrapper(new IntConverter(null), "pre-", "-su")),
                        new RouteSegment("kutya", null)
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
                    {
                        mockIntConverterFactory.Verify(x => x.Invoke(null), Times.Once);
                        mockIntConverterFactory.VerifyNoOtherCalls();
                        mockStrConverterFactory.VerifyNoOtherCalls();
                    }
                );
                yield return
                (
                    "/cica/{param:int:x}",
                    new RouteSegment[]
                    {
                        new RouteSegment("cica", null),
                        new RouteSegment("param", new IntConverter("x"))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new IntConverter(null)),
                        new RouteSegment("mica", null),
                        new RouteSegment("param2", new StrConverter(null))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
                        new RouteSegment("param", new IntConverter(null)),
                        new RouteSegment("param2", new StrConverter(null))
                    },
                    (mockIntConverterFactory, mockStrConverterFactory) =>
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
        public void ParseShouldBreakDownTheInputToProperSegments((string Route, IEnumerable<RouteSegment> Parsed, Action<Mock<ConverterFactory>, Mock<ConverterFactory>> Assert) @case)
        {
            Mock<ConverterFactory> mockIntConverterFactory = new(MockBehavior.Strict);
            mockIntConverterFactory
                .Setup(x => x.Invoke(It.IsAny<string?>()))
                .Returns<string>(style => new IntConverter(style));

            Mock<ConverterFactory> mockStrConverterFactory = new(MockBehavior.Strict);
            mockStrConverterFactory
                .Setup(x => x.Invoke(It.IsAny<string?>()))
                .Returns<string>(style => new StrConverter(style));

            Assert.That
            (
                new RouteParser
                (
                    new Dictionary<string, ConverterFactory>
                    {
                        { "int", mockIntConverterFactory.Object },
                        { "str", mockStrConverterFactory.Object }
                    }
                )
                .Parse(@case.Route)
                .SequenceEqual(@case.Parsed)
            );
            @case.Assert(mockIntConverterFactory, mockStrConverterFactory);
        }

        [Test]
        public void ParseShouldThrowOnMissingConverter([Values("{param}", "{param:}", "pre-{param}", "pre-{param:}", "{param}-su", "{param:}-su", "pre-{param}-su", "pre-{param:}-su")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.INVALID_TEMPLATE);

        [Test]
        public void ParseShouldThrowOnMissingParameterName([Values("{}", "{:int}", "pre-{}", "pre-{:int}", "{}-su", "{:int}-su", "pre-{}-su", "pre-{:int}-su")] string input) =>
            Assert.Throws<ArgumentException>(() => new RouteParser(new Dictionary<string, ConverterFactory>(0)).Parse(input).ToList(), Resources.INVALID_TEMPLATE);

        [Test]
        public void ParseShouldThrowOnDuplicateParameterName([Values("{param:int}/{param:int}", "{param:int}/{param:str}", "{param:int}/pre-{param:int}", "{param:int}/{param:int}-su", "{param:int}/pre-{param:int}-su", "{param:int}/segment/{param:int}")] string input) =>
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

            Mock<IConverter> intConverter = new(MockBehavior.Strict);
            intConverter
                .Setup(x => x.ConvertToValue("16", out ret))
                .Returns(true);
            intConverter
                .SetupGet(x => x.Id)
                .Returns("IntConverter");
            intConverter
                .SetupGet(x => x.Type)
                .Returns(typeof(int));

            Mock<ConverterFactory> intConverterFactory = new(MockBehavior.Strict);
            intConverterFactory
                .Setup(x => x.Invoke(userData))
                .Returns(intConverter.Object);

            RouteParser parser = new(new Dictionary<string, ConverterFactory>
            {
                { "int", intConverterFactory.Object }
            });

            IConverter wrapper = parser.Parse(input).Single().Converter!;

            Assert.That(wrapper, Is.InstanceOf<ConverterWrapper>());
            Assert.That(wrapper!.ConvertToValue(test, out ret), Is.EqualTo(shouldCallOriginal));
            intConverter.Verify(x => x.ConvertToValue("16", out ret), shouldCallOriginal ? Times.Once : Times.Never);
        }
    }
}