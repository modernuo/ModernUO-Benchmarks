using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks.Rng;

// var doublefixed = BenchmarkRunner.Run<BenchmarkDoubleVsFixed>();
// var xoshiro = BenchmarkRunner.Run<BenchmarkXoshiro>();
var bitpop = BenchmarkRunner.Run<BenchmarkBitPop>();