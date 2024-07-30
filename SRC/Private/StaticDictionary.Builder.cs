/********************************************************************************
* StaticDictionary.Builder.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    internal delegate StaticDictionary<TData> StaticDictionaryFactory<TData>();

    internal sealed partial class StaticDictionary<TData>
    {
        public sealed class Builder()
        {
            #region Private
            private delegate int GetIndexDelegate(ReadOnlySpan<char> key);

            private static readonly LabelTarget FFound = Expression.Label(type: typeof(int), "found");

            private static readonly ParameterExpression
                FOrder = Expression.Variable(typeof(int), "order"),
                FKey = Expression.Parameter(typeof(ReadOnlySpan<char>), "key");

            private readonly SwitchExpression FSwitchExpression = new(false)
            {
                Default = Expression.Goto(FFound, Expression.Constant(-1)),
                Key = FKey,
                Order = FOrder
            };

            private readonly Dictionary<string, int> FShortcuts = [];

            private LookupDelegate Build(DelegateCompiler compiler)
            {
                Expression<GetIndexDelegate> getIndexExpr = Expression.Lambda<GetIndexDelegate>
                (
                    Expression.Block
                    (
                        type: typeof(int),
                        variables: [FOrder],
                        FSwitchExpression.Expression,
                        Expression.Label(FFound, Expression.Constant(-1))
                    ),
                    FKey
                );

                Debug.WriteLine(getIndexExpr.GetDebugView());
                FutureDelegate<GetIndexDelegate> getIndex = compiler.Register(getIndexExpr);

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

            public bool RegisterKey(string key)
            {
                if (FSwitchExpression.AddCase(key, Expression.Goto(FFound, Expression.Constant(FShortcuts.Count))))
                {
                    FShortcuts.Add(key, FShortcuts.Count);
                    return true;
                }
                return false;
            }

            public StaticDictionaryFactory<TData> CreateFactory(DelegateCompiler compiler, out IReadOnlyDictionary<string, int> shortcuts)
            {
                IReadOnlyList<string> keys = new List<string>(FShortcuts.Keys);  // copy the actual key list

                shortcuts = new Dictionary<string, int>(FShortcuts);  // copy the actual shortcuts

                LookupDelegate lookup = Build(compiler);

                return () => new StaticDictionary<TData>(keys, lookup);
            }
        }
    }
}
