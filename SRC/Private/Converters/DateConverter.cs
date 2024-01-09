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

            value = date.ToString(Style);
            return true;
        }
#if NETSTANDARD2_1_OR_GREATER
        public override bool ConvertToValue(ReadOnlySpan<char> input, out object? value)
#else
        public override bool ConvertToValue(string input, out object? value)
#endif
        {
            if (DateTime.TryParseExact(input, Style, null, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out DateTime parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
