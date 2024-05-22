/********************************************************************************
* EnumConverter.cs.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    using static Properties.Resources;

    internal sealed class EnumConverter : ConverterBase
    {
        #region Helpers
        private delegate bool ConvertStringDelegate(ReadOnlySpan<char> str, out object ret);

        private delegate string AsStringDelegate(ReadOnlySpan<char> input);

        private static string SpanToString(ReadOnlySpan<char> span) => span.ToString();

        private static ConvertStringDelegate CreateConverter(Type type)
        {
            ParameterExpression
                input  = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input)),
                result = Expression.Parameter(typeof(object).MakeByRefType(), nameof(result)),
                ret    = Expression.Parameter(type, nameof(ret));

            MethodCallExpression tryParseExpr;

            MethodInfo? tryParse = typeof(Enum)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault
                (
                    static m =>
                    {
                        if (m.Name != nameof(Enum.TryParse) || !m.ContainsGenericParameters)
                            return false;

                        ParameterInfo[] paramz = m.GetParameters();
                        if (paramz.Length != 3)
                            return false;

                        return
                            paramz[0].ParameterType == typeof(ReadOnlySpan<char>) &&
                            paramz[1].ParameterType == typeof(bool) &&
                            paramz[2].ParameterType == m.GetGenericArguments()[0].MakeByRefType();
                    }
                );
            if (tryParse is not null)
            {
                tryParseExpr = Expression.Call
                (
                    tryParse.MakeGenericMethod(type),
                    input,
                    Expression.Constant(true),
                    ret
                );
            }
            else
            {
                tryParse = MethodInfoExtractor
                    .Extract<int>(static val => Enum.TryParse(default, false, out val))
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(type);

                tryParseExpr = Expression.Call
                (
                    tryParse,
                    Expression.Invoke
                    (
                        Expression.Constant((AsStringDelegate) SpanToString),
                        input
                    ),
                    Expression.Constant(true),
                    ret
                );
            }

            Expression<ConvertStringDelegate> convertStringExpr = Expression.Lambda<ConvertStringDelegate>
            (
                Expression.Block
                (
                    type: typeof(bool),
                    [ret],
                    Expression.Condition
                    (
                        tryParseExpr,
                        ifTrue: Expression.Block
                        (
                            type: typeof(bool),
                            Expression.Assign
                            (
                                result,
                                Expression.Convert(ret, typeof(object))
                            ),
                            Expression.Constant(true)
                        ),
                        ifFalse: Expression.Constant(false)
                    )
                ),
                input,
                result
            );
            Debug.WriteLine(convertStringExpr.GetDebugView());
            return convertStringExpr.Compile();
        }

        private readonly ConvertStringDelegate FConvert;

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
        #endregion

        public EnumConverter(string? style): base
        (
            style,
            GetEnumType
            (
                style ?? throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style))
            )
        ) => FConvert = CreateConverter(Type);

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

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value) => FConvert(input, out value);
    }
}
