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
        public static IEnumerable<string> Split(string path)
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
                            yield return new string(buffer, 0, pos);
                            pos = 0;
                        }
                        continue;
                    case '%':
                        if (path.Length - i <= 2 || !int.TryParse(path.Substring(i + 1, 2), NumberStyles.HexNumber, null, out int chr))
                            throw new ArgumentException(Resources.INVALID_PATH, nameof(path));

                        c = (char) chr;
                        i += 2;
                        break;
                }
                buffer[pos++] = c;

            }

            if (pos > 0)
                yield return new string(buffer, 0, pos);
        } 
    }
}
