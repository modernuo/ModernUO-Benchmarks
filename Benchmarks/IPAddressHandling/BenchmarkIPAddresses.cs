using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkIPAddresses
{
    private IPAddress _address1;
    private IPAddress _address2;

    [GlobalSetup]
    public void Setup()
    {
        // Worst case scenario is when the addresses are equal
        _address1 = IPAddress.Parse("3729:0b01:9949:2eef:28c8:fee7:43de:f37e");
        _address2 = IPAddress.Parse("3729:0b01:9949:2eef:28c8:fee7:43de:f37e");
    }

    [Benchmark]
    public int BenchmarkCompareNetworkBytes()
    {
        var x = _address1;
        var y = _address2;

        var a = x.GetAddressBytes();
        var b = y.GetAddressBytes();

        for (var i = 0; i < a.Length && i < b.Length; i++)
        {
            var compare = a[i].CompareTo(b[i]);

            if (compare != 0)
            {
                return compare;
            }
        }

        return 0;
    }

    [Benchmark]
    public int BenchmarkUInt128()
    {
        return _address1.ToUInt128().CompareTo(_address2.ToUInt128());
    }
}