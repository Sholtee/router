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
        private static readonly Regex FTemplateMatcher = new("^(?<prefix>.+)?{(?<name>\\w+)?(?::(?<converter>\\w+)?)?(?::(?<param>\\w+)?)?}(?<suffix>.+)?$", RegexOptions.Compiled);

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

        public IEnumerable<RouteSegment> Parse(string input) => PathSplitter.Split(input).Select(segment =>
        {
            MatchCollection match = FTemplateMatcher.Matches(segment);
            if (match.Count is 0)
                return new RouteSegment(segment, null, null);

            if (match.Count is not 1)
                throw new ArgumentException(TOO_MANY_PARAM_DESCRIPTOR);

            string? name = GetMatch("name");
            if (IsNullOrEmpty(name))
                throw new ArgumentException(Format(Culture, CANNOT_BE_NULL, nameof(name)), nameof(input));

            string? converter = GetMatch("converter");
            if (IsNullOrEmpty(converter))
                throw new ArgumentException(Format(Culture, CANNOT_BE_NULL, nameof(converter)), nameof(input));

            if (!Converters.TryGetValue(converter, out TryConvert converterFn))
                throw new ArgumentException(Format(Culture, CONVERTER_NOT_FOUND, converter), nameof(input));

            string
                prefix = GetMatch("prefix", Empty)!,
                suffix = GetMatch("suffix", Empty)!;

            if (!IsNullOrEmpty(prefix) || !IsNullOrEmpty(suffix))
                converterFn = Wrap(prefix, suffix, converterFn);

            return new RouteSegment(name, converterFn, GetMatch("param")); 
            
            string? GetMatch(string name, string? @default = null)
            {
                Group group = match[0].Groups[name];
                return group.Success ? group.Value : @default;
            }
        });
    }
}
