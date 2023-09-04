/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed class PathSplitter
    {
        private readonly string FPath;

        private readonly SplitOptions FOptions;

        private readonly char[]
            FResultBuffer,
            FHexBufffer;

        private int
            FPosition,
            FIndex;

        private PathSplitter(string path, SplitOptions options)
        {
            FPath = path;
            FResultBuffer = new char[path.Length];
            FHexBufffer = options.HasFlag(SplitOptions.ConvertHexValues) ? new char[2] : null!;
            FOptions = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InvalidOperationException InvalidPath(string err)
        {
            InvalidOperationException ex = new(string.Format(Culture, INVALID_PATH, err));
            ex.Data["Position"] = FPosition;
            ex.Data["Path"] = FPath;
            return ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            FPosition = 0;

            for (; ; FIndex++)
            {
                if (FIndex == FPath.Length)
                    return FPosition > 0;

                char c = FPath[FIndex];
                switch (c)
                {
                    case '/':
                        //
                        // Skip leading separator
                        //

                        if (FIndex is 0)
                            continue;

                        //
                        // Ensure the chunk is not empty
                        //

                        if (FPosition is 0)
                            throw InvalidPath(EMPTY_CHUNK);

                        FIndex++;
                        return true;
                    case '%' when FOptions.HasFlag(SplitOptions.ConvertHexValues):
                        //
                        // Validate the HEX value.
                        //

                        if (FPath.Length - FIndex > 2)
                        {
                            FHexBufffer[0] = FPath[FIndex + 1];
                            FHexBufffer[1] = FPath[FIndex + 2];

                            if (byte.TryParse(FHexBufffer, NumberStyles.HexNumber, null, out byte chr))
                            {
                                c = (char) chr;
                                FIndex += 2;
                                break;
                            }
                        }

                        throw InvalidPath(INVALID_HEX);
                    case '+' when FOptions.HasFlag(SplitOptions.ConvertSpaces):
                        c = ' ';
                        break;
                }

                FResultBuffer[FPosition++] = c;
            }
        }

        public string Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(FResultBuffer, 0, FPosition);
            }
        }

        public void Reset() => FPosition = FIndex = 0;

        public IEnumerable<string> AsEnumerable()
        {
            Reset();

            while (MoveNext())
            {
                yield return Current;
            }
        }

        /// <summary>
        /// Splits the given path converting hex values if necessary.
        /// </summary>
        /// <remarks>Due to performance considerations, this method intentionally doesn't return an <see cref="IEnumerable{String}"/>.</remarks>
        public static PathSplitter Split(string path, SplitOptions options = SplitOptions.Default) => new(path, options);
    }
}
