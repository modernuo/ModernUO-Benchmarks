#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms.BitmapAStar;
using Server.Systems.FeatureFlags;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    /// <summary>
    /// Plan 2F unified pathfinding under BitmapAStarAlgorithm (FastAStar deleted).
    /// The cache feature flags now only affect MovementImpl's per-cell integration
    /// (i.e., the slow-path fallback path used for capability creatures and for
    /// fallthrough cells). Default-walker pathfinds bypass MovementImpl entirely
    /// via BitmapAStar's batched cache lookup.
    ///
    /// Variants:
    ///   Cold      — cache cleared every iteration; measures full cold-build cost
    ///   Warm      — cache stays warm across iterations; measures steady-state
    ///   Shadow    — cache + slow-path divergence recording (overhead only)
    ///   ShadowWarm — Shadow with warm cache
    /// </summary>
    public enum PathProvider
    {
        Cold,
        Warm,
        Shadow,
        ShadowWarm,
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
        // Cold variants clear every iteration to measure cold-build cost.
        // Warm variants skip the clear so BDN's natural warmup primes the cache.
        var isCold = Provider == PathProvider.Cold || Provider == PathProvider.Shadow;
        if (isCold)
        {
            StaticWalkabilityCache.Instance.Clear();
        }

        var shadowOn = Provider == PathProvider.Shadow || Provider == PathProvider.ShadowWarm;

        // Note: PathfindingCacheUseForMovement is mostly irrelevant for default-walker
        // pathfinds because BitmapAStar uses the cache directly. It still affects the
        // MovementImpl per-cell slow-path used for capability-creature fallback. Set it
        // ON for non-shadow variants (matches production-shape).
        PathfindingFeatureFlags.PathfindingCacheShadow = shadowOn;
        PathfindingFeatureFlags.PathfindingCacheUseForMovement = !shadowOn;
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
        // Method name kept as "FastAStar_Find" for benchmark-output continuity with
        // pre-Plan-2F runs. Underlying algorithm is now BitmapAStarAlgorithm.
        var s = _staticScenarios[scenarioIndex];
        var stub = _stubMobiles[scenarioIndex];
        return BitmapAStarAlgorithm.Instance.Find(stub, s.ResolveMap(), s.Start, s.Goal);
    }

    public IEnumerable<int> ScenarioIndices()
    {
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
            yield return i;
        }
    }
}
