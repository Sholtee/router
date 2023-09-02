/********************************************************************************
* GuidConverter.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class GuidConverter : ConverterBase
    {
        public GuidConverter(string? style): base(style ?? "N")
        {
            if (!new List<string> { "N", "D", "B", "P", "X" }.Contains(Style!))
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not Guid guid)
            {
                value = null;
                return false;
            }

            value = guid.ToString(Style);
            return true;
        }

        public override bool ConvertToValue(string input, out object? value)
        {
            if (Guid.TryParseExact(input, Style, out Guid parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
