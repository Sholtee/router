/********************************************************************************
* ConverterWrapper.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router.Internals
{
    internal sealed class ConverterWrapper : ConverterBase
    {
        public IConverter Wrapped { get; }

        public string Prefix { get; }

        public string Suffix { get; }

        public ConverterWrapper(IConverter toBeWrapped, string prefix, string suffix): base($"[{prefix}]{toBeWrapped.Id}[{suffix}]", null, toBeWrapped.Type)
        {
            Wrapped = toBeWrapped;
            Prefix = prefix;
            Suffix = suffix;
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (Wrapped.ConvertToString(input, out value))
            {
                value = Prefix + value + Suffix;
                return true;
            }
            return false;
        }

        public override bool ConvertToValue(string input, out object? value)
        {
            if 
            (
                input.Length > Prefix.Length + Suffix.Length &&
                input.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
                input.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)
            )
                return Wrapped.ConvertToValue
                (
                    input.Substring(Prefix.Length, input.Length - Prefix.Length - Suffix.Length),
                    out value
                );

            value = null;
            return false;
        }
    }
}
