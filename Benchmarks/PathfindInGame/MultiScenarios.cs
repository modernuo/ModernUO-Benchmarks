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

    // Open Trammel region the synthesizer's MultiPathInvariant / HousePathRouting tests use and
    // know to be feasible. Start from a single proven house+route (validated to ~29 steps against
    // this exact submodule code), then layer in well-separated neighbours that still leave walkable
    // streets, so a route can weave the cluster hugging footprint halos.
    public readonly record struct Placement(int Id, int X, int Y);
    public readonly record struct Route(string Name, int Sx, int Sy, int Gx, int Gy);

    // Three houses in a row at the proven-open y=1620 band, 20 tiles apart (wide walkable streets
    // between footprints). A straight east-west route across them detours around each in sequence,
    // expanding three footprints' worth of wall/halo cells the synthesizer serves.
    public static Placement[] Placements() => new[]
    {
        new Placement(GuildHouseId, 1460, 1620),
        new Placement(GuildHouseId, 1480, 1620),
        new Placement(GuildHouseId, 1500, 1620),
    };

    // One vertical around-the-house route per placement (the MultiPathInvariantTests pattern:
    // straight S->N through a footprint forces a side detour, ~29 steps, ~150 multi-cell synthesis
    // calls each). Three clean single-house data points in the same proven-open band.
    // Two feasible vertical around-the-house routes (validated: ~48 steps/246 multi-cell synthesis
    // calls and ~29 steps/154). The eastern house's south approach is blocked by natural terrain
    // (the search wanders into a dead end), so it's excluded — a failing search measures budget-
    // exhaustion cost, not pathfinding cost.
    public static readonly Route[] Routes =
    {
        new Route("around_w", 1460, 1630, 1460, 1610),
        new Route("around_c", 1480, 1630, 1480, 1610),
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
