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
            Assert.That(DefaultConverters.IntConverter(input, style, out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "X")]
        [TestCase("INVALID", "x")]
        public void IntConverterShouldRejectInvalidValues(string input, string? style)
        {
            Assert.False(DefaultConverters.IntConverter(input, style, out object? val));
            Assert.That(val, Is.Null);
        }

        [Test]
        public void IntConverterShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => DefaultConverters.IntConverter("1986", "INVALID", out object? val), Resources.INVALID_FORMAT_STYLE);

        [TestCase("1986", null)]
        public void StrCoverterShouldParse(string input, string? style)
        {
            Assert.That(DefaultConverters.StrConverter(input, style, out object? val));
            Assert.That(val, Is.EqualTo(input));
        }

        [Test]
        public void StrConverterShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => DefaultConverters.StrConverter("1986", "INVALID", out object? val), Resources.INVALID_FORMAT_STYLE);
    }
}