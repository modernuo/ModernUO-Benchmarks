using ModernUO.Serialization;
using Server;
using Server.Mobiles;

namespace PathfindInGame;

[SerializationGenerator(0)]
public sealed partial class StubCreature : BaseCreature
{
    private bool _canOpenDoors;
    private bool _canMoveOverObstacles;

    [Constructible]
    public StubCreature() : base(AIType.AI_Animal, FightMode.None, 10, 1)
    {
        Name = "benchmark stub";
        Body = 0xC9;
    }

    public override bool CanOpenDoors
    {
        get => _canOpenDoors;
    }

    public override bool CanMoveOverObstacles
    {
        get => _canMoveOverObstacles;
    }

    public void SetMobilityFlags(bool canOpenDoors, bool canMoveOverObstacles)
    {
        _canOpenDoors = canOpenDoors;
        _canMoveOverObstacles = canMoveOverObstacles;
    }
}
