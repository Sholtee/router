/********************************************************************************
* SwitchExpression.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal sealed class SwitchExpression(bool ignoreCase)
    {
        #region Private
        private delegate ReadOnlySpan<char> AsSpanDelegate(string s);

        private delegate int CompareDelegate(ReadOnlySpan<char> a, ReadOnlySpan<char> b, StringComparison comparison);

        private static readonly MethodInfo
            FCompareTo = ((CompareDelegate) MemoryExtensions.CompareTo).Method,
            FAsSpan = ((AsSpanDelegate) MemoryExtensions.AsSpan).Method;

        private readonly ConstantExpression FComparison = Expression.Constant
        (
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
        );

        private readonly RedBlackTree<KeyValuePair<string, Expression>> FTree = new
        (
            new NodeComparer(ignoreCase)
        );

        private sealed class NodeComparer(bool ignoreCase) : IComparer<KeyValuePair<string, Expression>>
        {
            private readonly StringComparer FKeyComparer = ignoreCase
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            public int Compare(KeyValuePair<string, Expression> x, KeyValuePair<string, Expression> y)
                => FKeyComparer.Compare(x.Key, y.Key);
        }

        private Expression ProcessNode(RedBlackTreeNode<KeyValuePair<string, Expression>>? node) => node is null ? Default : Expression.Block
        (
            Expression.Assign
            (
                Order,
                Expression.Call
                (
                    FCompareTo,
                    Key,
                    Expression.Call(FAsSpan, Expression.Constant(node.Data.Key)),   
                    FComparison
                )
            ),
            Expression.IfThen
            (
                Expression.LessThan(Order, Expression.Constant(0)),
                ProcessNode(node.Left)
            ),
            Expression.IfThen
            (
                Expression.GreaterThan(Order, Expression.Constant(0)),
                ProcessNode(node.Right)
            ),
            node.Data.Value
        );
        #endregion

        public required ParameterExpression Order { get; init; }

        public required Expression Key { get; init; }

        public required Expression Default { get; init; }

        public Expression Expression => ProcessNode(FTree.Root);

        public bool AddCase(string key, Expression @case) => FTree.Add(new KeyValuePair<string, Expression>(key, @case));
    }
}
