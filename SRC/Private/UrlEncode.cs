/********************************************************************************
* UrlEncode.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text;

namespace Solti.Utils.Router.Internals
{
    internal static class UrlEncode
    {
        public static string Encode(string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            using StringBuilder stringBuilder = new(value.Length * 2);

            for (int i = 0; i < value.Length; i++)
            {
                char chr = value[i];

                if (chr.IsArbitraryUnicode())
                {
                    stringBuilder.Append('%');
                    stringBuilder.Append('u');
                    stringBuilder.Append((int)chr);
                }
                else
                {
                    if (chr.IsUrlSafe())
                        stringBuilder.Append(chr);
                    else if (chr == ' ')
                        stringBuilder.Append('+');
                    else
                    {
                        stringBuilder.Append('%');

                        byte[] bytes = encoding.GetBytes(new char[] { chr });
                        for (int j = 0; j < bytes.Length; j++)
                            stringBuilder.Append(bytes[j]);
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}
