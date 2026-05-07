#nullable enable

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms.BitmapAStar;
using Server.PathAlgorithms.FastAStar;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    /// <summary>
    /// Four production-relevant variants:
    ///
    ///   Cold         — no .swb file open, cache cleared each iteration. Pessimistic:
    ///                  every iteration pays the full BuildChunk cost. Models a "first
    ///                  pathfind ever in this region" event, not steady-state — a real
    ///                  hobby admin without baked files only sees this for the FIRST
    ///                  pathfind in each region per server lifetime.
    ///   WarmNoFile   — no .swb file open, cache kept warm across iterations. The
    ///                  realistic hobby-admin workload: no .swb files, but BDN warmup
    ///                  populates the chunks once via the runtime baker; measurement
    ///                  iterations all hit the warm cache. After warmup, this should
    ///                  match LazyWarm — the .swb file's only job is to make the FIRST
    ///                  pathfind cheaper, not the steady state.
    ///   LazyCold     — .swb files opened as lazy backing stores in [GlobalSetup],
    ///                  cache cleared each iteration. Measures "first pathfind through
    ///                  a region after boot" with a baked file present — chunks reload
    ///                  from file, not from baker.
    ///   LazyWarm     — .swb files opened, cache kept warm across iterations. Production
    ///                  steady-state shape: file-loaded chunks resident, lowest mean
    ///                  per-call latency we ship.
    /// </summary>
    public enum PathProvider
    {
        Cold,
        WarmNoFile,
        LazyCold,
        LazyWarm,
    }

    [ParamsAllValues]
    public PathProvider Provider { get; set; }

    private static readonly PathfindScenario[] _staticScenarios = LoadCorpus();

    private StubCreature[] _stubMobiles = null!;

    private static PathfindScenario[] LoadCorpus()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus", "baseline.jsonl");
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

        StepCache.Instance.CloseLazyReaders();

        if (Provider is PathProvider.LazyCold or PathProvider.LazyWarm)
        {
            OpenLazyReadersForCorpus();
        }
    }

    /// <summary>
    /// Open the pre-baked &lt;mapId&gt;.swb files for every map referenced by the corpus
    /// as lazy backing stores. The fixture has already invoked the baker if any file was
    /// missing or stale (see BenchmarkFixture.EnsureBakedFiles).
    /// </summary>
    private static void OpenLazyReadersForCorpus()
    {
        var dir = BenchmarkFixture.ResolvePathfindingDataDir();
        var loaded = new System.Collections.Generic.HashSet<int>();
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
            if (!StepCache.Instance.TryOpenLazyReader(path, s.MapId))
            {
                throw new InvalidDataException(
                    $"StepCache.TryOpenLazyReader rejected {path} (bad magic, version, or stale TileDataHash)"
                );
            }
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Cold/LazyCold clear residents so every iteration measures first-pathfind cost
        // from baker / file respectively. WarmNoFile and LazyWarm keep the cache warm
        // across iterations — measurement iterations land on cache hits after BDN's
        // warmup populates the chunks.
        if (Provider is PathProvider.Cold or PathProvider.LazyCold)
        {
            StepCache.Instance.ClearResidentChunks();
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(ScenarioIndices))]
    public Direction[]? BitmapAStar_Find(int scenarioIndex)
    {
        var s = _staticScenarios[scenarioIndex];
        var stub = _stubMobiles[scenarioIndex];
        return BitmapAStarAlgorithm.Instance.Find(stub, s.ResolveMap(), s.Start, s.Goal);
    }

    /// <summary>
    /// Pre-cache baseline: the FastAStarAlgorithm that ModernUO shipped before the
    /// step-cache rework (snapshot at e1e1a7c640). Pulled in directly so the bench can
    /// produce apples-to-apples comparison numbers without checking out an old branch.
    /// FastAStar is cache-agnostic, so the Provider param has no effect on its results
    /// — every Provider variant produces ~the same number for this method. Filter to a
    /// single Provider if you want a clean row per scenario, e.g.:
    ///   --filter '*FastAStar_Find*Provider=Cold*'
    /// </summary>
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(ScenarioIndices))]
    public Direction[]? FastAStar_Find(int scenarioIndex)
    {
        var s = _staticScenarios[scenarioIndex];
        var stub = _stubMobiles[scenarioIndex];
        return FastAStarAlgorithm.Instance.Find(stub, s.ResolveMap(), s.Start, s.Goal);
    }

    public System.Collections.Generic.IEnumerable<int> ScenarioIndices()
    {
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
            yield return i;
        }
    }
}
