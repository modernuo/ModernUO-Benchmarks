using BenchmarkDotNet.Running;
using Benchmarks;

var test = BenchmarkRunner.Run<BenchmarkNewLayoutGumps>();
// var test = BenchmarkRunner.Run<BenchmarkCraftGumps>();