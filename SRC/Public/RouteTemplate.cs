/********************************************************************************
* RouteTemplate.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;
    using Properties;

    /// <summary>
    /// Route template related functions.
    /// </summary>
    public static class RouteTemplate
    {
        private static readonly MethodInfo FTryGetValue =
        (
            (MethodCallExpression)
            (
                (Expression<Action<IReadOnlyDictionary<string, object?>, object?>>)
                (
                    static (dict, val) => dict.TryGetValue(null!, out val)
                )
            ).Body
        ).Method;

        private static readonly MethodInfo FTryConvertToString =
        (
            (MethodCallExpression)
            (
                (Expression<Action<IConverter, string?>>)
                (
                    static (conv, val) => conv.ConvertToString(null!, out val)
                )
            ).Body
        ).Method;

        private static readonly MethodInfo FConcat =
        (
            (MethodCallExpression)
            (
                (Expression<Action>)
                (
                    static () => string.Concat(null!, null!)
                )
            ).Body
        ).Method;

        /// <summary>
        /// Creates the template compiler function that interpolates parameters in the encapsulated route template.
        /// </summary>
        public static RouteTemplateCompiler CreateCompiler(string template, IReadOnlyDictionary<string, ConverterFactory>? converters = null)
        {
            ParameterExpression paramz = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(paramz));
            StringBuilder sb = new();

            Expression? ret = null;

            foreach (RouteSegment segment in new RouteParser(converters).Parse(template))
            {
                sb.Append('/');

                if (segment.Converter is null)
                {
                    sb.Append(HttpUtility.UrlEncode(segment.Name));
                }
                else
                {
                    ParameterExpression
                        objValue = Expression.Parameter(typeof(object), nameof(objValue)),
                        strValue = Expression.Parameter(typeof(string), nameof(strValue));

                    Expression interpolation = Expression.Block
                    (
                        type: typeof(string),
                        variables: new ParameterExpression[]
                        {
                            objValue,
                            strValue
                        },
                        Expression.IfThen
                        (
                            Expression.Or
                            (
                                Expression.Not
                                (
                                    Expression.Call
                                    (
                                        paramz,
                                        FTryGetValue,
                                        Expression.Constant(segment.Name),
                                        objValue
                                    )
                                ),
                                Expression.Not
                                (
                                    Expression.Call
                                    (
                                        Expression.Constant(segment.Converter),
                                        FTryConvertToString,
                                        objValue,
                                        strValue
                                    )
                                )
                            ),
                            ifTrue: Expression.Throw
                            (
                                Expression.Constant(new ArgumentException(Resources.INAPPROPRIATE_PARAMETERS, nameof(paramz)))
                            )
                        ),
                        strValue
                    );

                    AppendConst();

                    ret = Expression.Call(null, FConcat, ret, interpolation);
                }
            }

            if (sb.Length is 0 && ret is null)
                sb.Append('/');    

            if (sb.Length > 0)
                AppendConst();

            Expression<RouteTemplateCompiler> compilerExpr = Expression.Lambda<RouteTemplateCompiler>(ret, paramz);

            Debug.WriteLine(compilerExpr.GetDebugView());

            return compilerExpr.Compile();

            void AppendConst()
            {
                Debug.Assert(sb.Length > 0, "There is nothing to append to");

                ConstantExpression pre = Expression.Constant(sb.ToString());
                ret = ret is null ? pre : Expression.Call(null, FConcat, ret, pre);

                sb.Clear();
            }
        }
    }
}
