/********************************************************************************
* EnumConverter.cs.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Router.Internals
{
#if NETSTANDARD2_0
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;

    using Primitives;
#endif
    using static Properties.Resources;

    internal sealed class EnumConverter : ConverterBase
    {
#if NETSTANDARD2_0
        private static readonly MethodInfo FTryParseGen =
        (
            (MethodCallExpression)
            (
                (Expression<Action<SplitOptions>>)
                (
                    static o => Enum.TryParse(null!, true, out o)
                )
            ).Body
        ).Method.GetGenericMethodDefinition();

        private delegate bool TryParse(string input, out object? value);

        private readonly TryParse FTryParse;
#endif
        public Type EnumType { get; }

        public EnumConverter(string? style): base(style)
        {
            if (style is null)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));

            //
            // Types declared outside of System.Private.CoreLib.dll can be loaded by assembly qualified name only
            // so we have to overcome this limitation. It's slow but won't run frequently
            // 

            List<Type> hits = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Select(asm => asm.GetType(style, throwOnError: false))
                .Where(t => t is not null)
                .ToList();
            if (hits.Count is not 1 || !hits[0].IsEnum)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));

            EnumType = hits[0];
#if NETSTANDARD2_0
            ParameterExpression
                input  = Expression.Parameter(typeof(string), nameof(input)),
                output = Expression.Parameter(typeof(object).MakeByRefType(), nameof(output)),
                ret    = Expression.Variable(EnumType, nameof(ret));

            LabelTarget exit = Expression.Label(typeof(bool), nameof(exit));

            Expression<TryParse> tryParseExpr = Expression.Lambda<TryParse>
            (
                Expression.Block
                (
                    variables: new ParameterExpression[] { ret },
                    Expression.IfThen
                    (
                        Expression.Call
                        (
                            FTryParseGen.MakeGenericMethod(EnumType),
                            input,
                            Expression.Constant(true),  // ignoreCase
                            ret
                        ),
                        ifTrue: Expression.Block
                        (
                            Expression.Assign(output, Expression.Convert(ret, typeof(object))),
                            Expression.Goto(exit, Expression.Constant(true))
                        )
                    ),
                    Expression.Label(exit, Expression.Constant(false))
                ),
                input,
                output
            );
            Debug.WriteLine(tryParseExpr.GetDebugView());

            FTryParse = tryParseExpr.Compile();
#endif
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not Enum @enum)
            {
                value = null;
                return false;
            }

            value = @enum.ToString("G");
            return true;
        }

        public override bool ConvertToValue(string input, out object? value) =>
#if NETSTANDARD2_0
            FTryParse(input, out value);
#else
            Enum.TryParse(EnumType, input, ignoreCase: true, out value);
#endif
    }
}
