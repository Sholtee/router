/********************************************************************************
* EnumConverter.cs.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class EnumConverter : ConverterBase
    {
        public Type EnumType { get; }

        public EnumConverter(string? style): base(style)
        {
            if (style is null)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));

            //
            // Types declared outside of System.Private.CoreLib.dll can be loaded by assembly qualified name only
            // so we have to overcome this limitation. It's slow but won't run frequently
            // 

            List<Type> hits = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Select(asm => asm.GetType(style, throwOnError: false))
                .Where(t => t is not null)
                .ToList();
            if (hits.Count is not 1 || !hits[0].IsEnum)
                throw new ArgumentException(string.Format(Culture, INVALID_FORMAT_STYLE, style), nameof(style));

            EnumType = hits[0];
        }

        public override bool ConvertToString(object? input, out string? value)
        {
            if (input is not Enum @enum)
            {
                value = null;
                return false;
            }

            value = @enum.ToString("G");
            return true;
        }

        public override bool ConvertToValue(string input, out object? value) =>
            Enum.TryParse(EnumType, input, ignoreCase: true, out value);
    }
}
