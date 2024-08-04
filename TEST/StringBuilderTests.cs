/********************************************************************************
* StringBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class StringBuilderTests
    {
        [Test]
        public void Append_ShouldExtendTheUnderlyingBuffer([Values(0, 1, 10, 100, 1024)] int initialSize, [Values(0, 1, 10, 100, 1000)] int expectedLength)
        {
            using StringBuilder sb = new(initialSize);

            string expected = "";

            for (int i = 0; i < expectedLength; i++)
            {
                expected += i.ToString();
                sb.Append(i.ToString());
            }

            Assert.That(sb.ToString(), Is.EqualTo(expected));
            Assert.That(sb.Length, Is.EqualTo(expected.Length));
        }
    }
}