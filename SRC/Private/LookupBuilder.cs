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

    internal sealed class LookupBuilder<TData>
    {
        private static readonly MethodInfo FCompareTo = MethodInfoExtractor.Extract<IComparer<string>>
        (
            static cmp => cmp.Compare(null!, null!)
        );

        private static readonly ParameterExpression
            FOrder = Expression.Variable(typeof(int), "order"),
            FKey = Expression.Parameter(typeof(string), "key");

        private static readonly LabelTarget FFound = Expression.Label(type: typeof(int), "found");

        private readonly IComparer<string> FComparer;

        private readonly RedBlackTree<string> FTree;

        private Expression ProcessNode(RedBlackTreeNode<string>? node, ref int index)
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
                        Expression.Constant(FComparer),
                        FCompareTo,
                        FKey,
                        Expression.Constant(node.Data)
                    )
                ),
                Expression.IfThen
                (
                    Expression.LessThan(FOrder, Expression.Constant(0)),
                    ProcessNode(node.Left, ref index)
                ),
                Expression.IfThen
                (
                    Expression.GreaterThan(FOrder, Expression.Constant(0)),
                    ProcessNode(node.Right, ref index)
                ),
                Expression.Goto(FFound, Expression.Constant(index++))
            );
        }

        public LookupBuilder(IComparer<string> comparer)
        {
            FTree = new RedBlackTree<string>(FComparer = comparer);
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

        public LookupDelegate<TData> Build(DelegateCompiler compiler, out int arSize)
        {
            arSize = 0;

            Expression<Func<string, int>> getIndexExpr = Expression.Lambda<Func<string, int>>
            (
                Expression.Block
                (
                    type: typeof(int),
                    variables: new ParameterExpression[] { FOrder },
                    ProcessNode(FTree.Root, ref arSize),
                    Expression.Label(FFound, Expression.Constant(-1))
                ),
                FKey
            );

            Debug.WriteLine(getIndexExpr.GetDebugView());

            FutureDelegate<Func<string, int>> getIndex = compiler.Register(getIndexExpr);
            return GetValue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            ref TData GetValue(TData[] dataArray, string key)
            {
                int index = getIndex.Value(key);
                if (index < 0)
                    throw new KeyNotFoundException(key);

                Debug.Assert(index < dataArray.Length, "Miscalculated index");

                return ref dataArray[index];
            }
        }
    }
}
