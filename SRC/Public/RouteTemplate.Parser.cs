/********************************************************************************
* RouteTemplate.Parser.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static System.String;

namespace Solti.Utils.Router
{
    using Internals;

    using static Properties.Resources;

    public static partial class RouteTemplate
    {
        private static readonly Regex
            FBaseUrlMatcher  = new("^(\\w+:\\/\\/)?(\\w+(\\.\\w+)+|localhost)(:\\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            FTemplateMatcher = new("{(?<content>.*)}", RegexOptions.Compiled), 
            FTemplateParser  = new("^(?<name>\\w+):(?<converter>\\w+)(?::(?<param>[\\w+.-]+)?)?$", RegexOptions.Compiled);

        private static IReadOnlyList<RouteSegment> ParseInternal(string template, IReadOnlyDictionary<string, ConverterFactory> converters, SplitOptions splitOptions)
        {
            HashSet<string> paramz = [];
            List<RouteSegment> segments = [];

            using PathSplitter pathSplitter = PathSplitter.Split(template.AsSpan(), splitOptions);

            while (pathSplitter.MoveNext())
            {
                string segment = pathSplitter.Current.ToString();

                MatchCollection parsedSegment = FTemplateMatcher.Matches(segment);

                switch (parsedSegment.Count)
                {
                    case 0:
                        segments.Add(new RouteSegment(segment, null));
                        break;
                    case 1:
                        Match parsed = FTemplateParser.Match(parsedSegment[0].GetGroup("content"));
                        if (!parsed.Success)
                             throw new ArgumentException(INVALID_TEMPLATE, nameof(template));

                        string name = parsed.GetGroup("name")!;
                        if (!paramz.Add(name))
                            throw new ArgumentException(Format(Culture, DUPLICATE_PARAMETER, name), nameof(template));

                        string converter = parsed.GetGroup("converter")!;
                        if (!converters.TryGetValue(converter, out ConverterFactory converterFactory))
                            throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(template));

                        IConverter converterInst = converterFactory(parsed.GetGroup("param"));

                        string templateStr = parsedSegment[0].ToString();
                        if (templateStr != segment)
                        {
                            string[] extra = segment.Split
                            (
#if NETSTANDARD2_1_OR_GREATER
                                templateStr,                          
#else
                                new string[] { templateStr },
#endif
                                StringSplitOptions.None
                            );
                            converterInst = new ConverterWrapper(converterInst, prefix: extra[0], suffix: extra[1]);
                        }

                        segments.Add(new RouteSegment(name, converterInst));
                        break;     
                    default:
                        throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR, nameof(template));
                }
            }

            return segments;
        }

        /// <summary>
        /// Parses the given route template.
        /// </summary>
        /// <remarks>
        /// A route template looks like: <code>"[/]segment1/[prefix]{paramName:converter[:userData]}[suffix]/segment3[/]"</code>
        /// </remarks>
        /// <param name="template">Route template to be parsed (for instsance: <i>/picute/{id:int}</i>. Must NOT include the base URL.</param>
        /// <param name="converters"></param>
        /// <param name="splitOptions"></param>
        public static ParsedRoute Parse(string template, IReadOnlyDictionary<string, ConverterFactory>? converters = null, SplitOptions? splitOptions = null)
        {
            if (FBaseUrlMatcher.IsMatch(template ?? throw new ArgumentNullException(nameof(template))))
                throw new ArgumentException(BASE_URL_NOT_ALLOWED, nameof(template));

            return new ParsedRoute
            (
                ParseInternal
                (
                    template,
                    converters ?? DefaultConverters.Instance,
                    splitOptions ?? SplitOptions.Default
                ),
                template
            );
        }
    }
}
