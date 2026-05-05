using BenchmarkDotNet.Attributes;
using Server;
using Server.Engines.Pathing.Cache;
using Cache = Server.Engines.Pathing.Cache.StaticWalkabilityCache;

namespace StaticWalkabilityCacheBench;

[MemoryDiagnoser]
public class CacheBenchmarks
{
    private Map _map;
    private Mobile _stub;

    [GlobalSetup]
    public void Setup()
    {
        CacheBenchmarkFixture.EnsureInitialized();
        _map = Map.Maps[1];
        _stub = new BenchStubMobile();
        _stub.MoveToWorld(new Point3D(1500, 1600, 10), _map);
    }

    [IterationSetup(Target = nameof(CacheHit_Cold))]
    public void IterationSetup_Cold()
    {
        Cache.Instance.Clear();
    }

    [Benchmark]
    public byte CacheHit_Cold()
    {
        Cache.Instance.TryGetMask(
            _map, 1500, 1600, 10,
            out var mask, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        return mask;
    }

    [Benchmark]
    public byte CacheHit_Warm()
    {
        Cache.Instance.TryGetMask(
            _map, 1500, 1600, 10,
            out var mask, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        return mask;
    }

    [Benchmark]
    public byte Baker_Direct()
    {
        var r = StaticWalkabilityBaker.ComputeMaskAt(_map, 1500, 1600, 10);
        return r.Mask;
    }

    [Benchmark]
    public bool MovementImpl_Direct()
    {
        return Server.Movement.Movement.CheckMovement(_stub, _map, new Point3D(1500, 1600, 10), Direction.North, out _);
    }

    [Benchmark]
    public bool Cache_FullRoundTrip()
    {
        return CachedMovementCheck.TryCheck(
            _stub, _map, new Point3D(1500, 1600, 10), Direction.North,
            out _, out _);
    }

    private sealed class BenchStubMobile : Mobile
    {
        public BenchStubMobile() { Body = 0xC9; }
    }
}
