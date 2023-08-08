/********************************************************************************
* RouteParser.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solti.Utils.Router.Internals
{
    using Properties;

    /// <summary>
    /// Parses the given route template.
    /// </summary>
    /// <remarks>
    /// Route template looks like: <code>"[/]segment1/{paramName:converter[:userData]}/segment3[/]"</code>
    /// </remarks>
    internal sealed class RouteParser
    {
        private static readonly Regex FTemplateMatcher = new("^{(?<name>\\w+)?(?::(?<converter>\\w+)?)?(?::(?<param>\\w+)?)?}$", RegexOptions.Compiled);

        private readonly IReadOnlyDictionary<string, TryConvert> FConverters;

        public RouteParser(IReadOnlyDictionary<string, TryConvert> converters) => FConverters = converters;

        public IEnumerable<RouteSegment> Parse(string input) => PathSplitter.Split(input).Select(segment =>
        {
            Match match = FTemplateMatcher.Match(segment);
            if (!match.Success)
                return new RouteSegment(segment, null, null);

            string?
                name      = GetMatch(nameof(name)),
                converter = GetMatch(nameof(converter)),
                param     = GetMatch(nameof(param));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.CANNOT_BE_NULL, nameof(name)), nameof(input));

            if (string.IsNullOrEmpty(converter))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.CANNOT_BE_NULL, nameof(converter)), nameof(input));

            if (!FConverters.TryGetValue(converter, out TryConvert converterFn))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.CONVERTER_NOT_FOUND, converter), nameof(input));

            return new RouteSegment(name, converterFn, param); 
            
            string? GetMatch(string name)
            {
                Group group = match.Groups[name];
                return group.Success ? group.Value : null;
            }
        });
    }
}
