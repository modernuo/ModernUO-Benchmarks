using System;
using System.Buffers;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Gumps;
using Server;
using Server.Engines.Craft;
using Server.Engines.Craft.Tests;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 40, iterationCount: 40)]
public class BenchmarkCraftGumps
{
    private static readonly byte[] _packetBuffer = GC.AllocateUninitializedArray<byte>(0x10000);
    private static Mobile _mobile;
    private static TextDefinition _notice;

    private static void InitServer()
    {
        Core.Assembly = Assembly.GetExecutingAssembly(); // Server.Tests.dll

        // Load Configurations
        ServerConfiguration.Load(true);

        // Load an empty assembly list into the resolver
        ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
        AssemblyHandler.LoadAssemblies(["ModernUO.dll"]);

        Core.LoopContext = new EventLoopContext();
        Core.Expansion = Expansion.EJ;

        // Configure / Initialize
        TestMapDefinitions.ConfigureTestMapDefinitions();

        // Configure the world
        World.Configure();

        Timer.Init(0);

        // Load the world
        World.Load();

        World.ExitSerializationThreads();

        DefBlacksmithy.Initialize();
    }
    
    [GlobalSetup]
    public void Setup()
    {
        InitServer();
        _mobile = new Mobile((Serial)0x100);
        _mobile.DefaultMobileInit();

        _notice = "Some random notice, ok!";
    }
    
    [Benchmark(Baseline = true)]
    public void OldCraftGump()
    {
        var gump = new OldCraftGump(_mobile, DefBlacksmithy.CraftSystem, _notice);
        FakeSender.SendOldGump(gump, out _, out _);
    }
    
    [Benchmark]
    public void DynamicLayoutCraftGump()
    {
        var gump = new DynamicLayoutCraftGump(_mobile, DefBlacksmithy.CraftSystem, _notice);
        var writer = new SpanWriter(_packetBuffer);
        gump.CreatePacket(ref writer);
        writer.Dispose();
    }
}