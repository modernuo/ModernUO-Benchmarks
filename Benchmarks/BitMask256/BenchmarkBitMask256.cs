using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

/// <summary>
/// Benchmarks comparing different BitMask256 storage strategies.
/// Tests: 4 ulongs vs Vector256 storage vs static methods.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkBitMask256_Storage
{
    private int[] _setBitIndices;
    private (int start, int end)[] _clearRanges;

    [GlobalSetup]
    public void Setup()
    {
        // Simulate typical CanSpawnMobile workload:
        // - Many SetBit calls for surface candidates
        // - Several ClearRange calls for blockers
        var random = new Random(42);

        // ~20 surface candidates (SetBit operations)
        _setBitIndices = new int[20];
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            _setBitIndices[i] = random.Next(0, 256);
        }

        // ~10 blockers (ClearRange operations with typical 16-32 Z range)
        _clearRanges = new (int, int)[10];
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var start = random.Next(0, 200);
            var end = start + random.Next(16, 48);
            _clearRanges[i] = (start, Math.Min(255, end));
        }
    }

    #region SetBit benchmarks

    [Benchmark(Baseline = true)]
    public int SetBit_4Ulongs()
    {
        var mask = BitMask256_4Ulongs.AllClear();
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            mask.SetBit(idx);
        }

        return mask.PopCount();
    }

    [Benchmark]
    public int SetBit_Vector()
    {
        var mask = BitMask256_Vector.AllClear();
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            mask.SetBit(idx);
        }

        return mask.PopCount();
    }

    [Benchmark]
    public int SetBit_Static()
    {
        var mask = BitMask256_4Ulongs.AllClear();
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            BitMask256Ops.SetBit(ref mask, idx);
        }

        return mask.PopCount();
    }

    #endregion

    #region ClearRange benchmarks

    [Benchmark]
    public int ClearRange_4Ulongs()
    {
        var mask = BitMask256_4Ulongs.AllSet();
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            mask.ClearRange(start, end);
        }

        return mask.PopCount();
    }

    [Benchmark]
    public int ClearRange_Vector()
    {
        var mask = BitMask256_Vector.AllSet();
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            mask.ClearRange(start, end);
        }

        return mask.PopCount();
    }

    [Benchmark]
    public int ClearRange_Static()
    {
        var mask = BitMask256_4Ulongs.AllSet();
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            BitMask256Ops.ClearRange(ref mask, start, end);
        }

        return mask.PopCount();
    }

    #endregion

    #region Combined workload (simulates CanSpawnMobile)

    [Benchmark]
    public int Combined_4Ulongs()
    {
        var openSlots = BitMask256_4Ulongs.AllSet();
        var surfaces = BitMask256_4Ulongs.AllClear();

        // Set surface bits
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            surfaces.SetBit(idx);
        }

        // Clear blocker ranges
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            openSlots.ClearRange(start, end);
        }

        // AND and find lowest
        var result = surfaces.And(in openSlots);
        return result.LowestSetBit();
    }

    [Benchmark]
    public int Combined_Vector()
    {
        var openSlots = BitMask256_Vector.AllSet();
        var surfaces = BitMask256_Vector.AllClear();

        // Set surface bits
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            surfaces.SetBit(idx);
        }

        // Clear blocker ranges
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            openSlots.ClearRange(start, end);
        }

        // AND and find lowest
        var result = surfaces.And(in openSlots);
        return result.LowestSetBit();
    }

    [Benchmark]
    public int Combined_Static()
    {
        var openSlots = BitMask256_4Ulongs.AllSet();
        var surfaces = BitMask256_4Ulongs.AllClear();

        // Set surface bits
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            BitMask256Ops.SetBit(ref surfaces, idx);
        }

        // Clear blocker ranges
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            BitMask256Ops.ClearRange(ref openSlots, start, end);
        }

        // AND and find lowest
        var result = BitMask256Ops.And(in surfaces, in openSlots);
        return BitMask256Ops.LowestSetBit(in result);
    }

    #endregion
}

