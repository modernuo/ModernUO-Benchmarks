using System.Runtime.CompilerServices;
using Server;

namespace NPCPathing;

public static class MovementScenarios
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CalcMoves(Point3D location, Direction d, out int z) =>
        AllMovementAllowed(location, d, out z);

    public static bool AllMovementAllowed(Point3D location, Direction d, out int z)
    {
        z = 1;
        return true;
    }
}