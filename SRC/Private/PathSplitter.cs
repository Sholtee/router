/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using static Properties.Resources;

    internal ref struct PathSplitter
    {
        #region Private
        private readonly ReadOnlySpan<char> FInput;

        private int FInputPosition;

        private char[] FOutput;

        private int FOutputPosition;

        private byte[]? FBytes;

        private int FByteCount;

        private readonly SplitOptions FOptions;

        private PathSplitter(ReadOnlySpan<char> path, SplitOptions options)
        {
            FInput   = path;
            FOptions = options;
            FOutput  = MemoryPool<char>.Get(path.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InvalidOperationException InvalidPath(string err)
        {
            InvalidOperationException ex = new(string.Format(Culture, INVALID_PATH, err));
            ex.Data["Path"] = FInput.ToString();
            ex.Data["Position"] = FInputPosition;
            return ex;
        }

        private bool ReadHexChar()
        {
            int pos = FInputPosition;

            if (FInput.Length - pos <= 2 || FInput[pos++] != '%')
                return false;

            if (FInput[pos] == 'u')
            {
                //
                // %uXXXX
                //

                pos++;
                if (FInput.Length - pos < 4)
                    return false;

                if
                (
                    !ushort.TryParse
                    (
                        FInput.Slice(pos, 4)
#if !NETSTANDARD2_1_OR_GREATER
                        .ToString()
#endif
                        ,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out ushort chr
                    )
                )
                    return false;

                pos += 3;
                FlushHexChars();

                //
                // Already unicode so no Encoding.GetChars() call required
                //

                FOutput[FOutputPosition++] = (char)chr;
            }
            else
            {
                //
                // %XX
                //

                FBytes ??= MemoryPool<byte>.Get(FInput.Length / 3); // 3 == "%XX".Length 

                if
                (
                    !byte.TryParse
                    (
                        FInput.Slice(pos, 2)
#if !NETSTANDARD2_1_OR_GREATER
                        .ToString()
#endif
                        ,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out FBytes[FByteCount]  // do not increment FByteCount as long as it is sure that everything was allright
                    )
                )
                    return false;

                pos += 1;
                FByteCount++;
            }

            //
            // Only set globals if everything was all right.
            //

            FInputPosition = pos;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushHexChars()
        {
            if (FByteCount > 0)
            {
                Debug.Assert(FBytes is not null, "Buffer must have value assigned");

                FOutputPosition += FOptions.Encoding.GetChars
                (
                    FBytes,
                    0,
                    FByteCount,
                    FOutput,
                    FOutputPosition
                );
                FByteCount = 0;
            }
        }
        #endregion

        public bool MoveNext()
        {
            if (FInputPosition == FInput.Length)
                return false;

            FOutputPosition = 0;

            for (; ; FInputPosition++)
            {
                ReadOnlySpan<char> chunk = FInput.Slice(FInputPosition);

                int chrIndex = chunk.IndexOfAny('/', '%', '+');
                if (chrIndex is not 0 || chunk[chrIndex] is not '%')
                    FlushHexChars();

                if (chrIndex is -1)
                {
                    chunk.CopyTo(FOutput.AsSpan(FOutputPosition));

                    FOutputPosition += chunk.Length;
                    FInputPosition += chunk.Length;

                    return FOutputPosition > 0;
                }

                chunk.Slice(0, chrIndex).CopyTo(FOutput.AsSpan().Slice(FOutputPosition));

                FInputPosition += chrIndex;
                FOutputPosition += chrIndex;

                switch (chunk[chrIndex])
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

                        if (FOutputPosition is 0)
                            throw InvalidPath(EMPTY_CHUNK);

                        FInputPosition++;
                        return true;
                    case '%' when FOptions.ConvertHexValues:
                        if (!ReadHexChar())
                            throw InvalidPath(INVALID_HEX);

                        //
                        // FOutput remains untouched until the next FlushHexChars() call
                        //

                        break;
                    case '+' when FOptions.ConvertSpaces:
                        FOutput[FOutputPosition++] = ' ';
                        break;
                }
            }
        }

        public readonly ReadOnlySpan<char> Current
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
            MemoryPool<char>.Return(ref FOutput!);
            MemoryPool<byte>.Return(ref FBytes!);
        }

        /// <summary>
        /// Splits the given path converting hex values if necessary.
        /// </summary>
        /// <remarks>Due to performance considerations and since <see cref="ReadOnlySpan{T}"/> cannot be a generic parameter, this method intentionally doesn't return an <see cref="IEnumerable{T}"/>.</remarks>
        public static PathSplitter Split(ReadOnlySpan<char> path, SplitOptions? options = null) => new(path, options ?? SplitOptions.Default);
    }
}