/// <summary>
/// Benchmarks comparing AVX2 vs Scalar implementations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkBitMask256_Avx2VsScalar
{
    private int[] _setBitIndices;
    private (int start, int end)[] _clearRanges;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        _setBitIndices = new int[20];
        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            _setBitIndices[i] = random.Next(0, 256);
        }

        _clearRanges = new (int, int)[10];
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var start = random.Next(0, 200);
            var end = start + random.Next(16, 48);
            _clearRanges[i] = (start, Math.Min(255, end));
        }
    }

    #region ClearRange: AVX2 vs Scalar

    [Benchmark(Baseline = true)]
    public int ClearRange_Avx2()
    {
        var mask = BitMask256_Avx2Only.AllSet();
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            mask.ClearRange(start, end);
        }

        return mask.LowestSetBit();
    }

    [Benchmark]
    public int ClearRange_Scalar()
    {
        var mask = BitMask256_ScalarOnly.AllSet();
        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            mask.ClearRange(start, end);
        }

        return mask.LowestSetBit();
    }

    #endregion

    #region And: AVX2 vs Scalar

    [Benchmark]
    public int And_Avx2()
    {
        var a = BitMask256_Avx2Only.AllSet();
        var b = BitMask256_Avx2Only.AllSet();

        // Clear some ranges to make masks different
        a.ClearRange(0, 50);
        b.ClearRange(200, 255);

        BitMask256_Avx2Only result = default;
        for (var i = 0; i < 100; i++)
        {
            result = a.And(in b);
        }
        return result.LowestSetBit();
    }

    [Benchmark]
    public int And_Scalar()
    {
        var a = BitMask256_ScalarOnly.AllSet();
        var b = BitMask256_ScalarOnly.AllSet();

        a.ClearRange(0, 50);
        b.ClearRange(200, 255);

        BitMask256_ScalarOnly result = default;
        for (var i = 0; i < 100; i++)
        {
            result = a.And(in b);
        }
        return result.LowestSetBit();
    }

    #endregion

    #region Combined workload: AVX2 vs Scalar

    [Benchmark]
    public int Combined_Avx2()
    {
        var openSlots = BitMask256_Avx2Only.AllSet();
        var surfaces = BitMask256_Avx2Only.AllClear();

        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            surfaces.SetBit(idx);
        }

        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            openSlots.ClearRange(start, end);
        }

        var result = surfaces.And(in openSlots);
        return result.LowestSetBit();
    }

    [Benchmark]
    public int Combined_Scalar()
    {
        var openSlots = BitMask256_ScalarOnly.AllSet();
        var surfaces = BitMask256_ScalarOnly.AllClear();

        for (var i = 0; i < _setBitIndices.Length; i++)
        {
            var idx = _setBitIndices[i];
            surfaces.SetBit(idx);
        }

        for (var i = 0; i < _clearRanges.Length; i++)
        {
            var (start, end) = _clearRanges[i];
            openSlots.ClearRange(start, end);
        }

        var result = surfaces.And(in openSlots);
        return result.LowestSetBit();
    }

    #endregion
}

/// <summary>
/// Benchmarks focusing on single-operation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkBitMask256_SingleOps
{
    [Benchmark(Baseline = true)]
    public int SingleSetBit_4Ulongs()
    {
        var mask = BitMask256_4Ulongs.AllClear();
        mask.SetBit(128);
        return mask.PopCount();
    }

    [Benchmark]
    public int SingleSetBit_Vector()
    {
        var mask = BitMask256_Vector.AllClear();
        mask.SetBit(128);
        return mask.PopCount();
    }

    [Benchmark]
    public int SingleClearRange_4Ulongs()
    {
        var mask = BitMask256_4Ulongs.AllSet();
        mask.ClearRange(64, 192);
        return mask.PopCount();
    }

    [Benchmark]
    public int SingleClearRange_Vector()
    {
        var mask = BitMask256_Vector.AllSet();
        mask.ClearRange(64, 192);
        return mask.PopCount();
    }

    [Benchmark]
    public int SingleAnd_4Ulongs()
    {
        var a = BitMask256_4Ulongs.AllSet();
        var b = BitMask256_4Ulongs.AllSet();
        a.ClearRange(0, 100);
        b.ClearRange(150, 255);
        var result = a.And(in b);
        return result.LowestSetBit();
    }

    [Benchmark]
    public int SingleAnd_Vector()
    {
        var a = BitMask256_Vector.AllSet();
        var b = BitMask256_Vector.AllSet();
        a.ClearRange(0, 100);
        b.ClearRange(150, 255);
        var result = a.And(in b);
        return result.LowestSetBit();
    }
}
