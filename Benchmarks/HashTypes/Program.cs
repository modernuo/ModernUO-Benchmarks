using BenchmarkDotNet.Running;
using Benchmarks;

var serialization = BenchmarkRunner.Run<BenchmarkHashTypeSerialization>();
var hashTypes = BenchmarkRunner.Run<BenchmarkHashTypes>();