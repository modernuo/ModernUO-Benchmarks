using Server;
using Server.Items;

namespace PathfindInGame;

/// <summary>
/// Fixed-coordinate multi (house/boat) placements and the pathfinding routes that traverse
/// them, for the multi-pathfinding benchmark. The placements and routes are co-located so they
/// stay in sync: <see cref="Place"/> drops the multis into the live world during fixture init,
/// and <see cref="Routes"/> drives <c>BitmapAStarAlgorithm.Find</c> through the resulting
/// dense multi coverage.
///
/// Signal: every multi-covered cell (footprint + 1-cell halo) returns <c>Fallthrough_Multi</c>
/// and is served by the single-pass <c>ComputeMultiMaskAt</c> synthesizer when the cache is on,
/// or by 8x per-cell <c>CheckMovement</c> when it's off. A route that weaves through a tight
/// grid of houses maximizes the count of such expansions, so the cache-on/off delta isolates the
/// synthesizer's per-multi-cell win.
/// </summary>
public static class MultiScenarios
{
    public const int MapId = 1;          // Trammel
    private const int GuildHouseId = 0x74; // static house: walls, a door aperture, floor

    public readonly record struct Placement(int Id, int X, int Y);
    public readonly record struct Route(string Name, int Sx, int Sy, int Gx, int Gy);

    // Green Acres (Trammel ~5445,1153) — the flat, empty staff/test region. Houses placed here have
    // genuinely footprint-CLEAN lots (terrain below the floor everywhere), so the Phase-3 per-instance
    // interior cache serves — reflecting a legitimately-placed house. (The old cluttered band
    // 1460/1480/1500,1620 overlaps tall map statics and is now correctly classified DIRTY → the cache
    // degrades to live synthesis there, byte-identical to the slow path.)
    public static Placement[] Placements() => new[]
    {
        new Placement(GuildHouseId, 5445, 1153),
        new Placement(GuildHouseId, 5475, 1153),
    };

    // Straight S->N through each footprint forces a side detour and expands the house interior, where
    // the clean instance's interior cells serve from the per-multiID cache (~20 ns lookups).
    public static readonly Route[] Routes =
    {
        new Route("around_a", 5445, 1163, 5445, 1143),
        new Route("around_b", 5475, 1163, 5475, 1143),
    };

    /// <summary>
    /// Place the benchmark multis into <paramref name="map"/>. Idempotent-ish: intended to run
    /// exactly once from the fixture's one-time init. Requires <c>MultiData.Configure()</c> to have
    /// run (so <c>Components</c> resolves) and the world/map sectors to be loaded.
    /// </summary>
    public static void Place(Map map)
    {
        foreach (var p in Placements())
        {
            var multi = new BenchMulti(p.Id);
            multi.MoveToWorld(new Point3D(p.X, p.Y, map.GetAverageZ(p.X, p.Y)), map);
        }
    }
}
