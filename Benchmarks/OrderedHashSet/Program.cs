using BenchmarkDotNet.Running;
using Benchmarks;

var orderedHashSet = BenchmarkRunner.Run<BenchmarkOrderedHashSet>();
