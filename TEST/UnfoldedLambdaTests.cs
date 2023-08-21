/********************************************************************************
* UnfoldedLambdaTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;

    [TestFixture]
    public class UnfoldedLambdaTests
    {
        [Test]
        public void CreateShouldUnfoldTheGivenLambda()
        {
            Expression<Func<int, int>> expr = i => i + 1;

            ParameterExpression newParam = Expression.Parameter(typeof(int), nameof(newParam));

            BinaryExpression unfolded = (UnfoldedLambda.Create(expr, newParam) as BinaryExpression)!;

            Assert.That(unfolded, Is.Not.Null);
            Assert.That(unfolded.Left, Is.EqualTo(newParam));
        }

        [Test]
        public void CreateShouldUnfoldTheGivenLambdaHavingNestedLambda()
        {
            Expression<Func<int, int>> exprInner = i => i + 1;

            ParameterExpression i = Expression.Parameter(typeof(int), nameof(i));

            Expression<Func<int, int>> expr = Expression.Lambda<Func<int, int>>
            (
                body: Expression.Invoke(exprInner, i),
                i
            );

            ParameterExpression newParam = Expression.Parameter(typeof(int), nameof(newParam));

            InvocationExpression unfolded = (UnfoldedLambda.Create(expr, newParam) as InvocationExpression)!;

            Assert.That(unfolded, Is.Not.Null);
            Assert.That(unfolded.Expression, Is.EqualTo(exprInner)); // not touched
            Assert.That(unfolded.Arguments.Single(), Is.EqualTo(newParam));
        }
    }
}