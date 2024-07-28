/********************************************************************************
* StaticDictionary.Builder.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal delegate StaticDictionary<TData> StaticDictionaryFactory<TData>();

    internal sealed partial class StaticDictionary<TData>
    {
        public sealed class Builder(bool ignoreCase = false)
        {
            #region Private
            private delegate int GetIndexDelegate(ReadOnlySpan<char> key);

            private delegate ReadOnlySpan<char> AsSpanDelegate(string s);

            private delegate int CompareDelegate(ReadOnlySpan<char> a, ReadOnlySpan<char> b, StringComparison comparison);

            private static readonly MethodInfo
                FCompareTo = ((CompareDelegate) MemoryExtensions.CompareTo).Method,
                FAsSpan = ((AsSpanDelegate) MemoryExtensions.AsSpan).Method;

            private static readonly ParameterExpression
                FOrder = Expression.Variable(typeof(int), "order"),
                FKey = Expression.Parameter(typeof(ReadOnlySpan<char>), "key");

            private static readonly LabelTarget FFound = Expression.Label(type: typeof(int), "found");

            private readonly ConstantExpression FComparison = Expression.Constant
            (
                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            );

            private readonly RedBlackTree<string> FTree = new
            (
                ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
            );

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
                            FCompareTo,
                            FKey,
                            Expression.Call(FAsSpan, Expression.Constant(node.Data)),   
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

            private LookupDelegate Build(DelegateCompiler compiler, out IReadOnlyDictionary<string, int> shortcuts)
            {
                Dictionary<string, int> dict = [];

                Expression<GetIndexDelegate> getIndexExpr = Expression.Lambda<GetIndexDelegate>
                (
                    Expression.Block
                    (
                        type: typeof(int),
                        variables: [FOrder],
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
                ref ValueWrapper GetValue(ValueWrapper[] dataArray, ReadOnlySpan<char> key)
                {
                    int index = getIndex.Value(key);
                    if (index < 0)
                        return ref Unsafe.NullRef<ValueWrapper>();

                    Debug.Assert(index < dataArray.Length, "Miscalculated index");

                    return ref dataArray[index];
                }
            }
            #endregion

            public bool RegisterKey(string key) => FTree.Add(key);

            public StaticDictionaryFactory<TData> CreateFactory(DelegateCompiler compiler, out IReadOnlyDictionary<string, int> shortcuts)
            {
                IReadOnlyList<string> keys = FTree.Select(static node => node.Data).ToList();

                LookupDelegate lookup = Build(compiler, out shortcuts);

                Debug.Assert(shortcuts.Count == keys.Count, "Size mismatch");

                return () => new StaticDictionary<TData>(keys, lookup);
            }
        }
    }
}
