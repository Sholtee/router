using NUnit.Framework;

namespace Router.Tests
{
    using Internals;

    [TestFixture]
    public class PathSplitterTests
    {
        [TestCase("/cica/")]
        [TestCase("/cica")]
        [TestCase("cica/")]
        [TestCase("/cica/mica/")]
        [TestCase("/cica/mica")]
        [TestCase("cica/mica/")]
        public void SplitShouldNotTakeLeadingAndTrailingSeparatorsIntoAccount(string input, string[] expected)
        {
        }

        [Test]
        public void SplitShouldHandleEmptyPath()
        {
        }

        // Too short hex
        [TestCase("%")]
        [TestCase("%/")]
        [TestCase("%/mica")]
        [TestCase("cica/%")]
        [TestCase("cica/%/")]
        [TestCase("cica/%/mica")]
        [TestCase("/%")]
        [TestCase("/%/")]
        [TestCase("/%/mica")]
        [TestCase("/cica/%")]
        [TestCase("/cica/%/")]
        [TestCase("/cica/%/mica")]

        [TestCase("%A")]
        [TestCase("%A/")]
        [TestCase("%A/mica")]
        [TestCase("cica/%A")]
        [TestCase("cica/%A/")]
        [TestCase("cica/%A/mica")]
        [TestCase("/%A")]
        [TestCase("/%A/")]
        [TestCase("/%A/mica")]
        [TestCase("/cica/%A")]
        [TestCase("/cica/%A/")]
        [TestCase("/cica/%A/mica")]

        // Invalid hex
        [TestCase("/%AÁ")]
        [TestCase("/%AÁ/")]
        [TestCase("/%AÁ/mica")]   
        [TestCase("/cica/%AÁ")]
        [TestCase("/cica/%AÁ/")]
        [TestCase("/cica/%AÁ/mica")]

        [TestCase("%AÁ")]
        [TestCase("%AÁ/")]
        [TestCase("%AÁ/mica")]
        [TestCase("cica/%AÁ")]
        [TestCase("cica/%AÁ/")]
        [TestCase("cica/%AÁ/mica")]
        public void SplitShouldThrowOnInvalidHex(string input)
        {
            Assert.Throws<ArgumentException>(() => PathSplitter.Split(input).ToList());
        }
    }
}