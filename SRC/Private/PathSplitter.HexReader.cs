/********************************************************************************
* PathSplitter.HexReader.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    internal sealed partial class PathSplitter
    {
        private byte[]? FBytes;

        private char[]? FHexBuffer;

        private int FByteCount;

        private bool ReadHexChar()
        {
            int pos = FInputPosition;

            if (FInput.Length - pos <= 2 || FInput[pos] != '%')
                return false;

            FHexBuffer ??= new char[4];

            if ((FHexBuffer[0] = FInput[++pos]) == 'u')
            {
                //
                // %uXXXX
                //

                if (FInput.Length - pos <= 4)
                    return false;

                FHexBuffer[0] = FInput[++pos];
                FHexBuffer[1] = FInput[++pos];
                FHexBuffer[2] = FInput[++pos];
                FHexBuffer[3] = FInput[++pos];

                if
                (
                    !ushort.TryParse
                    (
#if !NETSTANDARD2_1_OR_GREATER
                        new string(FHexBuffer),
#else
                        FHexBuffer,
#endif
                        NumberStyles.HexNumber,
                        null,
                        out ushort chr
                    )
                )
                    return false;

                FlushHexChars();

                //
                // No Encoding.GetChars() call required
                //

                FOutput[FOutputPosition++] = (char) chr;
            } else {
                //
                // %XX
                //

                FHexBuffer[1] = FInput[++pos];

                FBytes ??= new byte[FInput.Length / 3]; // 3 == "%XX".Length 

                if 
                (
                    !byte.TryParse
                    (
#if !NETSTANDARD2_1_OR_GREATER
                        new string(FHexBuffer),
#else
                        FHexBuffer,
#endif
                        NumberStyles.HexNumber,
                        null,
                        out FBytes[FByteCount]  // do not increment FByteCount till it is sure that everything was allright
                    )
                )
                    return false;

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
