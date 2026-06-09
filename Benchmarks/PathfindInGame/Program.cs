using System;
using System.IO;
using BenchmarkDotNet.Running;
using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;

namespace PathfindInGame;

public static class Program
{
    public static void Main(string[] args)
    {
        // `dotnet run ... -- validate` runs a fast corpus health check (no BenchmarkDotNet
        // measurement cycle): for each scenario it runs one warm Find with the real creature
        // flags and prints whether a path was found and the cache fallthrough fraction. Use
        // this to confirm corpus coordinates produce real paths before trusting the numbers.
        if (args.Length > 0 && string.Equals(args[0], "validate", StringComparison.OrdinalIgnoreCase))
        {
            Validate();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static void Validate()
    {
        BenchmarkFixture.EnsureInitialized();

        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus", "baseline.jsonl");
        var scenarios = ScenarioCorpus.LoadJsonl(corpusPath);

        var cache = StepCache.Instance;
        cache.MissPromotionThreshold = 1; // eager build — judge the cache's best case

        Console.WriteLine($"Corpus health: {scenarios.Length} scenarios from {corpusPath}");
        Console.WriteLine("idx scenario                           result       cacheFallthrough");

        for (var i = 0; i < scenarios.Length; i++)
        {
            var s = scenarios[i];
            var map = s.ResolveMap();

            var stub = new StubCreature { CanSwim = s.CanSwim, CantWalk = false };
            stub.SetMobilityFlags(s.CanOpenDoors, s.CanMoveOverObstacles);
            stub.MoveToWorld(s.Start, map);

            for (var w = 0; w < 3; w++)
            {
                BitmapAStarAlgorithm.Instance.Find(stub, map, s.Start, s.Goal);
            }

            var before = cache.GetStats();
            var path = BitmapAStarAlgorithm.Instance.Find(stub, map, s.Start, s.Goal);
            var after = cache.GetStats();

            var served = after.Hits - before.Hits
                         + (after.MissesNotBuilt - before.MissesNotBuilt)
                         + (after.MissesDirtyRebuild - before.MissesDirtyRebuild);
            var fallthrough = after.FallthroughMultiZ - before.FallthroughMultiZ
                              + (after.FallthroughSourceZMismatch - before.FallthroughSourceZMismatch)
                              + (after.FallthroughOffMap - before.FallthroughOffMap)
                              + (after.FallthroughNotBuilt - before.FallthroughNotBuilt);
            var total = served + fallthrough;
            var pct = total == 0 ? 0 : 100.0 * fallthrough / total;

            var result = path == null ? "NO PATH" : $"{path.Length} steps";
            var flag = path == null ? "  <-- SUSPECT (bad coords/Z?)" : pct > 50 ? "  <-- high fallthrough" : "";
            Console.WriteLine($"[{i,2}] {s.Name,-34} {result,-12} {pct,5:F1}%{flag}");

            cache.ClearResidentChunks();
            stub.Delete();
        }
    }
}
