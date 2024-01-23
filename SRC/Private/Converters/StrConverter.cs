/********************************************************************************
* StrConverter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class StrConverter : ConverterBase
    {
        public StrConverter(string? style): base(style, typeof(string))
        {
            if (style is not null)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not string str)
            {
                value = null;
                return false;
            }

            value = str;
            return true;
        }

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
        {
            value = input.AsString();
            return true;
        }
    }
}
