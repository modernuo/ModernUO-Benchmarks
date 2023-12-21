using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Gumps.MockedGumps;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 40, iterationCount: 40)]
public class BenchmarkNewLayoutGumps
{
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
    
    // [Benchmark(Baseline = true)]
    // [BenchmarkCategory("StaticLayout")]
    // public void OldGump()
    // {
    //     OldPetResurrectGump gump = new(_petName);
    //     FakeSender.SendOldGump(gump, out _, out _);
    // }
    
    // [Benchmark]
    // [BenchmarkCategory("StaticLayout")]
    // public void DynamicLayoutGump()
    // {
    //     NewDynamicLayoutPetResurrectGump gump = new(_petName);
    //     gump.CreatePacket();
    // }

    [Benchmark]
    [BenchmarkCategory("StaticLayout")]
    public void StaticLayoutDynamicStringsGump()
    {
        NewPetResurrectGump gump = new(_petName);
        gump.CreatePacket();
    }
    
    // [Benchmark]
    // [BenchmarkCategory("StaticLayout")]
    // public void StaticLayoutGump()
    // {
    //     NewPetStaticResurrectGump gump = new();
    //     gump.CreatePacket();
    // }
}