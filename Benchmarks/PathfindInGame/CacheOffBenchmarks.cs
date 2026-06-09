#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.Systems.FeatureFlags;

namespace PathfindInGame;

/// <summary>
/// Proves the "cache OFF == no regression vs the removed FastAStar" claim for RAM-starved /
/// crappy-hardware shards.
///
/// With <see cref="ContentFeatureFlags.BitmapPathfindingCache"/> = false,
/// <see cref="BitmapAStarAlgorithm"/> routes every cell expansion straight to its per-cell
/// slow path (<c>GetSuccessorsSlowPath</c> → <c>MovementImpl.CheckMovement</c>) with NO cache
/// probe and NO chunk building — the same per-cell work the old FastAStar did. A shard that
/// disables the cache pays zero warming memory; this bench shows it also pays ~1x, not a
/// regression.
///
/// Runs BitmapAStar (cache forced OFF) head-to-head against the vendored FastAStar baseline
/// over the same corpus. Expect ratio ≈ 1.0 across scenarios, zero allocations for both.
///
/// REQUIRES the ModernUO submodule to expose ContentFeatureFlags.BitmapPathfindingCache
/// (present on the pathfinding branch). Run:
///   dotnet run -c Release -- --filter '*CacheOffBenchmarks*'
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CacheOffBenchmarks
{
    private static readonly PathfindScenario[] _scenarios = LoadCorpus();
    private StubCreature[] _stubs = null!;
    private bool _prevFlag;

    private static PathfindScenario[] LoadCorpus()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus", "baseline.jsonl");
        return ScenarioCorpus.LoadJsonl(corpusPath);
    }

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkFixture.EnsureInitialized();

        _prevFlag = ContentFeatureFlags.BitmapPathfindingCache;
        ContentFeatureFlags.BitmapPathfindingCache = false; // force pure slow path, no cache

        _stubs = new StubCreature[_scenarios.Length];
        for (var i = 0; i < _scenarios.Length; i++)
        {
            var s = _scenarios[i];
            var stub = new StubCreature { CanSwim = s.CanSwim, CantWalk = false };
            stub.SetMobilityFlags(s.CanOpenDoors, s.CanMoveOverObstacles);
            stub.MoveToWorld(s.Start, s.ResolveMap());
            _stubs[i] = stub;
        }

        // No cache state should matter with the flag off; drop any residents/readers so the
        // measurement is unambiguously the slow path.
        StepCache.Instance.CloseLazyReaders();
        StepCache.Instance.ClearResidentChunks();
    }

    [GlobalCleanup]
    public void Cleanup() => ContentFeatureFlags.BitmapPathfindingCache = _prevFlag;

    public IEnumerable<int> ScenarioIndices()
    {
        for (var i = 0; i < _scenarios.Length; i++)
        {
            yield return i;
        }
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(ScenarioIndices))]
    public Direction[]? FastAStar_Find(int scenarioIndex)
    {
        var s = _scenarios[scenarioIndex];
        return FastAStarAlgorithm.Instance.Find(_stubs[scenarioIndex], s.ResolveMap(), s.Start, s.Goal);
    }

    [Benchmark]
    [ArgumentsSource(nameof(ScenarioIndices))]
    public Direction[]? BitmapAStar_CacheOff_Find(int scenarioIndex)
    {
        var s = _scenarios[scenarioIndex];
        return BitmapAStarAlgorithm.Instance.Find(_stubs[scenarioIndex], s.ResolveMap(), s.Start, s.Goal);
    }
}
