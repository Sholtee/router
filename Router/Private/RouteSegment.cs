﻿/********************************************************************************
* RouteSegment.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Router.Internals
{
    /// <summary>
    /// Represents a segment of route (for instance "picture" && "id" in case of "/picture/{id:int}") 
    /// </summary>
    /// <param name="Name">The name of segment or variable</param>
    /// <param name="Converter">The converter function</param>
    /// <param name="ConverterParam">User data to be passed to the <see cref="Converter"/> function</param>
    internal sealed record RouteSegment(string Name, TryConvert? Converter, string? ConverterParam);
}
