#nullable enable

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;

namespace PathfindInGame;

/// <summary>
/// Sweeps the A* node-expansion budget (<see cref="BitmapAStarAlgorithm.MaxSearchNodes"/>,
/// the "pathfinding.maxSearchNodes" shard setting) over three representative shapes, to size
/// it without introducing perf regressions:
///
///   open   — short open-terrain path; A* terminates immediately when the goal is found, so
///            this is budget-INSENSITIVE (the control: raising the budget must not slow it).
///   detour — a walled-off goal 2 tiles away reachable only via a ~33-step route around the
///            Britain Inn (Trammel). Needs ~500 expansions; returns NULL below that.
///   fail   — the owner one floor up (Z+20) behind walls, unreachable within the 38-tile
///            window. The search always runs to the FULL budget, so this is the worst-case
///            per-Find cost — the number that bounds how high the budget can safely go.
///
/// Expected shape: 'open' flat across all budgets; 'detour' NULL at 300, found from ~500 up;
/// 'fail' rising with budget then PLATEAUING (~1500) once the reachable window is exhausted.
/// The cache is warmed in setup and kept warm (no per-iteration clear) so this isolates
/// search cost from chunk-build cost.
///
/// REQUIRES the ModernUO submodule to include the `MaxSearchNodes` field (the
/// pathfinding.maxSearchNodes change). Bump the submodule before building/running.
/// Run e.g.:  dotnet run -c Release -- --filter '*MaxSearchNodesBenchmarks*'
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MaxSearchNodesBenchmarks
{
    [Params(300, 500, 1000, 1500, 2000)]
    public int MaxSearchNodes { get; set; }

    // Trammel coordinates. Index 0 = open, 1 = detour, 2 = fail (see class summary).
    private static readonly Point3D[] _starts =
    {
        new(1455, 1560, 30),
        new(1443, 1568, 30),
        new(1443, 1568, 30),
    };

    private static readonly Point3D[] _goals =
    {
        new(1449, 1560, 30),
        new(1444, 1566, 30),
        new(1444, 1566, 50),
    };

    private StubCreature _stub = null!;
    private Map _map = null!;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkFixture.EnsureInitialized();
        BitmapAStarAlgorithm.Instance.MaxSearchNodes = MaxSearchNodes;

        _map = Map.Maps[1]; // Trammel
        _stub = new StubCreature();
        _stub.MoveToWorld(_starts[0], _map);

        // Steady-state warm cache: prime the chunks each route touches so the measured Find
        // pays only search cost, not first-touch BuildChunk cost.
        StepCache.Instance.CloseLazyReaders();
        for (var i = 0; i < _starts.Length; i++)
        {
            for (var w = 0; w < 5; w++)
            {
                BitmapAStarAlgorithm.Instance.Find(_stub, _map, _starts[i], _goals[i]);
            }
        }
    }

    [Benchmark]
    [Arguments(0)] // open   (control: budget-insensitive)
    [Arguments(1)] // detour (~33-step indoor route; NULL until budget >= ~500)
    [Arguments(2)] // fail   (unreachable; runs to full budget = worst case)
    public Direction[]? Find(int scenario) =>
        BitmapAStarAlgorithm.Instance.Find(_stub, _map, _starts[scenario], _goals[scenario]);
}
