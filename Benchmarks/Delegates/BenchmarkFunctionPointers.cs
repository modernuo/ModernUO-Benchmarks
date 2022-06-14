using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.Delegates;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60, warmupCount: 20, targetCount: 20)]
public unsafe class BenchmarkFunctionPointers
{
    public Func<int, int, int> @delegate;
    public delegate* managed<int, int, int> pointer;
    public int firstNumber;
    public int secondNumber;

    [GlobalSetup]
    public void Setup()
    {
        @delegate = (a, b) => a + b;
        pointer = &Sum;

        firstNumber = 100;
        secondNumber = 200;
    }

    [Benchmark(Baseline = true)]
    public int StaticMethodExecute()
    {
        return Sum(firstNumber, secondNumber);
    }

    [Benchmark]
    public int DelegateExecute()
    {
        return @delegate(firstNumber, secondNumber);
    }

    [Benchmark]
    public int PointerExecute()
    {
        return pointer(firstNumber, secondNumber);
    }

    public static int Sum(int a, int b)
    {
        return a + b;
    }
}