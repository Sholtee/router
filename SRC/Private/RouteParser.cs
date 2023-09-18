/********************************************************************************
* RouteParser.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using static System.String;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    /// <summary>
    /// Parses the given route template.
    /// </summary>
    /// <remarks>
    /// Route template looks like: <code>"[/]segment1/[prefix]{paramName:converter[:userData]}[suffix]/segment3[/]"</code>
    /// </remarks>
    internal sealed class RouteParser
    {
        private static readonly Regex
            FTemplateMatcher = new("{(?<content>.*)}"), 
            FTemplateParser  = new("^(?<name>\\w+):(?<converter>\\w+)(?::(?<param>[\\w+.-]+)?)?$", RegexOptions.Compiled);

        public IReadOnlyDictionary<string, ConverterFactory> Converters { get; }

        public RouteParser(IReadOnlyDictionary<string, ConverterFactory> converters) => Converters = converters;

        public IEnumerable<RouteSegment> Parse(string input, SplitOptions? splitOptions = null)
        {
            HashSet<string> paramz = new();

            return PathSplitter.Split(input, splitOptions).AsEnumerable().Select(segment =>
            {
                MatchCollection template = FTemplateMatcher.Matches(segment);

                switch (template.Count)
                {
                    case 0:
                        return new RouteSegment(segment, null);
                    case 1:
                        Match parsed = FTemplateParser.Match(template[0].Groups["content"].Value);
                        if (!parsed.Success)
                             throw new ArgumentException(INVALID_TEMPLATE, nameof(input));

                        string name = GetMatch(nameof(name))!;
                        if (!paramz.Add(name!))
                            throw new ArgumentException(Format(Culture, DUPLICATE_PARAMETER, name), nameof(input));

                        string converter = GetMatch(nameof(converter))!;
                        if (!Converters.TryGetValue(converter, out ConverterFactory converterFactory))
                            throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(input));

                        string? param = GetMatch(nameof(param));
                        IConverter converterInst = converterFactory(param);

                        if (template[0].ToString() != segment)
                        {
                            string[] extra = segment.Split
                            (
#if !NETSTANDARD2_1_OR_GREATER
                                new string[] { template[0].ToString() },
#else
                                template[0].ToString(),
#endif
                                StringSplitOptions.None
                            );
                            converterInst = new ConverterWrapper(converterInst, prefix: extra[0], suffix: extra[1]);
                        }

                        return new RouteSegment(name, converterInst);

                        string? GetMatch(string name)
                        {
                            Group group = parsed.Groups[name];
                            return group.Success ? group.Value : null;
                        }
                    default:
                        throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR, nameof(input));
                }
            });
        }
    }
}
