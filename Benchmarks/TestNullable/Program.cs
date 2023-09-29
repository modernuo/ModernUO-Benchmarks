using BenchmarkDotNet.Running;
using Benchmarks;

var nullables = BenchmarkRunner.Run<BenchmarkNullable>();