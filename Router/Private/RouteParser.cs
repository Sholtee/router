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

    /// <summary>
    /// Represents a segment of route (for instance "getpictures" && "id" in case of "/getpictures/id:(\d+)") 
    /// </summary>
    /// <param name="Name">The name of segment or variable</param>
    /// <param name="Regex">The related <see cref="System.Text.RegularExpressions.Regex"/> if we have to match by expression</param>
    internal sealed record RouteSegment(string Name, Regex? Regex);

    internal static class RouteParser
    {
        private static readonly Regex FTemplateMatcher = new("^(?<name>\\w+):\\((?<regex>.+)\\)(?<options>i?)$", RegexOptions.Compiled);

        internal static IEnumerable<RouteSegment> Parse(string input) => PathSplitter.Split(input).Select(static segment =>
        {
            Match match = FTemplateMatcher.Match(segment);
            if (!match.Success)
                return new RouteSegment(segment, null);

            string
                expr = match.Groups["regex"].Value,
                opts = match.Groups["options"].Value;

            RegexOptions regexOptions = RegexOptions.Compiled;
            if (opts?.Contains("i") is true)
                regexOptions |= RegexOptions.IgnoreCase;

            Regex regex = new(expr, regexOptions);
            if (regex.GetGroupNames().Length > 1)
                throw new ArgumentException(Resources.CAPTURING_GROUPS_NOT_SUPPORTED, nameof(input));

            return new RouteSegment(match.Groups["name"].Value, regex);
        });
    }
}
