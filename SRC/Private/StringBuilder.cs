/********************************************************************************
* StringBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Router.Internals
{
    /// <summary>
    /// String builder that uses recycled memory
    /// </summary>
    internal sealed class StringBuilder(int initialLength = 128) : IDisposable
    {
        private char[] FBuffer = MemoryPool<char>.Get(initialLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfRequired(int addition)
        {
            int newLength = Length + addition;
            if (newLength >= FBuffer.Length)
                Array.Resize(ref FBuffer, newLength * 2);
        }

        public void Dispose()
        {
            if (FBuffer is not null)
            {
                MemoryPool<char>.Return(FBuffer);
                FBuffer = null!;
            }
        }

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
