/********************************************************************************
* Converters.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Router
{
    /// <summary>
    /// Tries to convert the input string to a given type.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="userData">User provided data, comes from the route template.</param>
    /// <param name="value">The converted value if the conversion was successful</param>
    public delegate bool TryConvert(string input, string? userData, out object? value);
}
