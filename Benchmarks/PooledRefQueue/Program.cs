using BenchmarkDotNet.Running;
using Benchmarks;

var pooledRefQueue = BenchmarkRunner.Run<BenchmarkPooledRefQueue>();
