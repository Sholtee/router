/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Router.Internals
{
    using Properties;

    internal static class PathSplitter
    {
        /// <summary>
        /// Splits the given path converting hex values if necessary.
        /// </summary>
        public static IEnumerable<string> Split(string path)
        {
            char[]
                resultBuffer = new char[path.Length],
                hexBuffer = new char[2];

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
                                ThrowInvalidPath();

                            yield return new string(resultBuffer, 0, pos);
                            pos = 0;
                        }
                        continue;
                    case '%':
                        //
                        // Validate the HEX value.
                        //

                        if (path.Length - i > 2)
                        {
                            hexBuffer[0] = path[i + 1];
                            hexBuffer[1] = path[i + 2];

                            if (byte.TryParse(hexBuffer, NumberStyles.HexNumber, null, out byte chr))
                            {
                                c = (char) chr;
                                i += 2;
                                break;
                            }
                        }

                        ThrowInvalidPath();
                        break;
                    case '+':
                        c = ' ';
                        break;
                }

                resultBuffer[pos++] = c;

                void ThrowInvalidPath()
                {
                    ArgumentException ex = new(Resources.INVALID_PATH, nameof(path));
                    ex.Data["Position"] = i;
                    ex.Data["Path"] = path;
                    throw ex;
                }
            }

            if (pos > 0)
                yield return new string(resultBuffer, 0, pos);
        }
    }
}
