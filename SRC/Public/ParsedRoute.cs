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
    public sealed record ParsedRoute(IReadOnlyList<RouteSegment> Segments, IReadOnlyDictionary<string, Type> Variables, string Original);
}
