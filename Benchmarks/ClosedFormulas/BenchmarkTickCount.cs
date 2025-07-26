using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ClosedFormulas;

[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkTickCount
{
    public static readonly Random _random = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestampOld(long rawTicks)
    {
        return rawTicks / (1_000_000L / 1000L);
    }

    [Benchmark]
    public void OldTimeStamp()
    {
        var rnd = 101;
        var rawTick = unchecked(long.MaxValue + 100000 + rnd);
        var rawLastTick = long.MaxValue - (100000 + rnd);

        var tickCountOld = GetTimestampOld(rawTick);
        var lastTickTurnedOld = GetTimestampOld(rawLastTick);
    }

    private static readonly bool UseFastMath = Stopwatch.Frequency % 1000 == 0;
    private static readonly ulong Divisor = (ulong)Stopwatch.Frequency / 1000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestampNew(long rawTicks)
    {
        if (UseFastMath)
            return (long)((ulong)rawTicks / Divisor);

        // fallback to slower but accurate calculation
        return (long)((UInt128)rawTicks * 1000 / (ulong)Stopwatch.Frequency);
    }

    [Benchmark]
    public void OldTimeStampNew()
    {
        var rnd = 101;
        var rawTick = unchecked(long.MaxValue + 100000 + rnd);
        var rawLastTick = long.MaxValue - (100000 + rnd);

        var tickCountOld = GetTimestampNew(rawTick);
        var lastTickTurnedOld = GetTimestampNew(rawLastTick);
    }
}