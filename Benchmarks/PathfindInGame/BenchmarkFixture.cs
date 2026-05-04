using System.Reflection;
using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Tests.Maps;

namespace PathfindInGame;

public static class BenchmarkFixture
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        Core.ApplicationAssembly = Assembly.GetExecutingAssembly();
        Core.LoopContext = new EventLoopContext();
        Core.Expansion = Expansion.EJ;

        ServerConfiguration.Load(true);
        ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
        AssemblyHandler.LoadAssemblies(["Server.dll", "UOContent.dll"]);

        NPCSpeeds.Configure();

        SkillsInfo.Configure();
        Server.Network.NetState.Configure();
        TestMapDefinitions.ConfigureTestMapDefinitions();

        World.Configure();
        Timer.Init(0);
        RaceDefinitions.Configure();
        World.Load();
        World.ExitSerializationThreads();
        DecayScheduler.Configure();

        _initialized = true;
    }
}
