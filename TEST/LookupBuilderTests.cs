/********************************************************************************
* LookupBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Primitives;

    [TestFixture]
    public class LookupBuilderTests
    {
        DelegateCompiler Compiler { get; set; } = null!;

        [SetUp]
        public void SetupTest() => Compiler = new DelegateCompiler();

        [Test]
        public void LookupBuilder_ShouldAssembleTheDesiredDelegate([Values(0, 1, 2, 3, 10, 20, 30)] int keys)
        {
            LookupBuilder<string> bldr = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys; i++)
            {
                Assert.That(bldr.CreateSlot(i.ToString()));
            }

            IDictionary<string, int> shortcuts = null!;

            Assert.DoesNotThrow(() => bldr.Build(Compiler, out shortcuts));
            Assert.That(shortcuts.Count, Is.EqualTo(keys));
        }

        [Test]
        public void Lookup_ShouldFindItemByKey([Values(1, 2, 3, 10, 20, 30)] int keys)
        {
            LookupBuilder<string> bldr = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys; i++)
            {
                Assert.That(bldr.CreateSlot(i.ToString()));
            }

            LookupDelegate<string> lookup = bldr.Build(Compiler, out IDictionary<string, int> shortcuts);
            Compiler.Compile();

            string[] ar = new string[shortcuts.Count];

            for (int i = 0; i < keys; i++)
            {
                ref string val = ref lookup(ar, i.ToString());
                Assert.That(val is null);
                val = "cica";
            }
        }

        [Test]
        public void Lookup_ShouldThrowOnMissingItem()
        {
            LookupBuilder<string> bldr = new(StringComparer.OrdinalIgnoreCase);
            LookupDelegate<string> lookup = bldr.Build(Compiler, out _);
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => lookup(Array.Empty<string>(), "cica"));
        }
    }
}