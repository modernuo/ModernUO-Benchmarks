using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Server.Tests;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TestWhileLoop
{
    private CancellationToken _token;

    [GlobalSetup]
    public void Setup() => _token = new();

    [Benchmark]
    public void TestWhileTryCatch()
    {
        var i = 0;
        while (++i < 50000)
        {
            try
            {
                SomeMethodThatCanThrow(_token);
            }
            catch
            {
                Console.WriteLine("We threw an exception");
            }
        }
    }

    [Benchmark]
    public void TestTryCatchWhile()
    {
        while (true)
        {
            try
            {
                var i = 0;
                while (++i < 50000)
                {
                    SomeMethodThatCanThrow(_token);
                }

                return;
            }
            catch
            {
                Console.WriteLine("We threw an exception");
            }
        }
    }

    public static void SomeMethodThatCanThrow(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            throw new Exception("This threw");
        }
    }
}