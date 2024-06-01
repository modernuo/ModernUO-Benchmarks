using BenchmarkDotNet.Running;
using Benchmarks;

var test = BenchmarkRunner.Run<BenchmarkIPAddresses>();