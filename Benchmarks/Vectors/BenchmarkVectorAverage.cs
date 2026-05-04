using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkVectorAverage
{
    [Params(8, 256, 8192, 262144)]
    public int Size { get; set; }

    private long[] SampleData;

    [GlobalSetup]
    public void Setup()
    {
        SampleData = Enumerable.Range(0, Size)
            .Select(i => (long)i)
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public double VectorizedAverage()
    {
        return VectorizedAverage(SampleData);
    }

    [Benchmark]
    public double StandardAverage()
    {
        return StandardAverage(SampleData);
    }

    [Benchmark]
    public double LINQAverage()
    {
        return LINQAverage(SampleData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double StandardAverage(ReadOnlySpan<long> span)
    {
        long sum = 0;
        for (var i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        return (double)sum / span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LINQAverage(long[] data)
    {
        return data.Average();
    }

    public static double VectorizedAverage(ReadOnlySpan<long> span)
    {
        long sum = 0;
        var i = 0;

        // Check if hardware acceleration is available and the span is long enough for vector processing
        if (Vector.IsHardwareAccelerated && span.Length >= Vector<long>.Count)
        {
            // Reinterpret the ReadOnlySpan<long> as a ReadOnlySpan<Vector<long>>
            var vectorSpan = MemoryMarshal.Cast<long, Vector<long>>(span);
            var vectorSum = Vector<long>.Zero;

            // Process full vector chunks
            for (; i < vectorSpan.Length; i++)
            {
                vectorSum += vectorSpan[i];
            }

            // Sum the elements within the resulting vector
            for (var j = 0; j < Vector<long>.Count; j++)
            {
                sum += vectorSum[j];
            }

            // Advance the index in the original span to the point where vector processing stopped
            i = vectorSpan.Length * Vector<long>.Count;
        }

        // Process any remaining elements with a standard loop
        for (; i < span.Length; i++)
        {
            sum += span[i];
        }

        // Calculate the average (cast to double to ensure floating-point division)
        return (double)sum / span.Length;
    }
}