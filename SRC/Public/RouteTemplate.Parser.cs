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
            FBaseUrlMatcher  = new("^(\\w+://)?(\\w+(\\.\\w+)+|localhost)(:\\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            FTemplateMatcher = new("{(?<content>.*)}", RegexOptions.Compiled), 
            FTemplateParser  = new("^(?<name>\\w+):(?<converter>\\w+)(?::(?<param>[\\w+.-]+)?)?$", RegexOptions.Compiled);

        private static IEnumerable<RouteSegment> ParseInternal(string template, IReadOnlyDictionary<string, ConverterFactory> converters, SplitOptions splitOptions)
        {
            HashSet<string> paramz = new();

            using PathSplitter segments = PathSplitter.Split(template, splitOptions);

            while (segments.MoveNext())
            {
                string segment = segments.Current.AsString();

                MatchCollection parsedSegment = FTemplateMatcher.Matches(segment);

                switch (parsedSegment.Count)
                {
                    case 0:
                        yield return new RouteSegment(segment, null);
                        break;
                    case 1:
                        Match parsed = FTemplateParser.Match(parsedSegment[0].Groups["content"].Value);
                        if (!parsed.Success)
                             throw new ArgumentException(INVALID_TEMPLATE, nameof(template));

                        string name = GetProperty(nameof(name))!;
                        if (!paramz.Add(name!))
                            throw new ArgumentException(Format(Culture, DUPLICATE_PARAMETER, name), nameof(template));

                        string converter = GetProperty(nameof(converter))!;
                        if (!converters.TryGetValue(converter, out ConverterFactory converterFactory))
                            throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(template));

                        string? param = GetProperty(nameof(param));
                        IConverter converterInst = converterFactory(param);

                        string templateStr = parsedSegment[0].ToString();
                        if (templateStr != segment)
                        {
                            string[] extra = segment.Split
                            (
#if !NETSTANDARD2_1_OR_GREATER
                                new string[] { templateStr },
#else
                                templateStr,
#endif
                                StringSplitOptions.None
                            );
                            converterInst = new ConverterWrapper(converterInst, prefix: extra[0], suffix: extra[1]);
                        }

                        yield return new RouteSegment(name, converterInst);
                        break;

                        string? GetProperty(string name)
                        {
                            Group group = parsed.Groups[name];
                            return group.Success ? group.Value : null;
                        }
                    default:
                        throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR, nameof(template));
                }
            }
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
