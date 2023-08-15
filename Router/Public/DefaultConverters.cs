/********************************************************************************
* DefaultConverters.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Router
{
    using Properties;

    /// <summary>
    /// Default converters.
    /// </summary>
    public sealed class DefaultConverters: Dictionary<string, TryConvert>
    {
        private DefaultConverters()
        {
            Add("int", IntConverter);
            Add("str", StrConverter);
        }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static IReadOnlyDictionary<string, TryConvert> Instance { get; } = new DefaultConverters();

        /// <summary>
        /// <see cref="Int32"/> converter
        /// </summary>
        public static bool IntConverter(string input, string? style, out object? val)
        {
            NumberStyles flags = style switch
            {
                "X" or "x" => NumberStyles.HexNumber,
                null => NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign,
                _ => throw new ArgumentException(Resources.INVALID_FORMAT_STYLE, nameof(style))
            };

            if (int.TryParse(input, flags, null, out int parsed))
            {
                val = parsed;
                return true;
            }

            val = null;
            return false;
        }

        /// <summary>
        /// <see cref="String"/> converter
        /// </summary>
        public static bool StrConverter(string input, string? style, out object? val)
        {
            if (style is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_STYLE, nameof(style));

            val = input;
            return true;
        }
    }
}
