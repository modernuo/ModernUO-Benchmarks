using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Runtime.CompilerServices;

namespace Benchmarks.Delegates;

[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 40, iterationCount: 40)]
public unsafe class BenchmarkFunctionPointers
{
    private static Func<int, int, int> _del;
    private static delegate*<int, int, int> _pointer;
    private int _firstNumber;
    private int _secondNumber;

    [GlobalSetup]
    public void Setup()
    {
        _del = [MethodImpl(MethodImplOptions.NoInlining)](a, b) => a + b;
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
    private static int Sum(int a, int b) => a + b;
}