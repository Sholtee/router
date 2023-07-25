﻿using NUnit.Framework;

namespace Router.Tests
{
    using Internals;

    [TestFixture]
    public class PathSplitterTests
    {
        [TestCase("/", arg2: new string[0])]
        [TestCase("/cica/", arg2: new string[] { "cica" })]
        [TestCase("/cica", arg2: new string[] { "cica" })]
        [TestCase("cica/", arg2: new string[] { "cica" })]
        [TestCase("/cica/mica/", arg2: new string[] { "cica", "mica" })]
        [TestCase("/cica/mica", arg2: new string[] { "cica", "mica" })]
        [TestCase("cica/mica/", arg2: new string[] { "cica", "mica" })]
        public void SplitShouldNotTakeLeadingAndTrailingSeparatorsIntoAccount(string input, string[] expected) =>
            Assert.That(PathSplitter.Split(input).SequenceEqual(expected));

        [Test]
        public void SplitShouldHandleEmptyPath() =>
            Assert.That(!PathSplitter.Split("").Any());

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
        public void SplitShouldThrowOnInvalidHex(string input) =>
            Assert.Throws<ArgumentException>(() => PathSplitter.Split(input).ToList());

        [TestCase("//")]
        [TestCase("//mica")]
        [TestCase("/cica//")]
        [TestCase("/cica//mica")]
        [TestCase("cica//")]
        [TestCase("cica//mica")]
        public void SplitShouldThrowOnEmptyChunk(string input) =>
            Assert.Throws<ArgumentException>(() => PathSplitter.Split(input).ToList());

        [TestCase("%E1", arg2: new string[] { "á" })]
        [TestCase("%E1/", arg2: new string[] { "á" })]
        [TestCase("%E1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("cica/%E1", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%E1/", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%E1/mica", arg2: new string[] { "cica", "á", "mica" })]
        [TestCase("/%E1", arg2: new string[] { "á" })]
        [TestCase("/%E1/", arg2: new string[] { "á" })]
        [TestCase("/%E1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("/cica/%E1", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%E1/", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%E1/mica", arg2: new string[] { "cica", "á", "mica" })]

        [TestCase("%E1bc", arg2: new string[] { "ábc" })]
        [TestCase("%E1bc/", arg2: new string[] { "ábc" })]
        [TestCase("%E1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("cica/%E1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%E1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%E1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]
        [TestCase("/%E1bc", arg2: new string[] { "ábc" })]
        [TestCase("/%E1bc/", arg2: new string[] { "ábc" })]
        [TestCase("/%E1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("/cica/%E1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%E1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%E1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]
        public void SplitShouldHandleHexChunks(string input, string[] expected) =>
            Assert.That(PathSplitter.Split(input).SequenceEqual(expected));
    }
}