using BenchmarkDotNet.Running;
using Benchmarks.BenchmarkUtilities;

var stringHelpers = BenchmarkRunner.Run<BenchmarkStringHelpers>();
