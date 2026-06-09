#nullable enable

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Server.Systems.FeatureFlags;

namespace PathfindInGame;

/// <summary>
/// Measures <c>BitmapAStarAlgorithm.Find</c> over routes that weave through a dense grid of houses
/// (see <see cref="MultiScenarios"/>), with the pathfinding cache ON vs OFF:
///
/// - Cache ON  → each multi-covered cell (footprint + halo) is served by the single-pass
///   <c>ComputeMultiMaskAt</c> synthesizer (one mask build per cell).
/// - Cache OFF → each such cell is served by the slow path's 8x per-cell <c>CheckMovement</c>.
///
/// The ON/OFF delta isolates the synthesizer's per-multi-cell win. REQUIRES the ModernUO submodule
/// at the synthesizer branch (server/pathfinding-multi-tests); against plain main both arms route
/// multi cells through the slow path and the delta collapses to ~0 (which is itself a useful
/// baseline sanity check).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MultiPathfindBenchmarks
{
    private static readonly MultiScenarios.Route[] _routes = MultiScenarios.Routes;
    private StubCreature _stub = null!;
    private Map _map = null!;
    private bool _prevFlag;

    [Params(false, true)]
    public bool CacheOn;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkFixture.EnsureInitialized(); // boots the world AND places the benchmark multis (once)

        _prevFlag = ContentFeatureFlags.BitmapPathfindingCache;
        ContentFeatureFlags.BitmapPathfindingCache = CacheOn;

        _map = Map.Maps[MultiScenarios.MapId];

        // Multi detour routes wander further than the static corpus; give A* headroom so longer
        // weave routes resolve instead of bailing at the default budget. Applied to both arms.
        BitmapAStarAlgorithm.Instance.MaxSearchNodes = 3000;

        _stub = new StubCreature();
        _stub.MoveToWorld(Start(0), _map);

        // Warm the static chunks each route touches so the measured Find pays only search +
        // multi-synthesis (or slow-path) cost, not first-touch BuildChunk cost. With the cache
        // off this is a no-op probe; with it on it primes the resident static base.
        StepCache.Instance.CloseLazyReaders();
        for (var i = 0; i < _routes.Length; i++)
        {
            for (var w = 0; w < 5; w++)
            {
                BitmapAStarAlgorithm.Instance.Find(_stub, _map, Start(i), Goal(i));
            }
        }

        ReportRouteHealthOnce();
    }

    private static bool _healthReported;

    /// <summary>
    /// One-time route audit (stderr, not measured). Confirms each route actually finds a path AND
    /// expands multi-covered cells (MultiLocalHits &gt; 0) — i.e. the synthesizer is genuinely
    /// exercised. A route that returns NO PATH or hits zero multi cells is a degenerate fixture
    /// (bad coords / cluster fully blocks / placement off-surface) whose timing is meaningless.
    /// </summary>
    private void ReportRouteHealthOnce()
    {
        if (_healthReported)
        {
            return;
        }
        _healthReported = true;

        var cache = StepCache.Instance;
        var wasFlag = ContentFeatureFlags.BitmapPathfindingCache;
        var wasThreshold = cache.MissPromotionThreshold;
        ContentFeatureFlags.BitmapPathfindingCache = true;
        cache.MissPromotionThreshold = 1; // eager build so the static base serves hits, not NotBuilt
        try
        {
            System.Console.Error.WriteLine("[MultiHealth] idx route               result          multiLocalHits");
            for (var i = 0; i < _routes.Length; i++)
            {
                for (var w = 0; w < 3; w++)
                {
                    BitmapAStarAlgorithm.Instance.Find(_stub, _map, Start(i), Goal(i));
                }

                var before = cache.GetStats().MultiLocalHits;
                var path = BitmapAStarAlgorithm.Instance.Find(_stub, _map, Start(i), Goal(i));
                var hits = cache.GetStats().MultiLocalHits - before;

                var result = path == null ? "NO PATH" : $"{path.Length} steps";
                var flag = path == null ? "  <-- SUSPECT (coords/blocked)"
                    : hits == 0 ? "  <-- no multi cells expanded" : "";
                System.Console.Error.WriteLine(
                    $"[MultiHealth] [{i,2}] {_routes[i].Name,-18} {result,-15} {hits,8}{flag}"
                );
            }
        }
        finally
        {
            ContentFeatureFlags.BitmapPathfindingCache = wasFlag;
            cache.MissPromotionThreshold = wasThreshold;
            cache.ClearResidentChunks();
        }
    }

    [GlobalCleanup]
    public void Cleanup() => ContentFeatureFlags.BitmapPathfindingCache = _prevFlag;

    public IEnumerable<int> RouteIndices()
    {
        for (var i = 0; i < _routes.Length; i++)
        {
            yield return i;
        }
    }

    private Point3D Start(int i) => new(_routes[i].Sx, _routes[i].Sy, _map.GetAverageZ(_routes[i].Sx, _routes[i].Sy));
    private Point3D Goal(int i) => new(_routes[i].Gx, _routes[i].Gy, _map.GetAverageZ(_routes[i].Gx, _routes[i].Gy));

    [Benchmark]
    [ArgumentsSource(nameof(RouteIndices))]
    public Direction[]? MultiFind(int routeIndex) =>
        BitmapAStarAlgorithm.Instance.Find(_stub, _map, Start(routeIndex), Goal(routeIndex));
}
