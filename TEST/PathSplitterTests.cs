/********************************************************************************
* PathSplitterTests.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    internal static class PathSplitterExtensions
    {
        public static IReadOnlyList<string> AsList(this PathSplitter self)
        {
            List<string> result = [];

            self.Reset();

            while (self.MoveNext())
            {
                result.Add(self.Current.ToString());
            }

            return result;
        }
    }

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
            Assert.That(PathSplitter.Split(input.AsSpan()).AsList().SequenceEqual(expected));

        [TestCase("")]
        [TestCase("/")]
        public void SplitShouldHandleEmptyPath(string input) =>
            Assert.That(!PathSplitter.Split(input.AsSpan()).AsList().Any());

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

        [TestCase("%u")]
        [TestCase("%u/")]
        [TestCase("%u/mica")]
        [TestCase("cica/%u")]
        [TestCase("cica/%u/")]
        [TestCase("cica/%u/mica")]
        [TestCase("/%u")]
        [TestCase("/%u/")]
        [TestCase("/%u/mica")]
        [TestCase("/cica/%u")]
        [TestCase("/cica/%u/")]
        [TestCase("/cica/%u/mica")]

        [TestCase("%u00E")]
        [TestCase("%u00E/")]
        [TestCase("%u00E/mica")]
        [TestCase("cica/%u00E")]
        [TestCase("cica/%u00E/")]
        [TestCase("cica/%u00E/mica")]
        [TestCase("/%u00E")]
        [TestCase("/%u00E/")]
        [TestCase("/%u00E/mica")]
        [TestCase("/cica/%u00E")]
        [TestCase("/cica/%u00E/")]
        [TestCase("/cica/%u00E/mica")]

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

        [TestCase("/%u00EŰ")]
        [TestCase("/%u00EŰ/")]
        [TestCase("/%u00EŰ/mica")]
        [TestCase("/cica/%u00EŰ")]
        [TestCase("/cica/%u00EŰ/")]
        [TestCase("/cica/%u00EŰ/mica")]

        [TestCase("%u00EŰ")]
        [TestCase("%u00EŰ/")]
        [TestCase("%u00EŰ/mica")]
        [TestCase("cica/%u00EŰ")]
        [TestCase("cica/%u00EŰ/")]
        [TestCase("cica/%u00EŰ/mica")]
        public void SplitShouldThrowOnInvalidHex(string input) =>
            Assert.Throws<InvalidOperationException>(() => PathSplitter.Split(input.AsSpan()).AsList().ToList());

        [TestCase("//")]
        [TestCase("//mica")]
        [TestCase("/cica//")]
        [TestCase("/cica//mica")]
        [TestCase("cica//")]
        [TestCase("cica//mica")]
        public void SplitShouldThrowOnEmptyChunk(string input) =>
            Assert.Throws<InvalidOperationException>(() => PathSplitter.Split(input.AsSpan()).AsList().ToList());

        [TestCase("%C3%A1", arg2: new string[] { "á" })]
        [TestCase("%C3%A1/", arg2: new string[] { "á" })]
        [TestCase("%C3%A1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("cica/%C3%A1", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%C3%A1/", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%C3%A1/mica", arg2: new string[] { "cica", "á", "mica" })]
        [TestCase("/%C3%A1", arg2: new string[] { "á" })]
        [TestCase("/%C3%A1/", arg2: new string[] { "á" })]
        [TestCase("/%C3%A1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("/cica/%C3%A1", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%C3%A1/", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%C3%A1/mica", arg2: new string[] { "cica", "á", "mica" })]

        [TestCase("%C3%A1bc", arg2: new string[] { "ábc" })]
        [TestCase("%C3%A1bc/", arg2: new string[] { "ábc" })]
        [TestCase("%C3%A1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("cica/%C3%A1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%C3%A1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%C3%A1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]
        [TestCase("/%C3%A1bc", arg2: new string[] { "ábc" })]
        [TestCase("/%C3%A1bc/", arg2: new string[] { "ábc" })]
        [TestCase("/%C3%A1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("/cica/%C3%A1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%C3%A1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%C3%A1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]

        [TestCase("%u00E1", arg2: new string[] { "á" })]
        [TestCase("%u00E1/", arg2: new string[] { "á" })]
        [TestCase("%u00E1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("cica/%u00E1", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%u00E1/", arg2: new string[] { "cica", "á" })]
        [TestCase("cica/%u00E1/mica", arg2: new string[] { "cica", "á", "mica" })]
        [TestCase("/%u00E1", arg2: new string[] { "á" })]
        [TestCase("/%u00E1/", arg2: new string[] { "á" })]
        [TestCase("/%u00E1/mica", arg2: new string[] { "á", "mica" })]
        [TestCase("/cica/%u00E1", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%u00E1/", arg2: new string[] { "cica", "á" })]
        [TestCase("/cica/%u00E1/mica", arg2: new string[] { "cica", "á", "mica" })]

        [TestCase("%u00E1%C3%A1", arg2: new string[] { "áá" })]  // mixed hex values

        [TestCase("%u00E1bc", arg2: new string[] { "ábc" })]
        [TestCase("%u00E1bc/", arg2: new string[] { "ábc" })]
        [TestCase("%u00E1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("cica/%u00E1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%u00E1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("cica/%u00E1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]
        [TestCase("/%u00E1bc", arg2: new string[] { "ábc" })]
        [TestCase("/%u00E1bc/", arg2: new string[] { "ábc" })]
        [TestCase("/%u00E1bc/mica", arg2: new string[] { "ábc", "mica" })]
        [TestCase("/cica/%u00E1bc", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%u00E1bc/", arg2: new string[] { "cica", "ábc" })]
        [TestCase("/cica/%u00E1bc/mica", arg2: new string[] { "cica", "ábc", "mica" })]

        [TestCase("%uD83D%uDE01", arg2: new string[] { "😁" })]
        public void SplitShouldHandleHexChunks(string input, string[] expected) =>
            Assert.That(PathSplitter.Split(input.AsSpan()).AsList().SequenceEqual(expected));

        [TestCase("+", arg2: new string[] { " " })]
        [TestCase("+/", arg2: new string[] { " " })]
        [TestCase("+/mica", arg2: new string[] { " ", "mica" })]
        [TestCase("cica/+", arg2: new string[] { "cica", " " })]
        [TestCase("cica/+/", arg2: new string[] { "cica", " " })]
        [TestCase("cica/+/mica", arg2: new string[] { "cica", " ", "mica" })]
        [TestCase("/+", arg2: new string[] { " " })]
        [TestCase("/+/", arg2: new string[] { " " })]
        [TestCase("/+/mica", arg2: new string[] { " ", "mica" })]
        [TestCase("/cica/+", arg2: new string[] { "cica", " " })]
        [TestCase("/cica/+/", arg2: new string[] { "cica", " " })]
        [TestCase("/cica/+/mica", arg2: new string[] { "cica", " ", "mica" })]

        [TestCase("a+b", arg2: new string[] { "a b" })]
        [TestCase("a+b/", arg2: new string[] { "a b" })]
        [TestCase("a+b/mica", arg2: new string[] { "a b", "mica" })]
        [TestCase("cica/a+b", arg2: new string[] { "cica", "a b" })]
        [TestCase("cica/a+b/", arg2: new string[] { "cica", "a b" })]
        [TestCase("cica/a+b/mica", arg2: new string[] { "cica", "a b", "mica" })]
        [TestCase("/a+b", arg2: new string[] { "a b" })]
        [TestCase("/a+b/", arg2: new string[] { "a b" })]
        [TestCase("/a+b/mica", arg2: new string[] { "a b", "mica" })]
        [TestCase("/cica/a+b", arg2: new string[] { "cica", "a b" })]
        [TestCase("/cica/a+b/", arg2: new string[] { "cica", "a b" })]
        [TestCase("/cica/a+b/mica", arg2: new string[] { "cica", "a b", "mica" })]
        public void SplitShouldHandleSpaces(string input, string[] expected) =>
            Assert.That(PathSplitter.Split(input.AsSpan()).AsList().SequenceEqual(expected));
    }
}