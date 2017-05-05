// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public abstract class OwnedBuffer<T> : BufferSource<T>, IDisposable, IRetainable
    {
        protected OwnedBuffer() { }

        public abstract bool IsDisposed { get; }

        public void Dispose()
        {
            if (IsRetained) throw new InvalidOperationException("outstanding references detected.");
            Dispose(true);
        }

        protected abstract void Dispose(bool disposing);

        public abstract bool IsRetained { get; }

        public abstract void Retain();

        public abstract void Release();

        // Default implementation so everything still works
        protected internal override void ReleaseHandle()
        {
            Release();
        }

        // Default implementation so everything still works
        public override BufferHandle RetainHandle()
        {
            Retain();
            return new BufferHandle(this);
        }
    }
}