/********************************************************************************
* CharExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Router.Internals
{
    internal static class CharExtensions
    {
        public static bool IsAscii(this char chr) => chr <= '\x007f';

        public static bool IsUrlSafe(this char chr) => 
            (chr.IsAscii() && char.IsLetterOrDigit(chr)) ||
            chr is '-' ||
            chr is '_' ||
            chr is '.' ||
            chr is '!' ||
            chr is '*' ||
            chr is '(' ||
            chr is ')';

        public static bool IsArbitraryUnicode(this char chr) => (chr & 0xff80) is not 0;
    }
}
