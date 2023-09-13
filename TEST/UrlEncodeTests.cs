/********************************************************************************
* UrlEncodeTests.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class UrlEncodeTests
    {
        [TestCase("", "")]
        [TestCase("a", "a")]
        [TestCase("abc", "abc")]
        [TestCase("/", "%2F")]
        [TestCase("///", "%2F%2F%2F")]
        [TestCase("😁", "%uD83D%uDE01")]
        [TestCase(" ", "+")]
        [TestCase("+", "%2B")]
        public void EncodeShouldGenerateProperOutput(string input, string expected) =>
            Assert.That(UrlEncode.Encode(input), Is.EqualTo(expected));
    }
}