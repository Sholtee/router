/********************************************************************************
* PathSplitter.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    using Primitives;

    using static Properties.Resources;

    /// <summary>
    /// Splits the given path converting hex values if necessary.
    /// </summary>
    /// <remarks>Due to performance considerations and since it is a ref struct, <see cref="PathSplitter"/> doesn't implement the <see cref="IEnumerable{T}"/> interface.</remarks>
    internal ref struct PathSplitter(ReadOnlySpan<char> path, SplitOptions? options = null)
    {
        #region Private
        private delegate int FindControlFn(ReadOnlySpan<char> input);

        private static int FindControl(ReadOnlySpan<char> input) => input.IndexOfAny
        (
            "/%+".AsSpan()
        );

        private static int FindControlSafe(ReadOnlySpan<char> input) => input.IndexOfAnyExcept
        (
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~".AsSpan()
        );

        private static readonly FindControlFn  // converting methods to delegates takes long so do it only once
            FFindControl = FindControl,
            FFindControlSafe = FindControlSafe;

        private int
            FInputPosition,
            FByteCount,
            FOutputPosition;

        private readonly char[] FOutput = ArrayPool<char>.Shared.Rent(path.Length);

        private readonly byte[] FBytes = ArrayPool<byte>.Shared.Rent(path.Length / 3); // 3 == "%XX".Length ;

        private readonly ReadOnlySpan<char> FPath = path;  // cannot capture "path" as it is a ref struct

        private readonly SplitOptions FOptions = options ?? SplitOptions.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void InvalidPath(string err)
        {
            InvalidOperationException ex = new(string.Format(Culture, INVALID_PATH, err));
            ex.Data["Path"] = FPath.ToString();
            ex.Data["Position"] = FInputPosition;
            throw ex;
        }

        private bool ReadHexChar()
        {
            int pos = FInputPosition;

            if (FPath.Length - pos <= 2 || FPath[pos++] != '%')
                return false;

            if (FPath[pos] == 'u')
            {
                //
                // %uXXXX
                //

                pos++;
                if (FPath.Length - pos < 4)
                    return false;

                if
                (
                    !ushort.TryParse
                    (
                        FPath.Slice(pos, 4)
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

                FOutput[FOutputPosition++] = (char) chr;
            }
            else
            {
                //
                // %XX
                //

                if
                (
                    !byte.TryParse
                    (
                        FPath.Slice(pos, 2)
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
            if (FInputPosition == FPath.Length)
                return false;

            FOutputPosition = 0;

            FindControlFn findControl = FOptions.AllowUnsafeChars ? FFindControl : FFindControlSafe;

            for (; ; FInputPosition++)
            {
                ReadOnlySpan<char> chunk = FPath.Slice(FInputPosition);

                int ctrlIndex = findControl(chunk);
                if (ctrlIndex is not 0 || chunk[0] is not '%')
                    FlushHexChars();

                if (ctrlIndex is -1)
                {
                    chunk.CopyTo(FOutput.AsSpan(FOutputPosition));

                    FOutputPosition += chunk.Length;
                    FInputPosition += chunk.Length;

                    return FOutputPosition > 0;
                }

                chunk.Slice(0, ctrlIndex).CopyTo(FOutput.AsSpan().Slice(FOutputPosition));

                FInputPosition += ctrlIndex;
                FOutputPosition += ctrlIndex;

                switch (chunk[ctrlIndex])
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
                            InvalidPath(EMPTY_CHUNK);

                        FInputPosition++;
                        return true;
                    case '%' when FOptions.ConvertHexValues:
                        if (!ReadHexChar())
                            InvalidPath(INVALID_HEX);

                        //
                        // FOutput remains untouched until the next FlushHexChars() call
                        //

                        break;
                    case '+' when FOptions.ConvertSpaces:
                        FOutput[FOutputPosition++] = ' ';
                        break;
                    default:
                        if (!FOptions.AllowUnsafeChars)
                            InvalidPath(UNSAFE_CHAR);
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

        public readonly void Dispose()
        {
            ArrayPool<char>.Shared.Return(FOutput);
            ArrayPool<byte>.Shared.Return(FBytes);
        }
    }
}
