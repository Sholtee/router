/********************************************************************************
* ApplyShortcuts.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal sealed class ApplyShortcuts(IReadOnlyDictionary<string, int> shortcuts) : ExpressionVisitor
    {
        private static readonly MethodInfo
            FGetByName = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>(static dict => dict[""]),
            FGetByIndex = MethodInfoExtractor.Extract<StaticDictionary<object?>, object?>(static dict => dict[0]);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return node.Object?.Type == typeof(StaticDictionary<object?>) && node.Method == FGetByName && node.Arguments.Single() is ConstantExpression constant && shortcuts.TryGetValue((string) constant.Value, out int shortcut)
                ? Expression.Call(node.Object, FGetByIndex, Expression.Constant(shortcut))
                : base.VisitMethodCall(node);
        }
    }
}
