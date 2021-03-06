// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// A collection of convenient span helpers, exposed as extension methods.
    /// </summary>
    public static partial class SpanExtensionsLabs
    {
        public static bool StartsWith(this ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> slice)
        {
            if (slice.Length > bytes.Length)
            {
                return false;
            }

            for (int i = 0; i < slice.Length; i++)
            {
                if (bytes[i] != slice[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool StartsWith<T>(this ReadOnlySpan<T> items, ReadOnlySpan<T> slice)
            where T : struct, IEquatable<T>
        {
            if (slice.Length > items.Length) return false;
            return items.Slice(0, slice.Length).SequenceEqual(slice);
        }

        public unsafe static int IndexOfVectorized(this Span<byte> buffer, byte value)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value);
            }
        }

        public unsafe static int IndexOfVectorized(this Span<byte> buffer, byte value0, byte value1)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value0, value1);
            }
        }

        public unsafe static int IndexOfVectorized(this Span<byte> buffer, byte value0, byte value1, byte value2)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value0, value1, value2);
            }
        }

        public unsafe static int IndexOfVectorized(this ReadOnlySpan<byte> buffer, byte value)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value);
            }
        }

        public unsafe static int IndexOfVectorized(this ReadOnlySpan<byte> buffer, byte value0, byte value1)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value0, value1);
            }
        }

        public unsafe static int IndexOfVectorized(this ReadOnlySpan<byte> buffer, byte value0, byte value1, byte value2)
        {
            fixed (byte* pSearchSpace = &buffer.DangerousGetPinnableReference())
            {
                return IndexOfVectorized(pSearchSpace, buffer.Length, value0, value1, value2);
            }
        }

        private static unsafe int IndexOfVectorized(byte* searchSpace, int length, byte value)
        {
            var offset = 0;
            // If length < vector length the jump over Vector dominates the search; as the Vector section is quite chunky
            // So do an early search and exit
            if (length < Vector<byte>.Count)
            {
                for (; offset < length; offset++)
                {
                    var ch = searchSpace[offset];
                    if (ch == value)
                    {
                        // goto rather than inline return to keep loop body small
                        goto shortExit;
                    }
                }
                // Not found
                offset = -1;
                shortExit:
                return offset;
            }

            if (Vector.IsHardwareAccelerated)
            {
                // Check Vector lengths
                if (length - Vector<byte>.Count >= offset)
                {
                    Vector<byte> values = GetVector(value);
                    do
                    {
                        var vMatches = Vector.Equals(Unsafe.Read<Vector<byte>>(searchSpace + offset), values);
                        if (!vMatches.Equals(Vector<byte>.Zero))
                        {
                            // Found match, reuse Vector values to keep register pressure low
                            values = vMatches;
                            break;
                        }

                        offset += Vector<byte>.Count;
                    } while (length - Vector<byte>.Count >= offset);

                    // Found match? Perform secondary search outside out of loop, so above loop body is small
                    if (length - Vector<byte>.Count >= offset)
                    {
                        // Find offset of first match
                        offset += LocateFirstFoundByte(values);
                        // goto rather than inline return to keep function smaller
                        goto exit;
                    }
                }
            }

            // Haven't found match, scan through remaining
            for (; offset < length; offset++)
            {
                var ch = searchSpace[offset];
                if (ch == value)
                {
                    // goto rather than inline return to keep loop body small
                    goto exit;
                }
            }

            // No Matches
            offset = -1;
            exit:
            return offset;
        }

        private static unsafe int IndexOfVectorized(byte* searchSpace, int length, byte value0, byte value1)
        {
            var offset = 0;
            // If length < vector length the jump over Vector dominates the search; as the Vector section is quite chunky
            // So do an early search and exit
            if (length < Vector<byte>.Count)
            {
                for (; offset < length; offset++)
                {
                    var ch = searchSpace[offset];
                    if (ch == value0 || ch == value1)
                    {
                        // goto rather than inline return to keep loop body small
                        goto shortExit;
                    }
                }
                // Not found
                offset = -1;
                shortExit:
                return offset;
            }

            if (Vector.IsHardwareAccelerated)
            {
                // Check Vector lengths
                if (length - Vector<byte>.Count >= offset)
                {
                    Vector<byte> values0 = GetVector(value0);
                    Vector<byte> values1 = GetVector(value1);
                    do
                    {
                        var vData = Unsafe.Read<Vector<byte>>(searchSpace + offset);
                        var vMatches = Vector.BitwiseOr(
                                            Vector.Equals(vData, values0),
                                            Vector.Equals(vData, values1));
                        if (!vMatches.Equals(Vector<byte>.Zero))
                        {
                            // Found match, reuse Vector values0 to keep register pressure low
                            values0 = vMatches;
                            break;
                        }

                        offset += Vector<byte>.Count;
                    } while (length - Vector<byte>.Count >= offset);

                    // Found match? Perform secondary search outside out of loop, so above loop body is small
                    if (length - Vector<byte>.Count >= offset)
                    {
                        // Find offset of first match
                        offset += LocateFirstFoundByte(values0);
                        // goto rather than inline return to keep function smaller
                        goto exit;
                    }
                }
            }

            // Haven't found match, scan through remaining
            for (; offset < length; offset++)
            {
                var ch = searchSpace[offset];
                if (ch == value0 || ch == value1)
                {
                    // goto rather than inline return to keep loop body small
                    goto exit;
                }
            }

            // No Matches
            offset = -1;
            exit:
            return offset;
        }

        private static unsafe int IndexOfVectorized(byte* searchSpace, int length, byte value0, byte value1, byte value2)
        {
            var offset = 0;
            // If length < vector length the jump over Vector dominates the search; as the Vector section is quite chunky
            // So do an early search and exit
            if (length < Vector<byte>.Count)
            {
                for (; offset < length; offset++)
                {
                    var ch = searchSpace[offset];
                    if (ch == value0 || ch == value1 || ch == value2)
                    {
                        // goto rather than inline return to keep loop body small
                        goto shortExit;
                    }
                }
                // Not found
                offset = -1;
                shortExit:
                return offset;
            }

            if (Vector.IsHardwareAccelerated)
            {
                // Check Vector lengths
                if (length - Vector<byte>.Count >= offset)
                {
                    Vector<byte> values0 = GetVector(value0);
                    Vector<byte> values1 = GetVector(value1);
                    Vector<byte> values2 = GetVector(value2);
                    do
                    {
                        var vData = Unsafe.Read<Vector<byte>>(searchSpace + offset);
                        var vMatches = Vector.BitwiseOr(
                                        Vector.BitwiseOr(
                                            Vector.Equals(vData, values0),
                                            Vector.Equals(vData, values1)),
                                            Vector.Equals(vData, values2));
                        if (!vMatches.Equals(Vector<byte>.Zero))
                        {
                            // Found match, reuse Vector values0 to keep register pressure low
                            values0 = vMatches;
                            break;
                        }

                        offset += Vector<byte>.Count;
                    } while (length - Vector<byte>.Count >= offset);

                    // Found match? Perform secondary search outside out of loop, so above loop body is small
                    if (length - Vector<byte>.Count >= offset)
                    {
                        // Find offset of first match
                        offset += LocateFirstFoundByte(values0);
                        // goto rather than inline return to keep function smaller
                        goto exit;
                    }
                }
            }

            // Haven't found match, scan through remaining
            for (; offset < length; offset++)
            {
                var ch = searchSpace[offset];
                if (ch == value0 || ch == value1 || ch == value2)
                {
                    // goto rather than inline return to keep loop body small
                    goto exit;
                }
            }

            // No Matches
            offset = -1;
            exit:
            return offset;
        }

        public static bool TryIndicesOf(this Span<byte> buffer, byte value, Span<int> indices, out int numberOfIndices)
        {
            var length = buffer.Length;
            if (length == 0 || indices.Length == 0)
            {
                numberOfIndices = 0;
                return false;
            }

            return TryIndicesOf(ref buffer.DangerousGetPinnableReference(), value, length, indices, out numberOfIndices);
        }

        public static bool TryIndicesOf(this ReadOnlySpan<byte> buffer, byte value, Span<int> indices, out int numberOfIndices)
        {
            var length = buffer.Length;
            if (length == 0 || indices.Length == 0)
            {
                numberOfIndices = 0;
                return false;
            }

            return TryIndicesOf(ref buffer.DangerousGetPinnableReference(), value, length, indices, out numberOfIndices);
        }

        private unsafe static bool TryIndicesOf(ref byte searchSpace, byte value, int length, Span<int> indices, out int numberOfIndices)
        {
            var result = false;
            numberOfIndices = 0;

            fixed (byte* pSearchSpace = &searchSpace)
            {
                var searchStart = pSearchSpace;
                var offset = 0;

                while (true)
                {
                    if (Vector.IsHardwareAccelerated)
                    {
                        // Check Vector lengths
                        if (length - Vector<byte>.Count >= offset)
                        {
                            Vector<byte> values = GetVector(value);
                            do
                            {
                                var vFlaggedMatches = Vector.Equals(Unsafe.Read<Vector<byte>>(searchStart + offset), values);
                                if (!vFlaggedMatches.Equals(Vector<byte>.Zero))
                                {
                                    // Found match, reuse Vector values to keep register pressure low
                                    values = vFlaggedMatches;
                                    break;
                                }

                                offset += Vector<byte>.Count;
                            } while (length - Vector<byte>.Count >= offset);

                            // Found match? Perform secondary search outside out of loop, so above loop body is small
                            if (length - Vector<byte>.Count >= offset)
                            {
                                // Find offset of first match
                                offset += LocateFirstFoundByte(values);
                                // goto rather than inline return to keep function smaller
                                goto exitFixed;
                            }
                        }
                    }

                    ulong flaggedMatches = 0;
                    // Check ulong length
                    while (length - sizeof(ulong) >= offset)
                    {
                        flaggedMatches = SetLowBitsForByteMatch(*(ulong*)(searchStart + offset), value);
                        if (flaggedMatches != 0)
                        {
                            // Found match
                            break;
                        }

                        offset += sizeof(ulong);
                    }

                    // Found match? Perform secondary search outside out of loop, so above loop body is small
                    if (length - sizeof(ulong) >= offset)
                    {
                        // Find offset of first match
                        offset += LocateFirstFoundByte(flaggedMatches);
                        // goto rather than inline return to keep function smaller
                        goto exitFixed;
                    }

                    // Haven't found match, scan through remaining
                    for (; offset < length; offset++)
                    {
                        if (*(searchStart + offset) == value)
                        {
                            // goto rather than inline return to keep loop body small
                            goto exitFixed;
                        }
                    }

                    // No Matches
                    result = true;
                    break;

                    exitFixed:;
                    indices[numberOfIndices++] = offset++;
                    if (numberOfIndices >= indices.Length)
                        break;
                }
            }

            return result && numberOfIndices < indices.Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(Vector<byte> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            var i = 0;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i < Vector<ulong>.Count; i++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 8 + LocateFirstFoundByte(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(ulong match)
        {
            unchecked
            {
                // Flag least significant power of two bit
                var powerOfTwoFlag = match ^ (match - 1);
                // Shift all powers of two into the high byte and extract
                return (int)((powerOfTwoFlag * xorPowerOfTwoToHighByte) >> 57);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SetLowBitsForByteMatch(ulong potentialMatch, byte search)
        {
            unchecked
            {
                var flaggedValue = potentialMatch ^ (byteBroadcastToUlong * search);
                return (
                        (flaggedValue - byteBroadcastToUlong) &
                        ~(flaggedValue) &
                        filterByteHighBitsInUlong
                       ) >> 7;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<byte> GetVector(byte vectorByte)
        {
#if !NETCOREAPP
            // Vector<byte> .ctor doesn't become an intrinsic due to detection issue
            // However this does cause it to become an intrinsic (with additional multiply and reg->reg copy)
            // https://github.com/dotnet/coreclr/issues/7459#issuecomment-253965670
            return Vector.AsVectorByte(new Vector<uint>(vectorByte * 0x01010101u));
#else
            return new Vector<byte>(vectorByte);
#endif
        }

        private const ulong xorPowerOfTwoToHighByte = (0x07ul |
                                                       0x06ul << 8 |
                                                       0x05ul << 16 |
                                                       0x04ul << 24 |
                                                       0x03ul << 32 |
                                                       0x02ul << 40 |
                                                       0x01ul << 48) + 1;
        private const ulong byteBroadcastToUlong = ~0UL / byte.MaxValue;
        private const ulong filterByteHighBitsInUlong = (byteBroadcastToUlong >> 1) | (byteBroadcastToUlong << (sizeof(ulong) * 8 - 1));
    }
}

