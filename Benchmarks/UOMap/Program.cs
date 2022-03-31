using BenchmarkDotNet.Running;
using Benchmarks;
using Benchmarks.EntitiesSelectors;
using Benchmarks.ItemSelectors;
using Benchmarks.MobileSelectors;
using Benchmarks.MultiSelectors;
using Benchmarks.MultiTilesSelectors;

var mapEntitiesSelectors = BenchmarkRunner.Run<BenchmarkMapEntitiesSelectors>();
var mapMobilesSelectors = BenchmarkRunner.Run<BenchmarkMapMobileSelectors>();
var mapMultiTilesSelectors = BenchmarkRunner.Run<BenchmarkMapMultiTilesSelectors>();
var mapMultiSelectors = BenchmarkRunner.Run<BenchmarkMapMultiSelectors>();
var mapItemsSelectors = BenchmarkRunner.Run<BenchmarkMapItemSelectors>();
