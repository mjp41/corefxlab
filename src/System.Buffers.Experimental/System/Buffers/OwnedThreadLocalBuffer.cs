// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Buffers
{
    public class OwnedThreadLocalBuffer<T> : ReferenceCountedBuffer<T>
    {
        private T[] _array;

        public OwnedThreadLocalBuffer(T[] array)
        {
            _array = array;
        }

        ~OwnedThreadLocalBuffer()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            _array = null;
            base.Dispose(disposing);
        }

        public override BufferHandle Pin(int index = 0)
        {
            ReferenceCounter.AddReference(this);
            unsafe
            {
                var handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                var pointer = Add((void*)handle.AddrOfPinnedObject(), index);
                return new BufferHandle(this, pointer, handle);
            }
        }

        public override BufferHandle GetHandle()
        {
            ReferenceCounter.AddReference(this);
            return new BufferHandle(this);
        }

        public override void ReleaseHandle()
        {
            ReferenceCounter.Release(this);
        }

        protected override bool TryGetArray(out ArraySegment<T> buffer)
        {
            buffer = new ArraySegment<T>(_array);
            return true;
        }

        public override int Length => _array.Length;
        
        public unsafe override Span<T> AsSpan(int index, int length)
        {
            if (IsDisposed) BuffersExperimentalThrowHelper.ThrowObjectDisposedException(nameof(OwnedNativeBuffer));
            return new Span<T>(_array).Slice(index, length);
        }

        public override bool IsRetained => base.IsRetained || ReferenceCounter.HasReference(this);
    }
}
