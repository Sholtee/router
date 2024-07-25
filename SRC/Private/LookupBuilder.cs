/********************************************************************************
* LookupBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal sealed class LookupBuilder<TData>(bool ignoreCase)
    {
        private delegate int GetIndexDelegate(ReadOnlySpan<char> key);

        private delegate int CompareDelegate(string a, ReadOnlySpan<char> b, StringComparison comparison);

        private static int Compare(string a, ReadOnlySpan<char> b, StringComparison comparison) =>
            b.CompareTo(a.AsSpan(), comparison);

        private static readonly MethodInfo FCompare = ((CompareDelegate) Compare).Method;

        private static readonly ParameterExpression
            FOrder = Expression.Variable(typeof(int), "order"),
            FKey = Expression.Parameter(typeof(ReadOnlySpan<char>), "key");

        private static readonly LabelTarget FFound = Expression.Label(type: typeof(int), "found");

        private readonly ConstantExpression FComparison = Expression.Constant(ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private readonly RedBlackTree<string> FTree = new(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private Expression ProcessNode(RedBlackTreeNode<string>? node, IDictionary<string, int> shortcuts)
        {
            if (node is null)
                return Expression.Goto(FFound, Expression.Constant(-1));

            return Expression.Block
            (
                Expression.Assign
                (
                    FOrder,
                    Expression.Call
                    (
                        FCompare,
                        Expression.Constant(node.Data),
                        FKey,
                        FComparison
                    )
                ),
                Expression.IfThen
                (
                    Expression.LessThan(FOrder, Expression.Constant(0)),
                    ProcessNode(node.Left, shortcuts)
                ),
                Expression.IfThen
                (
                    Expression.GreaterThan(FOrder, Expression.Constant(0)),
                    ProcessNode(node.Right, shortcuts)
                ),
                Expression.Goto(FFound, Expression.Constant(CreateEntry()))
            );

            int CreateEntry()
            {
                int id = shortcuts.Count;
                shortcuts.Add(node!.Data, id);
                return id;
            }
        }

        public bool CreateSlot(string name) => FTree.Add(name);

        public IEnumerable<string> Slots
        {
            get
            {
                foreach (RedBlackTreeNode<string> node in FTree)
                {
                    yield return node.Data;
                }
            }
        }

        public LookupDelegate<TData> Build(DelegateCompiler compiler, out IReadOnlyDictionary<string, int> shortcuts)
        {
            Dictionary<string, int> dict = [];

            Expression<GetIndexDelegate> getIndexExpr = Expression.Lambda<GetIndexDelegate>
            (
                Expression.Block
                (
                    type: typeof(int),
                    variables: new ParameterExpression[] { FOrder },
                    ProcessNode(FTree.Root, dict),
                    Expression.Label(FFound, Expression.Constant(-1))
                ),
                FKey
            );

            Debug.WriteLine(getIndexExpr.GetDebugView());
            FutureDelegate<GetIndexDelegate> getIndex = compiler.Register(getIndexExpr);

            shortcuts = dict;
            return GetValue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            ref TData GetValue(TData[] dataArray, ReadOnlySpan<char> key)
            {
                int index = getIndex.Value(key);
                if (index < 0)
                    return ref Unsafe.NullRef<TData>();

                Debug.Assert(index < dataArray.Length, "Miscalculated index");

                return ref dataArray[index];
            }
        }
    }
}
