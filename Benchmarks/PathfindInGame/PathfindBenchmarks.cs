#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms.FastAStar;
using Server.Systems.FeatureFlags;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    public enum PathProvider
    {
        SlowPath,
        CachedClean,
        CachedClean_Warm,
        CachedShadow,
        CachedShadow_Warm,
    }

    [ParamsAllValues]
    public PathProvider Provider { get; set; }

    // BenchmarkDotNet calls ScenarioIndices() before [GlobalSetup] to build the
    // parameter matrix. Load the corpus once into a static field so both
    // ScenarioIndices and Setup share the same data without a null-reference.
    private static readonly PathfindScenario[] _staticScenarios = LoadCorpus();

    private StubCreature[] _stubMobiles = null!;

    private static PathfindScenario[] LoadCorpus()
    {
        var corpusPath = Path.Combine(
            AppContext.BaseDirectory,
            "Corpus",
            "baseline.jsonl"
        );
        return ScenarioCorpus.LoadJsonl(corpusPath);
    }

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkFixture.EnsureInitialized();

        _stubMobiles = new StubCreature[_staticScenarios.Length];
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
          var s = _staticScenarios[i];
          var stub = new StubCreature
          {
            CanSwim = s.CanSwim,
            CantWalk = false
          };
          stub.SetMobilityFlags(s.CanOpenDoors, s.CanMoveOverObstacles);
          stub.MoveToWorld(s.Start, s.ResolveMap());
          _stubMobiles[i] = stub;
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Cold-cache variants clear every iteration to measure cold-build cost.
        // Warm-cache variants skip the clear so BDN's natural warmup primes the cache,
        // and the measured iterations hit warm chunks.
        var isCold = Provider == PathProvider.CachedClean || Provider == PathProvider.CachedShadow;
        if (isCold)
        {
            // Fully-qualified — PathfindInGame namespace might collide if `using Server.Engines.Pathing.Cache`
            // ever introduced a name clash. Defensive.
            Server.Engines.Pathing.Cache.StaticWalkabilityCache.Instance.Clear();
        }

        var shadowOn = Provider == PathProvider.CachedShadow || Provider == PathProvider.CachedShadow_Warm;
        var useCacheOn = Provider == PathProvider.CachedClean || Provider == PathProvider.CachedClean_Warm;

        PathfindingFeatureFlags.PathfindingCacheShadow = shadowOn;
        PathfindingFeatureFlags.PathfindingCacheUseForMovement = useCacheOn;
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        PathfindingFeatureFlags.PathfindingCacheShadow = false;
        PathfindingFeatureFlags.PathfindingCacheUseForMovement = false;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ScenarioIndices))]
    public Direction[]? FastAStar_Find(int scenarioIndex)
    {
        var s = _staticScenarios[scenarioIndex];
        var stub = _stubMobiles[scenarioIndex];
        return FastAStarAlgorithm.Instance.Find(stub, s.ResolveMap(), s.Start, s.Goal);
    }

    public IEnumerable<int> ScenarioIndices()
    {
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
            yield return i;
        }
    }
}
