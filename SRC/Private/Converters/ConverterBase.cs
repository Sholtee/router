/********************************************************************************
* ConverterBase.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Router.Internals
{
    internal abstract class ConverterBase : IConverter
    {
        public string Id { get; }

        public string? Style { get; }

        public Type Type { get; }

        protected ConverterBase(string id, string? style, Type type)
        {
            Id = id;
            Style = style;
            Type = type;
        }

        protected ConverterBase(string? style, Type type)
        {
            Id = $"{GetType().Name}:{style}";
            Style = style;
            Type = type;
        }

        public override bool Equals(object obj) => obj is ConverterBase other && other.Id == Id;

        public override int GetHashCode() => Id.GetHashCode();

        public abstract bool ConvertToString(object? input, out string? value);

        public abstract bool ConvertToValue(string input, out object? value);
    }
}
