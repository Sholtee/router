/********************************************************************************
* ParsedRoute.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Represents a parsed route.
    /// </summary>
    public sealed class ParsedRoute
    {
        internal ParsedRoute(IEnumerable<RouteSegment> segments, string template)
        {
            List<RouteSegment> segmentList = new();
            SortedDictionary<string, Type> paramz = new();

            foreach (RouteSegment segment in segments)
            {
                segmentList.Add(segment);

                if (segment.Converter is not null)
                    paramz.Add(segment.Name, segment.Converter.Type);
            }

            Segments   = segmentList;
            Parameters = paramz;
            Template   = template;
        }

        /// <summary>
        /// Parsed segments.
        /// </summary>
        public IReadOnlyList<RouteSegment> Segments { get; }

        /// <summary>
        /// Parameters declared in  <see cref="Template"/>
        /// </summary>
        public IReadOnlyDictionary<string, Type> Parameters { get; }

        /// <summary>
        /// The original template.
        /// </summary>
        public string Template { get; }
    }
}
