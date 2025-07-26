// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using BitArrayBenchmarks;

var test = BenchmarkRunner.Run<BitArrayDeserializeBenchmarks>();