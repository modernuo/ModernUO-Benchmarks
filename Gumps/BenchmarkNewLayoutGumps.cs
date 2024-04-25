using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Gumps;
using Gumps.MockedGumps;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 40, iterationCount: 40)]
public class BenchmarkNewLayoutGumps
{
    private static readonly byte[] _packetBuffer = GC.AllocateUninitializedArray<byte>(0x10000);
    private string _petName = "a horse";
    // [Benchmark(Baseline = true)]
    // [BenchmarkCategory("DynamicLayout")]
    // public void OldAnimalFormGump()
    // {
    //     OldAnimalFormGump gump = new(100, AnimalForm.Entries);
    //     FakeSender.SendOldGump(gump, out _, out _);
    // }
    //
    // [Benchmark]
    // [BenchmarkCategory("DynamicLayout")]
    // public void NewAnimalFormGump()
    // {
    //     NewAnimalFormGump gump = new(100, AnimalForm.Entries);
    //     gump.CreateGumpPacket();
    // }
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("StaticLayout")]
    public void OldGump()
    {
        OldPetResurrectGump gump = new(_petName);
        FakeSender.SendOldGump(gump, out _, out _);
    }
    
    [Benchmark]
    [BenchmarkCategory("StaticLayout")]
    public void DynamicLayoutGump()
    {
        NewDynamicLayoutPetResurrectGump gump = new(_petName);
        var writer = new SpanWriter(_packetBuffer);
        gump.CreatePacket(ref writer);
        writer.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("StaticLayout")]
    public void StaticLayoutDynamicStringsGump()
    {
        NewPetResurrectGump gump = new(_petName);
        var writer = new SpanWriter(_packetBuffer);
        gump.CreatePacket(ref writer);
        writer.Dispose();
    }
    
    [Benchmark]
    [BenchmarkCategory("StaticLayout")]
    public void StaticLayoutGump()
    {
        NewPetStaticResurrectGump gump = new();
        var writer = new SpanWriter(_packetBuffer);
        gump.CreatePacket(ref writer);
        writer.Dispose();
    }
}