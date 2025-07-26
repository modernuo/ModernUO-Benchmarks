using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.PathAlgorithms.FastAStar;

namespace NPCPathing;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class BenchmarkFastAStar
{
    private static readonly Point3D _start = new(1, 1, 1);
    private static readonly Point3D _end = new(36, 36, 1);
    private static readonly FastAStar_PQueue _pathFinderPQueue = new();
    private static readonly FastAStar_NodeChain _pathFinderNodeChain = new();

    [Benchmark]
    public Direction[] FastAStar_PQueue()
    {
        return _pathFinderPQueue.Find(_start, _end);
    }

    [Benchmark]
    public Direction[] FastAStar_NodeChain()
    {
        return _pathFinderNodeChain.Find(_start, _end);
    }
}