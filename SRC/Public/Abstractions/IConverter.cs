/********************************************************************************
* IConverter.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Router
{
    /// <summary>
    /// Represents an abstract value converter.
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// The unique identifier of the converter. The value of <see cref="Style"/> should not affect the value of <see cref="Id"/>.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Style used during conversion.
        /// </summary>
        string? Style { get; }

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
