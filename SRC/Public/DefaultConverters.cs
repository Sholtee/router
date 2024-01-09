/********************************************************************************
* DefaultConverters.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Router
{
    using Internals;

    /// <summary>
    /// Default converters.
    /// </summary>
    public sealed class DefaultConverters: Dictionary<string, ConverterFactory>
    {
        private DefaultConverters()
        {
            Add("enum", static style => new EnumConverter(style));
            Add("guid", static style => new GuidConverter(style));
            Add("int",  static stlye => new IntConverter(stlye));
            Add("str",  static style => new StrConverter(style));
            Add("date", static style => new DateConverter(style));
        }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static DefaultConverters Instance { get; } = new DefaultConverters();
    }
}
