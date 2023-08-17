/********************************************************************************
* ConverterTests.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Properties;

    [TestFixture]
    public class ConverterTests
    {
        [TestCase("1986", null, 1986)]
        [TestCase("7C2", "X", 1986)]
        [TestCase("7C2", "x", 1986)]
        public void IntCoverterShouldParse(string input, string? style, int value)
        {
            Assert.That(DefaultConverters.IntConverterFactory(style)(input, out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "X")]
        [TestCase("INVALID", "x")]
        public void IntConverterShouldRejectInvalidValues(string input, string? style)
        {
            Assert.False(DefaultConverters.IntConverterFactory(style)(input, out object? val));
            Assert.That(val, Is.Null);
        }

        [Test]
        public void IntConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => DefaultConverters.IntConverterFactory("INVALID"), Resources.INVALID_FORMAT_STYLE);

        [Test]
        public void StrCoverterShouldParse([Values("1986")] string input)
        {
            Assert.That(DefaultConverters.StrConverterFactory(null)(input, out object? val));
            Assert.That(val, Is.EqualTo(input));
        }

        [Test]
        public void StrConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => DefaultConverters.IntConverterFactory("INVALID"), Resources.INVALID_FORMAT_STYLE);
    }
}