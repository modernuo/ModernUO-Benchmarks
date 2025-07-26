using BenchmarkDotNet.Running;
using Benchmarks.ItemSelectors;


var mapItemsSelectors = BenchmarkRunner.Run<BenchmarkMapItemSelectors>();