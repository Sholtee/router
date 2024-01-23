/********************************************************************************
* EnumConverter.cs.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

#if !NETSTANDARD2_1_OR_GREATER
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
#endif

namespace Solti.Utils.Router.Internals
{
#if !NETSTANDARD2_1_OR_GREATER
    using Primitives;
#endif
    using static Properties.Resources;

    internal sealed class EnumConverter : ConverterBase
    {
#if !NETSTANDARD2_1_OR_GREATER
        private static readonly MethodInfo FTryParseGen = MethodInfoExtractor
            .Extract<int>(static i => Enum.TryParse(null!, true, out i))
            .GetGenericMethodDefinition();

        private delegate bool TryParse(string input, out object? value);

        private readonly TryParse FTryParse;
#endif
        private static Type GetEnumType(string qualifiedName)
        {
            //
            // Types declared outside of System.Private.CoreLib.dll can be loaded by assembly qualified name only
            // so we have to overcome this limitation. It's slow but won't run frequently
            // 

            List<Type> hits = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Select
                (
                    asm => asm.GetType(qualifiedName, throwOnError: false)
                )
                .Where(static t => t is not null)
                .ToList();
            if (hits.Count is not 1 || !hits[0].IsEnum)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, qualifiedName), nameof(qualifiedName));

            return hits[0];
        }

        public EnumConverter(string? style): base
        (
            style,
            GetEnumType
            (
                style ?? throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style))
            )
        )
        {
#if !NETSTANDARD2_1_OR_GREATER
            ParameterExpression
                input  = Expression.Parameter(typeof(string), nameof(input)),
                output = Expression.Parameter(typeof(object).MakeByRefType(), nameof(output)),
                ret    = Expression.Variable(Type, nameof(ret));

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
                            FTryParseGen.MakeGenericMethod(Type),
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

            value = @enum.ToString("g").ToLower();
            return true;
        }

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value) =>
#if NETSTANDARD2_1_OR_GREATER
            Enum.TryParse(Type, input.AsString(), ignoreCase: true, out value);
#else
            FTryParse(input.AsString(), out value);
#endif
    }
}
