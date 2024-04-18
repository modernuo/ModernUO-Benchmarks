using System;
using System.Buffers.Binary;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkVectors
{
    private static readonly byte[] PrimeArray = {
        0x61, 0x8a, 0x5b, 0xfb, 0x31, 0x11, 0x45, 0xe8,
        0x9a, 0x97, 0x5d, 0x31, 0xe9, 0x39, 0x9f, 0x58
    };
    
    private byte[] _data = {
        0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF,
        0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF
    };
    
    private static ulong _vector1 = BinaryPrimitives.ReadUInt64BigEndian(PrimeArray);
    private static ulong _vector2 = BinaryPrimitives.ReadUInt64BigEndian(PrimeArray.AsSpan(8));

    private static readonly Vector256<ulong> PrimeVector = Vector256.Create(_vector1, _vector2, _vector1, _vector2);
    
    [Benchmark]
    public void LegacyXor()
    {
        var data = new Span<byte>(_data);
        var a = 0;
        for (var i = 0; i < data.Length; i++, a++)
        {
            if (a >= PrimeArray.Length)
            {
                a = 0;
            }
            
            data[i] ^= PrimeArray[a];
        }
    }

    [Benchmark]
    public unsafe void VectorXor()
    {
        Span<byte> data = stackalloc byte[]
        {
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF,
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF
        };
        
        fixed (byte* pData = data)
        {
            var vector = Avx.LoadVector256((ulong*)pData);
            var res = Avx2.Xor(vector, PrimeVector);
            Avx.Store((ulong*)pData, res);
        }
    }
}
