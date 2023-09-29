using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[DisassemblyDiagnoser]
[SimpleJob(RuntimeMoniker.Net70)]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkNullable
{
    private static SomeObject _someObject = new();
    private static long _someConstant = 500;
    private static long _anotherVariable;
    private static bool _someOtherValue = false;
    
    [Benchmark]
    public long BenchmarkNullableCode()
    {
        return !_someOtherValue && _someConstant - _someObject?._someValue > 0 ? 10 : 100;
    }
    
    [Benchmark]
    public long BenchmarkOldCode()
    {
        return !_someOtherValue && _someObject != null && _someConstant - _someObject._someValue > 0 ? 10 : 100;
    }

    internal class SomeObject
    {
        internal long _someValue = 100;
    }
}
