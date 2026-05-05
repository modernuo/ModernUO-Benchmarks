using System;
using System.Reflection;
using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Movement;
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

        // Wire the local UO client data files (env var override or fallback).
        var clientFiles = Environment.GetEnvironmentVariable("MODERNUO_TEST_DATA_DIR")
                          ?? @"C:\Ultima Online Classic";
        ServerConfiguration.DataDirectories.Add(clientFiles);

        AssemblyHandler.LoadAssemblies(["Server.dll", "UOContent.dll"]);

        NPCSpeeds.Configure();

        SkillsInfo.Configure();
        Server.Network.NetState.Configure();
        TestMapDefinitions.ConfigureTestMapDefinitions();

        World.Configure();
        Timer.Init(0);
        RaceDefinitions.Configure();
        MovementImpl.Configure();
        World.Load();
        World.ExitSerializationThreads();
        DecayScheduler.Configure();

        // CRITICAL: TileData's static cctor short-circuits when running outside the live
        // server (see Server/TileData.cs ~line 295). Force-load via reflection so
        // LandTable/ItemTable flags are populated; without this every tile reads as
        // flag=None and FastAStar treats everything as walkable (meaningless 86ns paths).
        ForceLoadTileData();

        _initialized = true;
    }

    private static void ForceLoadTileData()
    {
        var loadMethod = typeof(TileData).GetMethod(
            "Load",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        if (loadMethod == null)
        {
            throw new InvalidOperationException(
                "TileData.Load not found via reflection — engine may have refactored."
            );
        }
        loadMethod.Invoke(null, null);
    }
}
