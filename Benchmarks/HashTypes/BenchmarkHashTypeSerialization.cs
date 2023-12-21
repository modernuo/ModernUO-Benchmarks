using System;
using System.Collections.Concurrent;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkHashTypeSerialization
{
    private byte[] _bytes;
    private static Type _type;
    private BufferWriter _writer;
    private ConcurrentQueue<Type> _queue = new();
    
    [GlobalSetup]
    public void SetUp()
    {
        _bytes = new byte[256];
        _writer = new BufferWriter(_bytes, true, _queue);
        _type = typeof(Server.Engines.Harvest.BonusHarvestResource);
    }

    [Benchmark]
    public void BenchmarkXXHash()
    {
        for (var i = 0; i < 500; i++)
        {
            _writer.Write(_type);
            _writer.Seek(0, SeekOrigin.Begin);
        }
    }
    
    [Benchmark]
    public void BenchmarkTypeStrings()
    {
        for (var i = 0; i < 500; i++)
        {
            _writer.Write(_type.FullName);
            _writer.Seek(0, SeekOrigin.Begin);
        }
    }
}
