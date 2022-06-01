using Benchmarks;
using BenchmarkDotNet.Running;
using Benchmarks.BenchmarkUtilities;

var stringHelpers = BenchmarkRunner.Run<BenchmarkLocalizationInterpolation>();
