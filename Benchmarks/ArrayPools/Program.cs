using BenchmarkDotNet.Running;
using Benchmarks;

var stArrayPool = BenchmarkRunner.Run<BenchmarkSTArray>();
