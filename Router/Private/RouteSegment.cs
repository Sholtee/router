/********************************************************************************
* RouteSegment.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Router.Internals
{
    /// <summary>
    /// The simplified version of <see cref="TryConvert"/>. The user data shall be embedded.
    /// </summary>
    internal delegate bool SimpleTryConvert(string input, out object? value);

    /// <summary>
    /// Represents a segment of route (for instance "picture" && "id" in case of "/picture/{id:int}") 
    /// </summary>
    /// <param name="Name">The name of segment or variable</param>
    /// <param name="Converter">The converter function</param>
    internal sealed record RouteSegment(string Name, SimpleTryConvert? Converter);
}
