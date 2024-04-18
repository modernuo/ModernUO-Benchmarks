using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks.Rng;

var bitpop = BenchmarkRunner.Run<BenchmarkBitPop>();