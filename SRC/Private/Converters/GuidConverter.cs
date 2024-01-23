/********************************************************************************
* GuidConverter.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class GuidConverter : ConverterBase
    {
        private static readonly string[] FValidStyles = ["N", "D", "B", "P", "X"];

        public GuidConverter(string? style): base(style ?? "N", typeof(Guid))
        {
            if (Array.IndexOf(FValidStyles, Style!) is -1)
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

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
        {
            if 
            (
                Guid.TryParseExact
                (
#if NETSTANDARD2_1_OR_GREATER
                    input,
#else
                    input.AsString(),
#endif
                    Style,
                    out Guid parsed
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
