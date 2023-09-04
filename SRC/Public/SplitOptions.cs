/********************************************************************************
* SplitOptions.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using System;

namespace Solti.Utils.Router
{
    /// <summary>
    /// Specifies how to split URIs
    /// </summary>
    [Flags]
    public enum SplitOptions
    {
        /// <summary>
        /// No options provided.
        /// </summary>
        None = 0,

        /// <summary>
        /// The default set.
        /// </summary>
        Default = ConvertHexValues | ConvertSpaces,

        /// <summary>
        /// Resolve characters that are passed by their hexadecimal values
        /// </summary>
        ConvertHexValues = 1 << 0,

        /// <summary>
        /// Convert "+" characters to spaces
        /// </summary>
        ConvertSpaces = 1 << 1
    }
}
