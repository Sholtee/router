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
    internal class RouteParser
    {
        private static readonly Regex FTemplateMatcher = new("{(?<name>\\w+)?(?::(?<converter>\\w+)?)?(?::(?<param>\\w+)?)?}", RegexOptions.Compiled);

        protected virtual TryConvert Wrap(string prefix, string suffix, TryConvert original) => (string input, string? userData, out object? value) =>
        {
            if (input.Length <= prefix.Length + suffix.Length || !input.StartsWith(prefix, StringComparison) || !input.EndsWith(suffix, StringComparison))
            {
                value = null;
                return false;
            }

            return original(input.Substring(prefix.Length, input.Length - (prefix.Length + suffix.Length)), userData, out value);
        };

        public IReadOnlyDictionary<string, TryConvert> Converters { get; }

        public StringComparison StringComparison { get; }

        public RouteParser(IReadOnlyDictionary<string, TryConvert> converters, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            Converters = converters;
            StringComparison = stringComparison;
        }

        public IEnumerable<RouteSegment> Parse(string input) => PathSplitter.Split(input).AsEnumerable().Select(segment =>
        {
            MatchCollection match = FTemplateMatcher.Matches(segment);

            switch (match.Count)
            {
                case 0:
                    return new RouteSegment(segment, null, null);
                case 1:
                    string? name = GetMatch("name");
                    if (IsNullOrEmpty(name))
                        throw new ArgumentException(Format(Culture, CANNOT_BE_NULL, nameof(name)), nameof(input));

                    string? converter = GetMatch("converter");
                    if (IsNullOrEmpty(converter))
                        throw new ArgumentException(Format(Culture, CANNOT_BE_NULL, nameof(converter)), nameof(input));

                    if (!Converters.TryGetValue(converter, out TryConvert converterFn))
                        throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(input));

                    if (match[0].ToString() != segment)
                    {
                        string[] extra = segment.Split(match[0].ToString(), StringSplitOptions.None);
                        converterFn = Wrap(extra[0], extra[1], converterFn);
                    }

                    return new RouteSegment(name, converterFn, GetMatch("param"));
                default:
                    throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR);
            }
            
            string? GetMatch(string name)
            {
                Group group = match[0].Groups[name];
                return group.Success ? group.Value : null;
            }
        });
    }
}
