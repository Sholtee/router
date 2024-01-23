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
            Assert.That(new IntConverter(style).ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("1986", null, 1986)]
        [TestCase("7C2", "X", 1986)]
        [TestCase("7c2", "x", 1986)]
        public void IntCoverterShouldStringify(string value, string? style, int input)
        {
            Assert.That(new IntConverter(style).ConvertToString(input, out string? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "X")]
        [TestCase("INVALID", "x")]
        public void IntConverterShouldRejectInvalidValues(string input, string? style)
        {
            IConverter converter = new IntConverter(style);

            Assert.False(converter.ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.Null);

            Assert.False(converter.ConvertToString(input, out string? str));
            Assert.That(str, Is.Null);
        }

        [Test]
        public void IntConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new IntConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);

        [Test]
        public void StrCoverterShouldParse([Values("1986")] string input)
        {
            Assert.That(new StrConverter(null).ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.EqualTo(input));
        }

        [Test]
        public void StrCoverterShouldStringify([Values("1986")] string input)
        {
            Assert.That(new StrConverter(null).ConvertToString(input, out string? val));
            Assert.That(val, Is.EqualTo(input));
        }

        [Test]
        public void StrCoverterShouldRejectInvalidValue()
        {
            Assert.False(new StrConverter(null).ConvertToString(1, out string? val));
            Assert.That(val, Is.Null);
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
            Assert.That(new GuidConverter(style).ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.EqualTo(TestGuid));
        }

        [TestCase("d6b6d5b5-826e-4362-a19a-219997e6d693", "D")]
        [TestCase("d6b6d5b5826e4362a19a219997e6d693", "N")]
        [TestCase("d6b6d5b5826e4362a19a219997e6d693", null)]
        public void GuidCoverterShouldStringify(string expected, string? style)
        {
            Assert.That(new GuidConverter(style).ConvertToString(TestGuid, out string? val));
            Assert.That(val, Is.EqualTo(expected));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "N")]
        [TestCase("INVALID", "D")]
        public void GuidConverterShouldRejectInvalidValues(string input, string? style)
        {
            IConverter converter = new GuidConverter(style);

            Assert.False(converter.ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.Null);

            Assert.False(converter.ConvertToString(input, out string? str));
            Assert.That(str, Is.Null);
        }

        private static readonly DateTime TestDate = DateTime.ParseExact("2009-06-15T13:45:30", "s", null);

        [TestCase("2009-06-15T13:45:30", "s")]
        [TestCase("2009-06-15 13:45:30Z", "u")]
        [TestCase("2009-06-15T13:45:30", null)]
        public void DateCoverterShouldParse(string input, string? style)
        {
            Assert.That(new DateConverter(style).ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.EqualTo(TestDate).Using<DateTime>(DateTime.Compare));
        }

        [TestCase("2009-06-15T13:45:30", "s")]
        [TestCase("2009-06-15 13:45:30Z", "u")]
        [TestCase("2009-06-15T13:45:30", null)]
        public void DateCoverterShouldStringify(string expected, string? style)
        {
            Assert.That(new DateConverter(style).ConvertToString(TestDate, out string? val));
            Assert.That(val, Is.EqualTo(expected));
        }

        [TestCase("INVALID", null)]
        [TestCase("INVALID", "s")]
        [TestCase("INVALID", "u")]
        public void DateConverterShouldRejectInvalidValues(string input, string? style)
        {
            IConverter converter = new DateConverter(style);

            Assert.False(converter.ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.Null);

            Assert.False(converter.ConvertToString(input, out string? str));
            Assert.That(str, Is.Null);
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
            Assert.That(new EnumConverter(typeof(MyEnum).FullName).ConvertToValue(input.AsSpan(), out object? val));
            Assert.That(val, Is.EqualTo(value));
        }

        [TestCase("value", MyEnum.Value)]
        [TestCase("default", MyEnum.Default)]
        public void EnumCoverterShouldStringify(string expected, MyEnum input)
        {
            Assert.That(new EnumConverter(typeof(MyEnum).FullName).ConvertToString(input, out string? val));
            Assert.That(val, Is.EqualTo(expected));
        }

        [Test]
        public void EnumConverterShouldRejectInvalidValues()
        {
            IConverter converter = new EnumConverter(typeof(MyEnum).FullName);

            Assert.False(converter.ConvertToValue("INVALID".AsSpan(), out object? val));
            Assert.That(val, Is.Null);

            Assert.False(converter.ConvertToString("INVALID", out string? str));
            Assert.That(str, Is.Null);
        }

        [Test]
        public void EnumConverterFactoryShouldThrowOnInvalidConfig() =>
            Assert.Throws<ArgumentException>(() => new EnumConverter("INVALID"), Resources.INVALID_FORMAT_STYLE);
    }
}