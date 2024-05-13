using BenchmarkDotNet.Running;
using Benchmarks;

var benchmark = BenchmarkRunner.Run<BenchmarkGumpLibDeflate>();