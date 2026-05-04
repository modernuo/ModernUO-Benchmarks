using Server;

namespace PathfindInGame;

public sealed record PathfindScenario(
    string Name,
    int MapId,
    int StartX,
    int StartY,
    int StartZ,
    int GoalX,
    int GoalY,
    int GoalZ,
    bool CanSwim,
    bool CanFly,
    bool CanOpenDoors,
    bool CanMoveOverObstacles
)
{
    public Point3D Start => new(StartX, StartY, StartZ);
    public Point3D Goal => new(GoalX, GoalY, GoalZ);
    public Map ResolveMap() => Map.Maps[MapId];
}
