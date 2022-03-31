using BenchmarkDotNet.Running;
using Benchmarks;
using Benchmarks.BenchmarkUtilities;

var stringHelpers = BenchmarkRunner.Run<BenchmarkStringHelpers>();
