/********************************************************************************
* ReadOnlyDictionaryTests.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class ReadOnlyDictionaryTests
    {
        [Test]
        public void Builder_ShouldAssembleTheDesiredDelegate([Values(0, 1, 2, 3, 10, 20, 30)] int keys)
        {
            ReadOnlyDictionaryBuilder bldr = new(StringComparer.OrdinalIgnoreCase);

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
            ReadOnlyDictionaryBuilder bldr = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys; i++)
            {
                Assert.That(bldr.CreateSlot(i.ToString()));
            }

            GetValueDelegate lookup = bldr.Build(out int arSize);

            ValueWrapper[] ar = new ValueWrapper[arSize];

            for (int i = 0; i < keys; i++)
            {
                ref ValueWrapper val = ref lookup(ar, i.ToString());
                Assert.False(val.Assigned);

                val.Assigned = true;
            }
        }
    }
}