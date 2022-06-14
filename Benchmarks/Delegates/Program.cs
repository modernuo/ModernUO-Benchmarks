using BenchmarkDotNet.Running;
using Benchmarks.Delegates;

var test = BenchmarkRunner.Run<BenchmarkFunctionPointers>();