using System;
using BenchmarkDotNet.Running;
using Benchmarks;

Console.WriteLine("BitMask256 Benchmarks");
Console.WriteLine("=====================");
Console.WriteLine();
Console.WriteLine("1. Storage comparison: 4 ulongs vs Vector256<ulong> vs Static methods");
Console.WriteLine("2. AVX2 vs Scalar comparison");
Console.WriteLine("3. Single operation overhead");
Console.WriteLine();

// Run all benchmarks
var storageBenchmark = BenchmarkRunner.Run<BenchmarkBitMask256_Storage>();
var avx2VsScalar = BenchmarkRunner.Run<BenchmarkBitMask256_Avx2VsScalar>();
var singleOps = BenchmarkRunner.Run<BenchmarkBitMask256_SingleOps>();
