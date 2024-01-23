/********************************************************************************
* FloatConverter.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class FloatConverter : ConverterBase
    {
        public FloatConverter(string? style): base(style, typeof(double))
        {
            if (style is not null)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not double num)
            {
                value = null;
                return false;
            }

            value = num.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
        {
            if 
            (
                double.TryParse
                (
#if NETSTANDARD2_1_OR_GREATER
                    input,
#else
                    input.AsString(),
#endif
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed
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
