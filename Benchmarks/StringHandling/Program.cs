using BenchmarkDotNet.Running;
using Benchmarks.BenchmarkUtilities;

var stringHelpers = BenchmarkRunner.Run<BenchmarkStringHelpers>();


// var runner = new BenchmarkStringHelpers();

// runner.BenchmarkFormattingUTF16();