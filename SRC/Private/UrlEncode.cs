/********************************************************************************
* UrlEncode.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Text;

namespace Solti.Utils.Router.Internals
{
    internal static class UrlEncode
    {
        public static string Encode(string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            char[] output = new char[value.Length * 2];
            int charsWritten = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char chr = value[i];

                if (chr.IsArbitraryUnicode())
                {
                    AppendChar('%');
                    AppendChar('u');
                    AppendOrdinal(chr);
                }
                else
                {
                    if (chr.IsUrlSafe())
                        AppendChar(chr);
                    else if (chr == ' ')
                        AppendChar('+');
                    else
                    {
                        AppendChar('%');

                        byte[] bytes = encoding.GetBytes(new char[] { chr });          
                        for (int j = 0; j < bytes.Length; j++)
                            AppendOrdinal(bytes[j]);
                    }
                }
            }

            return new string(output, 0, charsWritten);

            void AppendChar(char chr)
            {
                if (charsWritten == output.Length)
                    Array.Resize(ref output, output.Length * 2);

                output[charsWritten++] = chr;
            }

            void AppendOrdinal(int i)
            {
                string hex = i.ToString("X");
                for (i = 0; i < hex.Length; i++)
                    AppendChar(hex[i]);
            }
        }
    }
}
