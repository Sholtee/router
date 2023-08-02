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
        private static readonly Regex FTemplateMatcher = new("^{(?<name>\\w+):(?<converter>\\w+)(?::(?<param>\\w+))?}$", RegexOptions.Compiled);

        private readonly IReadOnlyDictionary<string, TryConvert> FConverters;

        public RouteParser(IReadOnlyDictionary<string, TryConvert> converters) => FConverters = converters;

        public IEnumerable<RouteSegment> Parse(string input) => PathSplitter.Split(input).Select(segment =>
        {
            Match match = FTemplateMatcher.Match(segment);
            if (!match.Success)
                return new RouteSegment(segment, null);

            string
                name      = match.Groups[nameof(name)].Value,
                converter = match.Groups[nameof(converter)].Value,
                param     = match.Groups[nameof(param)].Value;

            if (!FConverters.TryGetValue(converter, out TryConvert converterFnCore))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.CONVERTER_NOT_FOUND, converter), nameof(input));

            return new RouteSegment
            (
                name,
                (string input, out object? value) => converterFnCore(input, param, out value)
            );    
        });
    }
}
