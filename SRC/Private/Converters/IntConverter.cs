/********************************************************************************
* IntConverter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class IntConverter : ConverterBase
    {
        public NumberStyles StyleFlag { get; }

        public IntConverter(string? style): base(style, typeof(int))
        {
            StyleFlag = style switch
            {
                "X" or "x" => NumberStyles.HexNumber,
                null => NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign,
                _ => throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style))
            };
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not int num)
            {
                value = null;
                return false;
            }

            value = num.ToString(Style, CultureInfo.InvariantCulture);
            return true;
        }

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
        {
            if 
            (
                int.TryParse
                (
                    input
#if !NETSTANDARD2_1_OR_GREATER
                        .ToString()
#endif
                    ,
                    StyleFlag,
                    CultureInfo.InvariantCulture,
                    out int parsed
                )
            )
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
