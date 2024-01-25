/********************************************************************************
* RouteTemplate.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Solti.Utils.Router
{
    using Internals;
    using Primitives;
    using Properties;

    /// <summary>
    /// Route template related functions.
    /// </summary>
    public static partial class RouteTemplate
    {
        private static readonly MethodInfo 
            FTryGetValue = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>(static (dict, val) => dict.TryGetValue(null!, out val)),
            FEncode      = MethodInfoExtractor.Extract(static () => UrlEncode.Encode(string.Empty, Encoding.Default)),
            FConvert     = MethodInfoExtractor.Extract<IConverter, string?>(static (conv, val) => conv.ConvertToString(null!, out val)),
            FConcat      = MethodInfoExtractor.Extract(static () => string.Concat(new string[0]));

        private static readonly Regex FFirstTierParser = new
        (
            "^(?<baseUrl>(?:\\w+://)?(?:\\w+(?:\\.\\w+)+|localhost)(?::\\d+)?)?(?<path>.+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Creates the template compiler function that interpolates parameters in the encapsulated route template.
        /// </summary>
        public static RouteTemplateCompiler CreateCompiler(string template, IReadOnlyDictionary<string, ConverterFactory>? converters = null, SplitOptions? splitOptions = null)
        {
            Match preProcessedTemplate = FFirstTierParser.Match(template ?? throw new ArgumentNullException(nameof(template)));
            if (!preProcessedTemplate.Success || preProcessedTemplate.Groups.Cast<Group>().All(static grp => string.IsNullOrEmpty(grp.Value)))
                throw new ArgumentException(Resources.INVALID_TEMPLATE, nameof(template));

            using StringBuilder sb = new();

            string? baseUrl = preProcessedTemplate.GetGroup(nameof(baseUrl));
            if (baseUrl is not null)
                sb.Append(baseUrl);

            List<Expression> arrayInitializers = new();

            ParameterExpression paramz = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(paramz));
           
            splitOptions ??= SplitOptions.Default;
            converters ??= DefaultConverters.Instance;

            string? path = preProcessedTemplate.GetGroup(nameof(path));
            if (path is not null)
            {
                foreach(RouteSegment segment in ParseInternal(path, converters, splitOptions))
                {
                    sb.Append('/');

                    if (segment.Converter is null)
                    {
                        sb.Append(UrlEncode.Encode(segment.Name, splitOptions.Encoding));
                    }
                    else
                    {
                        arrayInitializers.Add
                        (
                            Expression.Constant(sb.ToString())
                        );
                        sb.Clear();

                        ParameterExpression
                            objValue = Expression.Parameter(typeof(object), nameof(objValue)),
                            strValue = Expression.Parameter(typeof(string), nameof(strValue));

                        arrayInitializers.Add
                        (
                            Expression.Call
                            (
                                FEncode,
                                Expression.Block
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
                                                    FConvert,
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
                                ),
                                Expression.Constant(splitOptions.Encoding, typeof(Encoding))
                            )
                        );
                    }
                }
            }

            if (sb.Length is 0 && arrayInitializers.Count is 0)
                sb.Append('/');    

            if (sb.Length > 0)
                arrayInitializers.Add
                (
                    Expression.Constant(sb.ToString())
                );

            Expression<RouteTemplateCompiler> compilerExpr = Expression.Lambda<RouteTemplateCompiler>
            (
                //
                // UrlEncode() cannot be called here as it would escape the "/" characters, too
                //

                Expression.Call
                (
                    FConcat,
                    Expression.NewArrayInit(typeof(string), arrayInitializers)
                ),
                paramz
            );

            Debug.WriteLine(compilerExpr.GetDebugView());

            return compilerExpr.Compile();
        }
    }
}
