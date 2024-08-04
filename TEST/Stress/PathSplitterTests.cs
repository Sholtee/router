/********************************************************************************
* PathSplitterTests.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    [TestFixture]
    public class PathSplitterStressTests: PathSplitterTestsBase
    {
        [Test]
        public void CarryOutStressTest()
        {
            string[] urls = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "urls.txt"));

            Parallel.ForEach
            (
                urls,
                static url =>
                    Assert.That(url.Split(['/'], StringSplitOptions.RemoveEmptyEntries).ToList(), Is.EquivalentTo(Split(url)))
            );
        }
    }
}