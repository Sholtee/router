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
            Add("enum", style => new EnumConverter(style));
            Add("guid", style => new GuidConverter(style));
            Add("int",  stlye => new IntConverter(stlye));
            Add("str",  style => new StrConverter(style));
        }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static DefaultConverters Instance { get; } = new DefaultConverters();
    }
}
