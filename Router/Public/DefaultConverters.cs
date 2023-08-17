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
    public sealed class DefaultConverters: Dictionary<string, ConverterFactory>
    {
        private DefaultConverters()
        {
            Add("int", IntConverterFactory);
            Add("str", StrConverterFactory);
        }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static IReadOnlyDictionary<string, ConverterFactory> Instance { get; } = new DefaultConverters();

        /// <summary>
        /// <see cref="Int32"/> converter
        /// </summary>
        public static TryConvert IntConverterFactory(string? style)
        {
            NumberStyles flags = style switch
            {
                "X" or "x" => NumberStyles.HexNumber,
                null => NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign,
                _ => throw new ArgumentException(Resources.INVALID_FORMAT_STYLE, nameof(style))
            };

            return IntConverter;

            bool IntConverter(string input, out object? val)
            {
                if (int.TryParse(input, flags, null, out int parsed))
                {
                    val = parsed;
                    return true;
                }

                val = null;
                return false;
            }
        }

        /// <summary>
        /// <see cref="String"/> converter
        /// </summary>
        public static TryConvert StrConverterFactory(string? style)
        {
            if (style is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_STYLE, nameof(style));

            return StrConverter;

            static bool StrConverter(string input, out object? val)
            {
                val = input;
                return true;
            }
        }
    }
}
