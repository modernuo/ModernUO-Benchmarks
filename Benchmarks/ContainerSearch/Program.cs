using BenchmarkDotNet.Running;
using Benchmarks;

var containerSearch = BenchmarkRunner.Run<BenchmarkContainerSearch>();