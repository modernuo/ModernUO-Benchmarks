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
        // server (Server/TileData.cs ~line 295). Force-load via reflection so
        // LandTable / ItemTable flags populate; otherwise every tile reads as flag=None
        // and the algorithm treats everything as walkable (meaningless 86ns paths).
        ForceLoadTileData();

        EnsureBakedFiles();

        _initialized = true;
    }

    /// <summary>
    /// Make sure each map referenced by the bench corpus has a fresh, valid
    /// &lt;mapId&gt;.swb file before the harness starts measuring. Calls
    /// <see cref="StepCache.BakeMap"/> in-process if the file is missing or stale.
    /// Single source of truth — no separate bake program.
    /// </summary>
    /// <remarks>
    /// Freshness is decided by trying to open the file: <see cref="StepCache.TryOpenLazyReader"/>
    /// goes through <c>StepCacheFile.OpenForLazy</c>, which validates the embedded TileData
    /// fingerprint and rejects a missing / stale / malformed file (returns false). So "can we
    /// open it?" IS the freshness check — there is no separate fingerprint compare (the old
    /// <c>ComputeLiveFingerprint</c>/<c>TryReadFingerprintFromFile</c> pair was removed by the
    /// file-fingerprint rework in #2478). Mirrors the live boot path
    /// (<c>PathCacheCommands.Initialize</c>: <c>HasLazyReader</c> else <c>BakeMap</c> then
    /// <c>AutoLoadAtStartup</c>).
    /// </remarks>
    private static void EnsureBakedFiles()
    {
        var dir = ResolvePathfindingDataDir();
        Directory.CreateDirectory(dir);

        // Bench corpus is currently Trammel-only; expand the set as new map scenarios land.
        var mapsToBake = new HashSet<int> { 1 };

        foreach (var mapId in mapsToBake)
        {
            var path = Path.Combine(dir, $"{mapId}.swb");

            // A fingerprint-valid file opens cleanly → nothing to bake.
            if (StepCache.Instance.TryOpenLazyReader(path, mapId))
            {
                continue;
            }

            Console.Error.WriteLine($"[BenchmarkFixture] Baking step cache for map {mapId} → {path}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var written = StepCache.Instance.BakeMap(mapId, path);
            sw.Stop();
            Console.Error.WriteLine(
                $"[BenchmarkFixture] Bake complete in {sw.Elapsed.TotalSeconds:F2}s ({written} chunks)"
            );

            StepCache.Instance.ClearResidentChunks();
        }

        // Leave a clean slate: drop resident chunks and close the freshness-probe readers.
        // The harness's GlobalSetup opens its own lazy readers per provider.
        StepCache.Instance.Clear();
    }

    /// <summary>
    /// Resolves the directory containing &lt;mapId&gt;.swb files. Honors
    /// <c>MODERNUO_PATHFINDING_DATA_DIR</c>; otherwise walks upward from
    /// <see cref="AppContext.BaseDirectory"/> looking for
    /// <c>ModernUO/Distribution/Data/Pathfinding/</c>. Public so the harness can
    /// reuse it when registering lazy readers.
    /// </summary>
    public static string ResolvePathfindingDataDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("MODERNUO_PATHFINDING_DATA_DIR");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return fromEnv;
        }

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
