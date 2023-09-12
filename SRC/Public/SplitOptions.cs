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
        /// The default set.
        /// </summary>
        public static SplitOptions Default { get; } = new SplitOptions();

        /// <summary>
        /// Resolve characters that are passed by their hexadecimal values
        /// </summary>
        public bool ConvertHexValues { get; set; } = true;

        /// <summary>
        /// Convert "+" characters to spaces
        /// </summary>
        public bool ConvertSpaces { get; set; } = true;

        /// <summary>
        /// <see cref="System.Text.Encoding"/> to be used when converting hex values. 
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.Default;
    }
}
