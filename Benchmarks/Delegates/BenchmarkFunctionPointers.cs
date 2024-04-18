using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Runtime.CompilerServices;

namespace Benchmarks.Delegates;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 20, iterationCount: 20)]
public unsafe class BenchmarkFunctionPointers
{
    private static Func<int, int, int> _del;
    private static delegate* managed<int, int, int> _pointer;
    private int _firstNumber;
    private int _secondNumber;

    [GlobalSetup]
    public void Setup()
    {
        _del = (a, b) => a + b;
        _pointer = &Sum;

        _firstNumber = 100;
        _secondNumber = 200;
    }

    [Benchmark(Baseline = true)]
    public int StaticMethodExecute() => Sum(_firstNumber, _secondNumber);

    [Benchmark]
    public int DelegateExecute() => _del(_firstNumber, _secondNumber);

    [Benchmark]
    public int PointerExecute() => _pointer(_firstNumber, _secondNumber);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b) => a + b;
}