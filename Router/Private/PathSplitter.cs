/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Router.Internals
{
    using Properties;

    internal static class PathSplitter
    {
        /// <summary>
        /// Splits the given path converting hex values if necessary.
        /// </summary>
        public static IEnumerable<T> Split<T>(string path, Func<char[], int, T> convert)
        {
            char[] buffer = new char[path.Length];
            int pos = 0;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                switch (c)
                {
                    case '/':
                        //
                        // Skip leading separator
                        //

                        if (i > 0)
                        {
                            //
                            // Ensure the chunk is not empty
                            //

                            if (pos == 0)
                                throw new ArgumentException(Resources.INVALID_PATH, nameof(path));

                            yield return convert(buffer, pos);
                            pos = 0;
                        }
                        continue;
                    case '%':
                        //
                        // Validate the HEX value.
                        //

                        if (path.Length - i <= 2 || !byte.TryParse(path.Substring(i + 1, 2), NumberStyles.HexNumber, null, out byte chr))
                            throw new ArgumentException(Resources.INVALID_PATH, nameof(path));

                        c = (char) chr;
                        i += 2;
                        break;
                    case '+':
                        c = ' ';
                        break;
                }

                buffer[pos++] = c;
            }

            if (pos > 0)
                yield return convert(buffer, pos);
        }

        public static IEnumerable<string> Split(string path) => Split
        (
            path,
            static (char[] buffer, int len) => new string(buffer, 0, len)
        );
    }
}
