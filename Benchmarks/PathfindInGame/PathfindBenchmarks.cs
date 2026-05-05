#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    /// <summary>
    /// Three production-relevant variants. Earlier matrices (Warm, PrecomputedCold,
    /// PrecomputedWarm without Tier 4) confirmed redundant once Tier 4 became the
    /// shipping shape — see the BDN history in the design docs for the data.
    ///
    /// Variants:
    ///   Cold                  — no file, cache cleared each iteration. Reference
    ///                           baseline showing the runtime-baker-only cost an
    ///                           operator gets if they don't bake .swb files at all.
    ///   PrecomputedColdTier4  — file loaded in [GlobalSetup] (with Tier 4 strata in v5),
    ///                           Tier 4 memo populated per scenario, cache cleared each
    ///                           iteration. Measures "first pathfind after boot" — chunks
    ///                           reload from file each iter, strata survive in the memo.
    ///   PrecomputedWarmTier4  — file loaded + Tier 4 memo populated, cache kept warm.
    ///                           Production steady-state shape: file-loaded chunks
    ///                           resident, multi-Z and source-Z fallthroughs absorbed
    ///                           by Tier 4. Lowest mean per-call latency we ship.
    /// </summary>
    public enum PathProvider
    {
        Cold,
        PrecomputedColdTier4,
        PrecomputedWarmTier4,
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

        // Always start with no precomputed files registered. Each variant decides whether
        // to load them and whether to populate the Tier 4 memo.
        StaticWalkabilityCache.Instance.UnloadAllPrecomputed();
        StaticWalkabilityCache.Instance.ClearTier4Memo();

        if (Provider == PathProvider.PrecomputedColdTier4
            || Provider == PathProvider.PrecomputedWarmTier4)
        {
            LoadPrecomputedFilesForCorpus();
            BuildTier4ForCorpus();
        }
    }

    /// <summary>
    /// Tier 4 PoC: pre-resolve every fallthrough cell in each scenario's exploration
    /// footprint over a Z range that covers stair / partial-step destinations. Sweeps
    /// sourceZ ∈ [-8, 32] which is the empirical range A* expands into for the bench
    /// corpus. A production Tier 4 would discover the precise Z-strata at bake time
    /// rather than spraying the range, but for the bench we want the ceiling number.
    /// </summary>
    private void BuildTier4ForCorpus()
    {
        var cache = StaticWalkabilityCache.Instance;
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
            var s = _staticScenarios[i];
            var stub = _stubMobiles[i];
            var map = s.ResolveMap();
            var midX = (s.StartX + s.GoalX) / 2;
            var midY = (s.StartY + s.GoalY) / 2;
            for (sbyte sz = -8; sz <= 32; sz++)
            {
                cache.BuildTier4ForRegion(stub, map, midX, midY, range: 24, sourceZ: sz);
            }
        }
    }

    /// <summary>
    /// Plan 2E.1.B — register the pre-baked <c>&lt;mapId&gt;.swb</c> files for every
    /// map referenced by the corpus. Path resolves via the
    /// <c>MODERNUO_PATHFINDING_DATA_DIR</c> environment variable; falls back to
    /// walking upward from <see cref="AppContext.BaseDirectory"/> looking for a
    /// <c>ModernUO/Distribution/Data/Pathfinding/</c> directory. (BenchmarkDotNet
    /// runs the harness from a generated subdir under bin/Release, so a fixed
    /// <c>../</c> count would be brittle.)
    /// </summary>
    private static void LoadPrecomputedFilesForCorpus()
    {
        var dir = ResolvePrecomputedDir();

        if (dir is null || !Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException(
                $"Precomputed pathfinding cache directory not found (searched upward from " +
                $"'{AppContext.BaseDirectory}'). " +
                "Set MODERNUO_PATHFINDING_DATA_DIR or run the bake tool to populate " +
                "<Distribution>/Data/Pathfinding/."
            );
        }

        var loaded = new HashSet<int>();
        foreach (var s in _staticScenarios)
        {
            if (!loaded.Add(s.MapId))
            {
                continue;
            }

            var path = Path.Combine(dir, $"{s.MapId}.swb");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Precomputed cache file missing for map {s.MapId} (scenario '{s.Name}'): {path}"
                );
            }

            StaticWalkabilityCache.Instance.LoadPrecomputed(s.MapId, path);
        }
    }

    private static string? ResolvePrecomputedDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("MODERNUO_PATHFINDING_DATA_DIR");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return fromEnv;
        }

        // Walk up from AppContext.BaseDirectory looking for ModernUO/Distribution/Data/Pathfinding/.
        // Bound the walk to avoid infinite loop on a malformed path.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && current is not null; i++)
        {
            var candidate = Path.Combine(current.FullName, "ModernUO", "Distribution", "Data", "Pathfinding");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        return null;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Cold variants (Cold, PrecomputedColdTier4) clear chunks every iteration to
        // measure first-pathfind cost: in Cold the runtime baker rebuilds chunks; in
        // PrecomputedColdTier4 the chunk-miss resolves from the registered .swb file
        // and the Tier 4 memo persists (it's not in _chunks). PrecomputedWarmTier4
        // keeps everything resident — production steady-state shape.
        if (Provider == PathProvider.Cold || Provider == PathProvider.PrecomputedColdTier4)
        {
            StaticWalkabilityCache.Instance.Clear();
        }
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
