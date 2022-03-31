using BenchmarkDotNet.Running;
using Benchmarks;

var consoleLogging = BenchmarkRunner.Run<BenchmarkConsoleLogging>();
