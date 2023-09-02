/********************************************************************************
* ConverterBase.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Router.Internals
{
    internal abstract class ConverterBase : IConverter
    {
        public string Id { get; }

        public string? Style { get; }

        protected ConverterBase(string id, string? style)
        {
            Id = id;
            Style = style;
        }

        protected ConverterBase(string? style)
        {
            Id = $"{GetType().Name}:{style}";
            Style = style;
        }

        public override bool Equals(object obj) => obj is ConverterBase other && other.Id == Id;

        public override int GetHashCode() => Id.GetHashCode();

        public abstract bool ConvertToString(object? input, out string? value);

        public abstract bool ConvertToValue(string input, out object? value);
    }
}
