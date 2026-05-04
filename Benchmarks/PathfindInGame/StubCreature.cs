using ModernUO.Serialization;
using Server;
using Server.Mobiles;

namespace PathfindInGame;

[SerializationGenerator(0)]
public sealed partial class StubCreature : BaseCreature
{
    [Constructible]
    public StubCreature() : base(AIType.AI_Animal, FightMode.None, 10, 1)
    {
        Name = "benchmark stub";
        Body = 0xC9;
    }
}
