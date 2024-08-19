/********************************************************************************
* ApplyShortcutsTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class ApplyShortcutsTests
    {
        [Test]
        public void Visit_ShouldRepalceTheNamedQueries()
        {
            Expression<Func<IReadOnlyDictionary<string, object?>, object?>> expr = dict => dict["cica"];

            ParameterExpression staticDict = Expression.Parameter(typeof(StaticDictionary<object?>), "dict");

            Expression<Func<StaticDictionary<object?>, object?>> replaced = (Expression<Func<StaticDictionary<object?>, object?>>) new ApplyShortcuts(new Dictionary<string, int> { {"cica", 1 } }).Visit
            (
                Expression.Lambda<Func<StaticDictionary<object?>, object?>>
                (
                    UnfoldedLambda.Create(expr, staticDict),
                    staticDict
                )
            );

            Assert.That(replaced.ToString(), Is.EqualTo("dict => dict.get_Item(1)"));
        }
    }
}