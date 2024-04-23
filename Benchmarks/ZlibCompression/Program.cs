using BenchmarkDotNet.Running;
using Benchmarks;

var benchmark = BenchmarkRunner.Run<BenchmarkGumpLibDeflate>();

//BenchmarkGumpLibDeflate c = new();
//c.Zlib();