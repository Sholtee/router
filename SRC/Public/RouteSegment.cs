/********************************************************************************
* RouteSegment.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Represents a segment of route (for instance <i>RouteSegment("picture", null)</i> in case of "/picture" or <i>RouteSegment("id", IntConverter(...))</i> in case of "/{id:int}") 
    /// </summary>
    public sealed record RouteSegment(string Name, IConverter? Converter = null);
}
