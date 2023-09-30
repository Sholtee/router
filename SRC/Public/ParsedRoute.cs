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
            SortedDictionary<string, Type> variables = new();

            foreach (RouteSegment segment in segments)
            {
                segmentList.Add(segment);

                if (segment.Converter is not null)
                    variables.Add(segment.Name, segment.Converter.Type);
            }

            Segments  = segmentList;
            Variables = variables;
            Template  = template;
        }

        /// <summary>
        /// Parse segments.
        /// </summary>
        public IReadOnlyList<RouteSegment> Segments { get; }

        /// <summary>
        /// Variables declared in the <see cref="Template"/>
        /// </summary>
        public IReadOnlyDictionary<string, Type> Variables { get; }

        /// <summary>
        /// The original parsed route template
        /// </summary>
        public string Template { get; }
    }
}
