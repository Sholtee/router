/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal sealed partial class PathSplitter: IDisposable
    {
        private readonly string FInput;

        private int FInputPosition;

        private char[] FOutput;

        private int FOutputPosition;

        private readonly SplitOptions FOptions;

        private PathSplitter(string path, SplitOptions options)
        {
            FInput   = path;
            FOptions = options;
            FOutput  = MemoryPool<char>.Get(path.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InvalidOperationException InvalidPath(string err)
        {
            InvalidOperationException ex = new(string.Format(Culture, INVALID_PATH, err));
            ex.Data["Path"] = FInput;
            ex.Data["Position"] = FInputPosition;
            return ex;
        }

        public bool MoveNext()
        {
            FOutputPosition = 0;

            for (; ; FInputPosition++)
            {
                if (FInputPosition == FInput.Length)
                {
                    FlushHexChars();
                    return FOutputPosition > 0;
                }

                char c = FInput[FInputPosition];
                switch (c)
                {
                    case '/':
                        //
                        // Skip leading separator
                        //

                        if (FInputPosition is 0)
                            continue;

                        //
                        // Ensure the chunk is not empty
                        //

                        FlushHexChars();
                        if (FOutputPosition is 0)
                            throw InvalidPath(EMPTY_CHUNK);

                        FInputPosition++;
                        return true;
                    case '%' when FOptions.ConvertHexValues:
                        if (!ReadHexChar())
                            throw InvalidPath(INVALID_HEX);

                        //
                        // FResultBuffer remains untouched until the next FHexReader.Flush() call
                        //

                        continue;
                    case '+' when FOptions.ConvertSpaces:
                        c = ' ';
                        break;
                }

                FlushHexChars();
                FOutput[FOutputPosition++] = c;
            }
        }

        public ReadOnlySpan<char> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return FOutput.AsSpan(0, FOutputPosition);
            }
        }

        public void Reset() => FInputPosition = FOutputPosition = 0;

        public void Dispose()
        {
            Return(ref FOutput!);
            Return(ref FBytes);

            static void Return<T>(ref T[]? buffer)
            {
                if (buffer is not null)
                {
                    MemoryPool<T>.Return(buffer);
                    buffer = null;
                }
            }
        }

        /// <summary>
        /// Splits the given path converting hex values if necessary.
        /// </summary>
        /// <remarks>Due to performance considerations and since <see cref="ReadOnlySpan{T}"/> cannot be a generic parameter, this method intentionally doesn't return an <see cref="IEnumerable{T}"/>.</remarks>
        public static PathSplitter Split(string path, SplitOptions? options = null) => new(path, options ?? SplitOptions.Default);
    }
}
