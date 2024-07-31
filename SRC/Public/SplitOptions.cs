/********************************************************************************
* SplitOptions.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Specifies how to split URIs
    /// </summary>
    public sealed record SplitOptions
    {
        /// <summary>
        /// The default value.
        /// </summary>
        public static SplitOptions Default { get; } = new SplitOptions();

        /// <summary>
        /// Instruct the system to throw if it encounters 
        /// </summary>
        public bool AllowUnsafeChars { get; init; }

        /// <summary>
        /// Resolve characters that are passed by their hexadecimal values
        /// </summary>
        public bool ConvertHexValues { get; init; } = true;

        /// <summary>
        /// Convert "+" characters to spaces
        /// </summary>
        public bool ConvertSpaces { get; init; } = true;

        /// <summary>
        /// <see cref="System.Text.Encoding"/> to be used when converting hex values. 
        /// </summary>
        public Encoding Encoding { get; init; } = Encoding.UTF8;
    }
}
