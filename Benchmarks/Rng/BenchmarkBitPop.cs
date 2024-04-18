using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.Benchmarks.Rng;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkBitPop
{
    [Params(1, 10, 50, 200, 1000, 10_000)]
    public int Amount { get; set; }

    private Random _builtIn;

    [GlobalSetup]
    public void Setup()
    {
        _builtIn = new Random();
    }

    [Benchmark]
    public int LoopFlip()
    {
        var amount = Amount;
        var counter = 0;
        for (var i = 0; i < amount; i++)
        {
            if (_builtIn.Next(2) == 0)
            {
                counter++;
            }
        }

        return counter;
    }

    [Benchmark]
    public int LoopBitPop()
    {
        var amount = Amount;
        var heads = 0;
        while (amount > 0)
        {
            // Range is 2^amount exclusively, maximum of 62 bits can be used.
            var num = amount >= 62
                ? (ulong)_builtIn.NextInt64()
                : (ulong)_builtIn.NextInt64(1L << amount);

            heads += BitOperations.PopCount(num);
            
            // 64 bits minus sign bit and exclusive maximum leaves 62 bits
            amount -= 62;
        }

        return heads;
    }
}