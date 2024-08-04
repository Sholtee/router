/********************************************************************************
* StringBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    /// <summary>
    /// String builder that uses recycled memory
    /// </summary>
    internal sealed class StringBuilder(int initialSize = 128) : IDisposable
    {
        private static readonly ArrayPool<char> FPool = ArrayPool<char>.Shared;

        private char[] FBuffer = FPool.Rent(initialSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfRequired(int addition)
        {
            int newLength = Length + addition;
            if (newLength >= FBuffer.Length)
            {
                char[] newBuffer = FPool.Rent(newLength * 2);
                FBuffer.CopyTo(newBuffer, 0);

                FPool.Return(FBuffer);
                FBuffer = newBuffer;
            }
        }

        public void Dispose() => FPool.Return(FBuffer);

        public void Append(string str)
        {
            ResizeIfRequired(str.Length);
            str.CopyTo(0, FBuffer, Length, str.Length);
            Length += str.Length;
        }

        public void Append(char chr)
        {
            ResizeIfRequired(1);
            FBuffer[Length++] = chr;
        }

        public override string ToString() => new(FBuffer, 0, Length);

        public void Clear() => Length = 0;

        public int Length { get; private set; }
    }
}
