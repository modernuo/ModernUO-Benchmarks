using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Gumps;
using Gumps.MockedGumps;
using Server.Spells.Ninjitsu;

namespace Benchmarks.Delegates;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 20, iterationCount: 20)]
public unsafe class BenchmarkNewDynamicGump
{
    [Benchmark(Baseline = true)]
    public void OldGump()
    {
        OldAnimalFormGump gump = new(100, AnimalForm.Entries);
        FakeSender.SendOldGump(gump, out _, out _);
    }

    [Benchmark]
    public void NewDynamicGump()
    {
        NewAnimalFormGump gump = new(100, AnimalForm.Entries);
        FakeSender.SendNewGump(gump);
    }
}