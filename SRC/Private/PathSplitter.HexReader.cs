/********************************************************************************
* PathSplitter.HexReader.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    internal sealed partial class PathSplitter
    {
        private byte[]? FBytes;

        private int FByteCount;

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
#if NETSTANDARD2_1_OR_GREATER
                        FInput.AsSpan(pos, 4),
#else
                        FInput.Substring(pos, 4),
#endif
                        NumberStyles.HexNumber,
                        null,
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
            } else {
                //
                // %XX
                //

                FBytes ??= MemoryPool<byte>.Get(FInput.Length / 3); // 3 == "%XX".Length 

                if 
                (
                    !byte.TryParse
                    (
#if NETSTANDARD2_1_OR_GREATER
                        FInput.AsSpan(pos, 2),
#else
                        FInput.Substring(pos, 2),
#endif
                        NumberStyles.HexNumber,
                        null,
                        out FBytes[FByteCount]  // do not increment FByteCount till it is sure that everything was allright
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
    }
}
