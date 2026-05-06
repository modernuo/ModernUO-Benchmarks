#nullable enable

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms.BitmapAStar;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    /// <summary>
    /// Three production-relevant variants:
    ///
    ///   Cold         — no .swb file open, cache cleared each iteration. Reference
    ///                  baseline showing the runtime-baker-only cost an operator gets
    ///                  if no .swb files have been baked.
    ///   LazyCold     — .swb files opened as lazy backing stores in [GlobalSetup],
    ///                  cache cleared each iteration. Measures "first pathfind through
    ///                  a region after boot" — chunks reload from file, not from baker.
    ///   LazyWarm     — .swb files opened, cache kept warm across iterations. Production
    ///                  steady-state shape: file-loaded chunks resident, lowest mean
    ///                  per-call latency we ship.
    /// </summary>
    public enum PathProvider
    {
        Cold,
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
        // Cold variants clear chunks every iteration so we measure first-pathfind cost.
        // In Cold the runtime baker rebuilds chunks; in LazyCold the chunk-miss resolves
        // from the open .swb file. LazyWarm keeps everything resident — production
        // steady-state shape.
        if (Provider is PathProvider.Cold or PathProvider.LazyCold)
        {
            // Clear residents but keep the lazy readers open.
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

    public System.Collections.Generic.IEnumerable<int> ScenarioIndices()
    {
        for (var i = 0; i < _staticScenarios.Length; i++)
        {
            yield return i;
        }
    }
}
