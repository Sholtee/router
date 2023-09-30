/********************************************************************************
* RouteParser.cs                                                                *
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

    /// <summary>
    /// Parses the given route template.
    /// </summary>
    /// <remarks>
    /// Route template looks like: <code>"[/]segment1/[prefix]{paramName:converter[:userData]}[suffix]/segment3[/]"</code>
    /// </remarks>
    public sealed class RouteParser
    {
        private static readonly Regex
            FBaseUrlMatcher  = new("^(\\w+://)?(\\w+(\\.\\w+)+|localhost)(:\\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            FTemplateMatcher = new("{(?<content>.*)}", RegexOptions.Compiled), 
            FTemplateParser  = new("^(?<name>\\w+):(?<converter>\\w+)(?::(?<param>[\\w+.-]+)?)?$", RegexOptions.Compiled);

        /// <summary>
        /// Converters.
        /// </summary>
        public IReadOnlyDictionary<string, ConverterFactory> Converters { get; }

        /// <summary>
        /// Creates a new <see cref="RouteParser"/> instance.
        /// </summary>
        public RouteParser(IReadOnlyDictionary<string, ConverterFactory>? converters = null) =>
            Converters = converters ?? DefaultConverters.Instance;

        private IEnumerable<RouteSegment> ParseInternal(string route, SplitOptions? splitOptions = null)
        {
            HashSet<string> paramz = new();

            foreach (string segment in PathSplitter.Split(route, splitOptions).AsEnumerable())
            {
                MatchCollection template = FTemplateMatcher.Matches(segment);

                switch (template.Count)
                {
                    case 0:
                        yield return new RouteSegment(segment, null);
                        break;
                    case 1:
                        Match parsed = FTemplateParser.Match(template[0].Groups["content"].Value);
                        if (!parsed.Success)
                             throw new ArgumentException(INVALID_TEMPLATE, nameof(route));

                        string name = GetProperty(nameof(name))!;
                        if (!paramz.Add(name!))
                            throw new ArgumentException(Format(Culture, DUPLICATE_PARAMETER, name), nameof(route));

                        string converter = GetProperty(nameof(converter))!;
                        if (!Converters.TryGetValue(converter, out ConverterFactory converterFactory))
                            throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(route));

                        string? param = GetProperty(nameof(param));
                        IConverter converterInst = converterFactory(param);

                        string templateStr = template[0].ToString();
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
                        throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR, nameof(route));
                }
            }
        }

        /// <summary>
        /// Parses the given <paramref name="route"/>.
        /// </summary>
        /// <param name="route">Route to be parsed (for instsance: <i>/picute/{id:int}</i>. Must NOT include the base URL.</param>
        /// <param name="splitOptions"></param>
        public ParsedRoute Parse(string route, SplitOptions? splitOptions = null)
        {
            if (FBaseUrlMatcher.IsMatch(route ?? throw new ArgumentNullException(nameof(route))))
                throw new ArgumentException(BASE_URL_NOT_ALLOWED, nameof(route));
             
            List<RouteSegment> segments = new();
            SortedDictionary<string, Type> variables = new();

            foreach (RouteSegment segment in ParseInternal(route, splitOptions))
            {
                segments.Add(segment);
                if (segment.Converter is not null)
                    variables.Add(segment.Name, segment.Converter.Type);
            }

            return new ParsedRoute(segments, variables, route);
        }
    }
}
