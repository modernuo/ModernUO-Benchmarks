using BenchmarkDotNet.Running;
using Benchmarks.Delegates;

// var test = BenchmarkRunner.Run<BenchmarkFunctionPointers>();
// var test = BenchmarkRunner.Run<BenchmarkConcurrentQueueDelegates>();
var test = BenchmarkRunner.Run<BenchmarkDictionaryActions>();