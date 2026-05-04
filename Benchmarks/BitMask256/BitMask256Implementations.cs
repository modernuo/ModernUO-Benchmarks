using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmarks;

#region Option 1: 4 ulongs (current approach)

/// <summary>
/// BitMask256 using 4 separate ulong fields.
/// Converts to/from Vector256 for bulk operations.
/// </summary>
public struct BitMask256_4Ulongs
{
    public ulong Bits0, Bits1, Bits2, Bits3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_4Ulongs AllSet() => new()
    {
        Bits0 = ulong.MaxValue,
        Bits1 = ulong.MaxValue,
        Bits2 = ulong.MaxValue,
        Bits3 = ulong.MaxValue
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_4Ulongs AllClear() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        switch (segment)
        {
            case 0: Bits0 |= mask; break;
            case 1: Bits1 |= mask; break;
            case 2: Bits2 |= mask; break;
            case 3: Bits3 |= mask; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool GetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        return segment switch
        {
            0 => (Bits0 & mask) != 0,
            1 => (Bits1 & mask) != 0,
            2 => (Bits2 & mask) != 0,
            3 => (Bits3 & mask) != 0,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end)
    {
        if (Avx2.IsSupported)
        {
            var mask = Vector256.Create(
                CreateSegmentMask(start, end, 0),
                CreateSegmentMask(start, end, 64),
                CreateSegmentMask(start, end, 128),
                CreateSegmentMask(start, end, 192)
            );

            var current = Vector256.Create(Bits0, Bits1, Bits2, Bits3);
            var result = Avx2.AndNot(mask, current);

            Bits0 = result.GetElement(0);
            Bits1 = result.GetElement(1);
            Bits2 = result.GetElement(2);
            Bits3 = result.GetElement(3);
        }
        else
        {
            Bits0 &= ~CreateSegmentMask(start, end, 0);
            Bits1 &= ~CreateSegmentMask(start, end, 64);
            Bits2 &= ~CreateSegmentMask(start, end, 128);
            Bits3 &= ~CreateSegmentMask(start, end, 192);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
            return 0;

        var bitCount = localEnd - localStart + 1;
        var mask = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return mask << localStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256_4Ulongs And(in BitMask256_4Ulongs other)
    {
        if (Avx2.IsSupported)
        {
            var a = Vector256.Create(Bits0, Bits1, Bits2, Bits3);
            var b = Vector256.Create(other.Bits0, other.Bits1, other.Bits2, other.Bits3);
            var result = Avx2.And(a, b);

            return new BitMask256_4Ulongs
            {
                Bits0 = result.GetElement(0),
                Bits1 = result.GetElement(1),
                Bits2 = result.GetElement(2),
                Bits3 = result.GetElement(3)
            };
        }

        return new BitMask256_4Ulongs
        {
            Bits0 = Bits0 & other.Bits0,
            Bits1 = Bits1 & other.Bits1,
            Bits2 = Bits2 & other.Bits2,
            Bits3 = Bits3 & other.Bits3
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int PopCount() =>
        BitOperations.PopCount(Bits0) +
        BitOperations.PopCount(Bits1) +
        BitOperations.PopCount(Bits2) +
        BitOperations.PopCount(Bits3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit()
    {
        if (Bits0 != 0) return BitOperations.TrailingZeroCount(Bits0);
        if (Bits1 != 0) return 64 + BitOperations.TrailingZeroCount(Bits1);
        if (Bits2 != 0) return 128 + BitOperations.TrailingZeroCount(Bits2);
        if (Bits3 != 0) return 192 + BitOperations.TrailingZeroCount(Bits3);
        return -1;
    }
}

#endregion

#region Option 2: Vector256<ulong> storage with Unsafe access

/// <summary>
/// BitMask256 storing Vector256 directly.
/// Uses Unsafe reinterpretation for single-bit operations.
/// </summary>
public struct BitMask256_Vector
{
    private Vector256<ulong> _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_Vector AllSet() => new()
    {
        _value = Vector256.Create(ulong.MaxValue)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_Vector AllClear() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        // Use Unsafe to access individual ulongs
        ref var asUlong = ref Unsafe.As<Vector256<ulong>, ulong>(ref _value);
        Unsafe.Add(ref asUlong, segment) |= mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool GetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        ref var asUlong = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in _value));
        return (Unsafe.Add(ref asUlong, segment) & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end)
    {
        if (Avx2.IsSupported)
        {
            var mask = Vector256.Create(
                CreateSegmentMask(start, end, 0),
                CreateSegmentMask(start, end, 64),
                CreateSegmentMask(start, end, 128),
                CreateSegmentMask(start, end, 192)
            );

            _value = Avx2.AndNot(mask, _value);
        }
        else
        {
            ref var asUlong = ref Unsafe.As<Vector256<ulong>, ulong>(ref _value);
            Unsafe.Add(ref asUlong, 0) &= ~CreateSegmentMask(start, end, 0);
            Unsafe.Add(ref asUlong, 1) &= ~CreateSegmentMask(start, end, 64);
            Unsafe.Add(ref asUlong, 2) &= ~CreateSegmentMask(start, end, 128);
            Unsafe.Add(ref asUlong, 3) &= ~CreateSegmentMask(start, end, 192);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
            return 0;

        var bitCount = localEnd - localStart + 1;
        var mask = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return mask << localStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256_Vector And(in BitMask256_Vector other)
    {
        if (Avx2.IsSupported)
        {
            return new BitMask256_Vector { _value = Avx2.And(_value, other._value) };
        }

        ref var a = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in _value));
        ref var b = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in other._value));

        var result = new BitMask256_Vector();
        ref var r = ref Unsafe.As<Vector256<ulong>, ulong>(ref result._value);
        Unsafe.Add(ref r, 0) = Unsafe.Add(ref a, 0) & Unsafe.Add(ref b, 0);
        Unsafe.Add(ref r, 1) = Unsafe.Add(ref a, 1) & Unsafe.Add(ref b, 1);
        Unsafe.Add(ref r, 2) = Unsafe.Add(ref a, 2) & Unsafe.Add(ref b, 2);
        Unsafe.Add(ref r, 3) = Unsafe.Add(ref a, 3) & Unsafe.Add(ref b, 3);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int PopCount()
    {
        ref var asUlong = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in _value));
        return BitOperations.PopCount(Unsafe.Add(ref asUlong, 0)) +
               BitOperations.PopCount(Unsafe.Add(ref asUlong, 1)) +
               BitOperations.PopCount(Unsafe.Add(ref asUlong, 2)) +
               BitOperations.PopCount(Unsafe.Add(ref asUlong, 3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit()
    {
        ref var asUlong = ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in _value));
        var b0 = Unsafe.Add(ref asUlong, 0);
        if (b0 != 0) return BitOperations.TrailingZeroCount(b0);
        var b1 = Unsafe.Add(ref asUlong, 1);
        if (b1 != 0) return 64 + BitOperations.TrailingZeroCount(b1);
        var b2 = Unsafe.Add(ref asUlong, 2);
        if (b2 != 0) return 128 + BitOperations.TrailingZeroCount(b2);
        var b3 = Unsafe.Add(ref asUlong, 3);
        if (b3 != 0) return 192 + BitOperations.TrailingZeroCount(b3);
        return -1;
    }
}

#endregion

#region Option 3: Static methods with ref/in parameters

/// <summary>
/// Static operations on BitMask256_4Ulongs using ref/in parameters.
/// </summary>
public static class BitMask256Ops
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref BitMask256_4Ulongs mask, int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var m = 1UL << localBit;

        switch (segment)
        {
            case 0: mask.Bits0 |= m; break;
            case 1: mask.Bits1 |= m; break;
            case 2: mask.Bits2 |= m; break;
            case 3: mask.Bits3 |= m; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(in BitMask256_4Ulongs mask, int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var m = 1UL << localBit;

        return segment switch
        {
            0 => (mask.Bits0 & m) != 0,
            1 => (mask.Bits1 & m) != 0,
            2 => (mask.Bits2 & m) != 0,
            3 => (mask.Bits3 & m) != 0,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearRange(ref BitMask256_4Ulongs mask, int start, int end)
    {
        if (Avx2.IsSupported)
        {
            var clearMask = Vector256.Create(
                CreateSegmentMask(start, end, 0),
                CreateSegmentMask(start, end, 64),
                CreateSegmentMask(start, end, 128),
                CreateSegmentMask(start, end, 192)
            );

            var current = Vector256.Create(mask.Bits0, mask.Bits1, mask.Bits2, mask.Bits3);
            var result = Avx2.AndNot(clearMask, current);

            mask.Bits0 = result.GetElement(0);
            mask.Bits1 = result.GetElement(1);
            mask.Bits2 = result.GetElement(2);
            mask.Bits3 = result.GetElement(3);
        }
        else
        {
            mask.Bits0 &= ~CreateSegmentMask(start, end, 0);
            mask.Bits1 &= ~CreateSegmentMask(start, end, 64);
            mask.Bits2 &= ~CreateSegmentMask(start, end, 128);
            mask.Bits3 &= ~CreateSegmentMask(start, end, 192);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
            return 0;

        var bitCount = localEnd - localStart + 1;
        var m = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return m << localStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_4Ulongs And(in BitMask256_4Ulongs a, in BitMask256_4Ulongs b)
    {
        if (Avx2.IsSupported)
        {
            var va = Vector256.Create(a.Bits0, a.Bits1, a.Bits2, a.Bits3);
            var vb = Vector256.Create(b.Bits0, b.Bits1, b.Bits2, b.Bits3);
            var result = Avx2.And(va, vb);

            return new BitMask256_4Ulongs
            {
                Bits0 = result.GetElement(0),
                Bits1 = result.GetElement(1),
                Bits2 = result.GetElement(2),
                Bits3 = result.GetElement(3)
            };
        }

        return new BitMask256_4Ulongs
        {
            Bits0 = a.Bits0 & b.Bits0,
            Bits1 = a.Bits1 & b.Bits1,
            Bits2 = a.Bits2 & b.Bits2,
            Bits3 = a.Bits3 & b.Bits3
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LowestSetBit(in BitMask256_4Ulongs mask)
    {
        if (mask.Bits0 != 0) return BitOperations.TrailingZeroCount(mask.Bits0);
        if (mask.Bits1 != 0) return 64 + BitOperations.TrailingZeroCount(mask.Bits1);
        if (mask.Bits2 != 0) return 128 + BitOperations.TrailingZeroCount(mask.Bits2);
        if (mask.Bits3 != 0) return 192 + BitOperations.TrailingZeroCount(mask.Bits3);
        return -1;
    }
}

#endregion

#region AVX2 vs Scalar implementations

/// <summary>
/// BitMask256 that ALWAYS uses AVX2 (assumes hardware support).
/// No runtime checks.
/// </summary>
public struct BitMask256_Avx2Only
{
    public ulong Bits0, Bits1, Bits2, Bits3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_Avx2Only AllSet() => new()
    {
        Bits0 = ulong.MaxValue,
        Bits1 = ulong.MaxValue,
        Bits2 = ulong.MaxValue,
        Bits3 = ulong.MaxValue
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_Avx2Only AllClear() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        switch (segment)
        {
            case 0: Bits0 |= mask; break;
            case 1: Bits1 |= mask; break;
            case 2: Bits2 |= mask; break;
            case 3: Bits3 |= mask; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end)
    {
        // Always use AVX2, no check
        var mask = Vector256.Create(
            CreateSegmentMask(start, end, 0),
            CreateSegmentMask(start, end, 64),
            CreateSegmentMask(start, end, 128),
            CreateSegmentMask(start, end, 192)
        );

        var current = Vector256.Create(Bits0, Bits1, Bits2, Bits3);
        var result = Avx2.AndNot(mask, current);

        Bits0 = result.GetElement(0);
        Bits1 = result.GetElement(1);
        Bits2 = result.GetElement(2);
        Bits3 = result.GetElement(3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
            return 0;

        var bitCount = localEnd - localStart + 1;
        var mask = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return mask << localStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256_Avx2Only And(in BitMask256_Avx2Only other)
    {
        // Always use AVX2, no check
        var a = Vector256.Create(Bits0, Bits1, Bits2, Bits3);
        var b = Vector256.Create(other.Bits0, other.Bits1, other.Bits2, other.Bits3);
        var result = Avx2.And(a, b);

        return new BitMask256_Avx2Only
        {
            Bits0 = result.GetElement(0),
            Bits1 = result.GetElement(1),
            Bits2 = result.GetElement(2),
            Bits3 = result.GetElement(3)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit()
    {
        if (Bits0 != 0) return BitOperations.TrailingZeroCount(Bits0);
        if (Bits1 != 0) return 64 + BitOperations.TrailingZeroCount(Bits1);
        if (Bits2 != 0) return 128 + BitOperations.TrailingZeroCount(Bits2);
        if (Bits3 != 0) return 192 + BitOperations.TrailingZeroCount(Bits3);
        return -1;
    }
}

/// <summary>
/// BitMask256 that NEVER uses AVX2 (pure scalar).
/// For comparison to measure AVX2 benefit.
/// </summary>
public struct BitMask256_ScalarOnly
{
    public ulong Bits0, Bits1, Bits2, Bits3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_ScalarOnly AllSet() => new()
    {
        Bits0 = ulong.MaxValue,
        Bits1 = ulong.MaxValue,
        Bits2 = ulong.MaxValue,
        Bits3 = ulong.MaxValue
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256_ScalarOnly AllClear() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index)
    {
        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        switch (segment)
        {
            case 0: Bits0 |= mask; break;
            case 1: Bits1 |= mask; break;
            case 2: Bits2 |= mask; break;
            case 3: Bits3 |= mask; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end)
    {
        // Never use AVX2, always scalar
        Bits0 &= ~CreateSegmentMask(start, end, 0);
        Bits1 &= ~CreateSegmentMask(start, end, 64);
        Bits2 &= ~CreateSegmentMask(start, end, 128);
        Bits3 &= ~CreateSegmentMask(start, end, 192);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
            return 0;

        var bitCount = localEnd - localStart + 1;
        var mask = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return mask << localStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256_ScalarOnly And(in BitMask256_ScalarOnly other)
    {
        // Never use AVX2, always scalar
        return new BitMask256_ScalarOnly
        {
            Bits0 = Bits0 & other.Bits0,
            Bits1 = Bits1 & other.Bits1,
            Bits2 = Bits2 & other.Bits2,
            Bits3 = Bits3 & other.Bits3
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit()
    {
        if (Bits0 != 0) return BitOperations.TrailingZeroCount(Bits0);
        if (Bits1 != 0) return 64 + BitOperations.TrailingZeroCount(Bits1);
        if (Bits2 != 0) return 128 + BitOperations.TrailingZeroCount(Bits2);
        if (Bits3 != 0) return 192 + BitOperations.TrailingZeroCount(Bits3);
        return -1;
    }
}

#endregion
