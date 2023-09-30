/********************************************************************************
* IConverter.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Represents an abstract value converter.
    /// </summary>
    /// <remarks>The implementation has to be thread safe.</remarks>
    public interface IConverter
    {
        /// <summary>
        /// The unique identifier of this converter.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Style used during conversion.
        /// </summary>
        string? Style { get; }

        /// <summary>
        /// The target <see cref="System.Type"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Tries to convert the input string to the type this converter responsible for.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="value">The converted value if the conversion was successful.</param>
        bool ConvertToValue(string input, out object? value);

        /// <summary>
        /// Tries to convert the input value to its string representation.
        /// </summary>
        /// <param name="input">The input value</param>
        /// <param name="value">The converted value if the conversion was successful.</param>
        bool ConvertToString(object? input, out string? value);
    }
}
