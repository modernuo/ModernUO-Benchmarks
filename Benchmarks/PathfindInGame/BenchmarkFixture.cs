using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.Engines.Pathing.Cache;
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

        EnsureWalkabilityCacheBaked();

        _initialized = true;
    }

    /// <summary>
    /// Make sure each map referenced by the bench corpus has a fresh, valid
    /// <c>&lt;mapId&gt;.swb</c> file before the harness starts measuring. Calls
    /// <see cref="WalkabilityCacheBaker.BakeMap"/> in-process if the file is missing
    /// or has a stale tile-data hash. Single source of truth — no standalone tool.
    /// </summary>
    private static void EnsureWalkabilityCacheBaked()
    {
        var dir = ResolveWalkabilityCacheDir();
        Directory.CreateDirectory(dir);

        var liveHash = PrecomputedCacheFile.ComputeLiveTileDataHash();
        var mapsToBake = new HashSet<int> { 1 }; // bench corpus is currently Trammel-only.

        foreach (var mapId in mapsToBake)
        {
            var path = Path.Combine(dir, $"{mapId}.swb");
            if (FileMatchesLiveHash(path, liveHash))
            {
                continue;
            }

            Console.Error.WriteLine($"[BenchmarkFixture] Baking walkability cache for map {mapId} → {path}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            WalkabilityCacheBaker.BakeMap(mapId, path);
            sw.Stop();
            Console.Error.WriteLine($"[BenchmarkFixture] Bake complete in {sw.Elapsed.TotalSeconds:F2}s");
        }
    }

    private static bool FileMatchesLiveHash(string path, ulong liveHash) =>
        PrecomputedCacheFile.TryReadTileDataHash(path) is ulong h && h == liveHash;

    private static string ResolveWalkabilityCacheDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("MODERNUO_PATHFINDING_DATA_DIR");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return fromEnv;
        }

        // Walk up from the bench bin dir to find <repo>/Distribution/Data/Pathfinding/.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && current is not null; i++)
        {
            var candidate = Path.Combine(current.FullName, "ModernUO", "Distribution", "Data", "Pathfinding");
            if (Directory.Exists(Path.GetDirectoryName(candidate)!))
            {
                return candidate;
            }
            current = current.Parent;
        }

        // Fallback: bake into the bench bin dir if the repo layout isn't recognised.
        return Path.Combine(AppContext.BaseDirectory, "Pathfinding");
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
