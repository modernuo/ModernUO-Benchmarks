using Server.Items;

namespace PathfindInGame;

/// <summary>
/// Lightweight <see cref="BaseMulti"/> for the benchmark: places a fixed-design multi (house/boat)
/// by itemID into the live world without the owning BaseHouse / BaseBoat machinery. Mirrors the
/// synthesizer test suite's <c>TestMulti</c>. <c>Components</c> resolves to the shared
/// <c>MultiData.GetComponents(itemID)</c> MCL — which is all the pathfinding path reads — so this
/// is a faithful stand-in for the per-cell walkability the synthesizer/slow-path measures.
/// </summary>
public sealed class BenchMulti : BaseMulti
{
    public BenchMulti(int itemID) : base(itemID)
    {
    }
}
