// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public abstract class BufferSource
    {
        public abstract BufferHandle Pin(int index = 0);

        public abstract BufferHandle RetainHandle();

        protected internal abstract void ReleaseHandle();
    }

    public abstract class BufferSource<T> : BufferSource
    {
        protected BufferSource() { }

        public abstract int Length { get; }

        public abstract Span<T> AsSpan(int index, int length);

        public virtual Span<T> AsSpan() => AsSpan(0, Length);

        public Buffer<T> Buffer => new Buffer<T>(this, 0, Length);

        public ReadOnlyBuffer<T> ReadOnlyBuffer => new ReadOnlyBuffer<T>(this, 0, Length);

        internal protected abstract bool TryGetArray(out ArraySegment<T> buffer);

        protected static unsafe void* Add(void* pointer, int offset)
        {
            return (byte*)pointer + ((ulong)Unsafe.SizeOf<T>() * (ulong)offset);
        }
    }
}