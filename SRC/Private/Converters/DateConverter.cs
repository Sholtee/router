/********************************************************************************
* DateConverter.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class DateConverter : ConverterBase
    {
        private static readonly string[] FValidStyles = ["s", "u"];

        public DateConverter(string? style): base(style ?? "s", typeof(DateTime))
        {
            if (Array.IndexOf(FValidStyles, Style!) is -1)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not DateTime date)
            {
                value = null;
                return false;
            }

            value = date.ToString(Style, CultureInfo.InvariantCulture);
            return true;
        }

        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
        {
            if 
            (
                DateTime.TryParseExact
                (
#if NETSTANDARD2_1_OR_GREATER
                    input,
#else
                    input.AsString(),
#endif
                    Style,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                    out DateTime parsed
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
