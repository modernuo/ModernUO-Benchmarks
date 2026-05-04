using BenchmarkDotNet.Running;
using Benchmarks;

var vectors = BenchmarkRunner.Run<BenchmarkVectorAverage>();