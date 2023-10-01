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

    [TestFixture]
    public class LookupBuilderTests
    {
        [Test]
        public void LookupBuilder_ShouldAssembleTheDesiredDelegate([Values(0, 1, 2, 3, 10, 20, 30)] int keys)
        {
            LookupBuilder<string> bldr = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys; i++)
            {
                Assert.That(bldr.CreateSlot(i.ToString()));
            }

            int arSize = 0;

            Assert.DoesNotThrow(() => bldr.Build(out arSize));
            Assert.That(arSize, Is.EqualTo(keys));
        }

        [Test]
        public void Lookup_ShouldFindItemByKey([Values(1, 2, 3, 10, 20, 30)] int keys)
        {
            LookupBuilder<string> bldr = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys; i++)
            {
                Assert.That(bldr.CreateSlot(i.ToString()));
            }

            LookupDelegate<string> lookup = bldr.Build(out int arSize);

            string[] ar = new string[arSize];

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
            LookupDelegate<string> lookup = bldr.Build(out int arSize);
            Assert.Throws<KeyNotFoundException>(() => lookup(Array.Empty<string>(), "cica"));
        }
    }
}