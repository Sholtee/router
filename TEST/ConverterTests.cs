/********************************************************************************
* ConverterTests.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    public class ConverterTests
    {
        [TestCase("1986", null, 1986)]
        [TestCase("7C2", "X", 1986)]
        [TestCase("7C2", "x", 1986)]
        public void IntCoverterShouldParse(string input, string? style, int value)
        {
            Assert.That(new IntConverter(style).ConvertToValue(input, out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "X")]
        [TestCase("INVALID", "x")]
        public void IntConverterShouldRejectInvalidValues(string input, string? style)
        {
            Assert.False(new IntConverter(style).ConvertToValue(input, out object? val));
            Assert.That(val, Is.Null);
        }

        [Test]
        public void IntConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new IntConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);

        [Test]
        public void StrCoverterShouldParse([Values("1986")] string input)
        {
            Assert.That(new StrConverter(null).ConvertToValue(input, out object? val));
            Assert.That(val, Is.EqualTo(input));
        }

        [Test]
        public void StrConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new StrConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);

        private static readonly Guid TestGuid = Guid.Parse("D6B6D5B5-826E-4362-A19A-219997E6D693");

        [TestCase("D6B6D5B5-826E-4362-A19A-219997E6D693", "D")]
        [TestCase("D6B6D5B5826E4362A19A219997E6D693", "N")]
        [TestCase("D6B6D5B5826E4362A19A219997E6D693", null)]
        public void GuidCoverterShouldParse(string input, string? style)
        {
            Assert.That(new GuidConverter(style).ConvertToValue(input, out object? val));
            Assert.That(val, Is.EqualTo(TestGuid));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "N")]
        [TestCase("INVALID", "D")]
        public void GuidConverterShouldRejectInvalidValues(string input, string? style)
        {
            Assert.False(new GuidConverter(style).ConvertToValue(input, out object? val));
            Assert.That(val, Is.Null);
        }

        [Test]
        public void GuidConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new GuidConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);

        public enum MyEnum 
        {
            Default,
            Value
        }

        [TestCase("Value", MyEnum.Value)]
        [TestCase("Default", MyEnum.Default)]
        public void EnumCoverterShouldParse(string input, MyEnum value)
        {
            Assert.That(new EnumConverter(typeof(MyEnum).FullName).ConvertToValue(input, out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [Test]
        public void EnumConverterShouldRejectInvalidValues()
        {
            Assert.False(new EnumConverter(typeof(MyEnum).FullName).ConvertToValue("INVALID", out object? val));
            Assert.That(val, Is.Null);
        }

        [Test]
        public void EnumConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new EnumConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);
    }
}