using System;
using System.Collections;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BitArrayBenchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BitArrayDeserializeBenchmarks
{
    private byte[] _packedBits;
    private bool[] _packedBools;
    public const int BitLength = 8192;

    [GlobalSetup]
    public void Setup()
    {
        _packedBits = new byte[(BitLength + 7) / 8];
        _packedBools = new bool[BitLength];

        var rand = new Random(42);
        rand.NextBytes(_packedBits);

        for (var i = 0; i < _packedBools.Length; i++)
        {
            var byteIndex = Math.DivRem(i, 8, out var bitIndex);
            var bit = (_packedBits[byteIndex] & (1 << bitIndex)) != 0;
            _packedBools[i] = bit;
        }
    }

    [Benchmark(Baseline = true)]
    public BitArray Deserialize_ManualSet()
    {
        var bitArray = new BitArray(BitLength);

        for (var i = 0; i < BitLength; i++)
        {
            var byteIndex = Math.DivRem(i, 8, out var bitIndex);
            var bit = (_packedBits[byteIndex] & (1 << bitIndex)) != 0;
            bitArray[i] = bit;
        }

        return bitArray;
    }

    [Benchmark]
    public BitArray Deserialize_ThreadStatic()
    {
        return BitArrayDeserializer.DeserializeFromSpan(_packedBits);
    }

    [Benchmark]
    public BitArray Deserialize_Bools()
    {
        return BitArrayDeserializer.DeserializeFromBools(_packedBools);
    }
}

public static class BitArrayDeserializer
{
    // [ThreadStatic]
    private static byte[] _tempBuffer = new byte[(BitArrayDeserializeBenchmarks.BitLength + 7) / 8];

    public static BitArray DeserializeFromSpan(ReadOnlySpan<byte> data)
    {
        const int byteLength = (BitArrayDeserializeBenchmarks.BitLength + 7) / 8;

        if (_tempBuffer == null || _tempBuffer.Length < byteLength)
        {
            _tempBuffer = new byte[byteLength];
        }

        data[..byteLength].CopyTo(_tempBuffer);

        var bitArray = new BitArray(_tempBuffer)
        {
            Length = BitArrayDeserializeBenchmarks.BitLength
        };

        return bitArray;
    }

    public static BitArray DeserializeFromBools(bool[] packedBools)
    {
        return new BitArray(packedBools);
    }
}