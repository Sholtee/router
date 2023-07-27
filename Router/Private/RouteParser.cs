/********************************************************************************
* RouteParser.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Router.Internals
{
    using Properties;

    internal delegate bool TryConvert(string input, out object? value);

    internal delegate bool TryConvertEx(string input, string? userData, out object? value);


    /// <summary>
    /// Represents a segment of route (for instance "picture" && "id" in case of "/picture/{id:int}") 
    /// </summary>
    /// <param name="Name">The name of segment or variable</param>
    /// <param name="Converter">The converter function</param>
    internal sealed record RouteSegment(string Name, TryConvert? Converter);

    internal static class RouteParser
    {
        private static readonly Regex FTemplateMatcher = new("^{(?<name>\\w+):(?<converter>\\w+)(?::(?<param>\\w+))?}$", RegexOptions.Compiled);

        internal static IEnumerable<RouteSegment> Parse(string input, IReadOnlyDictionary<string, TryConvertEx> converters) => PathSplitter.Split(input).Select(segment =>
        {
            Match match = FTemplateMatcher.Match(segment);
            if (!match.Success)
                return new RouteSegment(segment, null);

            string
                name      = match.Groups[nameof(name)].Value,
                converter = match.Groups[nameof(converter)].Value,
                param     = match.Groups[nameof(param)].Value;

            if (!converters.TryGetValue(converter, out TryConvertEx converterFnCore))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.CONVERTER_NOT_FOUND, converter), nameof(input));

            return new RouteSegment
            (
                name,
                (string input, out object? value) => converterFnCore(input, param, out value)
            );    
        });
    }
}
