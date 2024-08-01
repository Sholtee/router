/********************************************************************************
* PathSplitterTests.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class PathSplitterTests
    {
        private static IReadOnlyList<string> Split(string path, SplitOptions? options = null)
        {
            using PathSplitter pathSplitter = new(path.AsSpan(), options);

            List<string> result = [];

            while (pathSplitter.MoveNext())
            {
                result.Add(pathSplitter.Current.ToString());
            }

            return result;
        }

        [TestCase("/", arg2: new string[0])]
        [TestCase("/cica/", arg2: new string[] { "cica" })]
        [TestCase("/cica", arg2: new string[] { "cica" })]
        [TestCase("cica/", arg2: new string[] { "cica" })]
        [TestCase("/cica/mica/", arg2: new string[] { "cica", "mica" })]
        [TestCase("/cica/mica", arg2: new string[] { "cica", "mica" })]
        [TestCase("cica/mica/", arg2: new string[] { "cica", "mica" })]
        public void SplitShouldNotTakeLeadingAndTrailingSeparatorsIntoAccount(string input, string[] expected) =>
            Assert.That(Split(input).SequenceEqual(expected));

        [TestCase("")]
        [TestCase("/")]
        public void SplitShouldHandleEmptyPath(string input) =>
            Assert.That(Split(input), Is.Empty);

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
            Assert.Throws<InvalidOperationException>(() => Split(input));

        [TestCase("//")]
        [TestCase("//mica")]
        [TestCase("/cica//")]
        [TestCase("/cica//mica")]
        [TestCase("cica//")]
        [TestCase("cica//mica")]
        public void SplitShouldThrowOnEmptyChunk(string input) =>
            Assert.Throws<InvalidOperationException>(() => Split(input));

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
            Assert.That(Split(input).SequenceEqual(expected));

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
            Assert.That(Split(input).SequenceEqual(expected));

        public static IEnumerable<object[]> SplitShouldThrowOnUnsafeChar_Cases
        {
            get
            {
                yield return ["cica\\", SplitOptions.Default, 4];
                yield return ["/cica\\", SplitOptions.Default, 5];
                yield return ["cica/mi\\ca", SplitOptions.Default, 7];
                yield return ["/cica/mi\\ca", SplitOptions.Default, 8];
                yield return ["cica/a+b", SplitOptions.Default with { ConvertSpaces = false }, 6];
                yield return ["/cica/a+b", SplitOptions.Default with { ConvertSpaces = false}, 7];
            }
        }

        [TestCaseSource(nameof(SplitShouldThrowOnUnsafeChar_Cases))]
        public void SplitShouldThrowOnUnsafeChar(string input, SplitOptions opts, int errPos)
        {
            InvalidOperationException err = Assert.Throws<InvalidOperationException>(() => Split(input, opts))!;
            Assert.That(err.Data, Does.ContainKey("Position"));
            Assert.That(err.Data["Position"], Is.EqualTo(errPos));
        }

        public static IEnumerable<object[]> SplitShouldSplit_Cases
        {
            get
            {
                foreach (string url in File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "urls.txt")))
                {
                    yield return new object[] { url };
                }
            }
        }

        [TestCaseSource(nameof(SplitShouldSplit_Cases))]
        public void SplitShouldSplit(string input)
        {
            Assert.That(input.Split(['/'], StringSplitOptions.RemoveEmptyEntries).ToList(), Is.EquivalentTo(Split(input)));
        }
    }
}