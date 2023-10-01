/********************************************************************************
* ReadOnlyDictionary.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal struct ValueWrapper
    {
        public bool Assigned;

        public object Value;
    }

    internal delegate ref ValueWrapper GetValueDelegate(ValueWrapper[] ar, string name);

    internal sealed class ReadOnlyDictionaryBuilder
    {
        private static readonly MethodInfo
            FCompareTo = MethodInfoExtractor.Extract<IComparer<string>>(static cmp => cmp.Compare(null!, null!)),
            FGetValue = ((GetValueByIndexDelegate) GetValue).Method; // MethodInfoExtractor.Extract(static () => GetValue(null!, 0))

        private static readonly Type FValueWrapperByRef = typeof(ValueWrapper).MakeByRefType();

        private readonly IComparer<string> FComparer;

        private readonly RedBlackTree<string> FTree;

        private readonly ParameterExpression
            FDataArray,
            FOrder,
            FKey;

        private readonly LabelTarget FFound;

        private delegate ref ValueWrapper GetValueByIndexDelegate(ValueWrapper[] ar, int index);

        private static ref ValueWrapper GetValue(ValueWrapper[] ar, int index)
        {
            if (index < 0)
                throw new KeyNotFoundException();

            return ref ar[index];
        }

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

        public ReadOnlyDictionaryBuilder(IComparer<string> comparer)
        {
            FTree = new RedBlackTree<string>(FComparer = comparer);
            FDataArray = Expression.Parameter(typeof(ValueWrapper[]), "dataAr");
            FKey = Expression.Parameter(typeof(string), "key");
            FOrder = Expression.Variable(typeof(int), "order");
            FFound = Expression.Label(type: typeof(int), "found");
        }

        public bool CreateSlot(string name) => FTree.Add(name);

        public GetValueDelegate Build(out int arSize)
        {
            arSize = 0;

            Expression<GetValueDelegate> del = Expression.Lambda<GetValueDelegate>
            (
                Expression.Block
                (
                    type: FValueWrapperByRef,
                    variables: new ParameterExpression[] { FOrder },
                    Expression.Call
                    (
                        FGetValue,
                        FDataArray,
                        Expression.Block
                        (
                            type: typeof(int),
                            ProcessNode(FTree.Root, ref arSize),
                            Expression.Label(FFound, Expression.Constant(-1))
                        )
                    )
                ),
                FDataArray,
                FKey
            );

            Debug.WriteLine(del.GetDebugView());

            return del.Compile();
        }
    }
}
