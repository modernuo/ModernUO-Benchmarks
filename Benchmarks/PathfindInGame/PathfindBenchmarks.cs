#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.PathAlgorithms.FastAStar;

namespace PathfindInGame;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PathfindBenchmarks
{
    // BenchmarkDotNet calls ScenarioIndices() before [GlobalSetup] to build the
    // parameter matrix. Load the corpus once into a static field so both
    // ScenarioIndices and Setup share the same data without a null-reference.
    private static readonly List<PathfindScenario> _staticScenarios = LoadCorpus();

    private List<StubCreature> _stubMobiles = null!;

    private static List<PathfindScenario> LoadCorpus()
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

        _stubMobiles = new List<StubCreature>(_staticScenarios.Count);
        foreach (var s in _staticScenarios)
        {
            var stub = new StubCreature
            {
                CanSwim = s.CanSwim,
                CantWalk = false
            };
            stub.SetMobilityFlags(s.CanOpenDoors, s.CanMoveOverObstacles);
            stub.MoveToWorld(s.Start, s.ResolveMap());
            _stubMobiles.Add(stub);
        }
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
        for (var i = 0; i < _staticScenarios.Count; i++)
        {
            yield return i;
        }
    }
}
