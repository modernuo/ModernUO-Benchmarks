using System;
using System.Collections.Concurrent;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.Delegates;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkConcurrentQueueDelegates
{
    private static ConcurrentQueue<Action> _queue = [];
    private static event Action EventQueue;

    private static int _number;
    private static int _index;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < 10000000; i++)
        {
            var index = i;
            _queue.Enqueue(() => _number += index);
        }

        for (var i = 0; i < 100; i++)
        {
            var index = i;
            EventQueue += () => _number += index;
        }
    }

    [Benchmark]
    public void BenchmarkConcurrentQueue()
    {
        for (var i = 0; i < 100; i++)
        {
            _queue.TryDequeue(out var action);
            action();
        }
    }

    [Benchmark]
    public void BenchmarkEventQueue()
    {
        var actions = Interlocked.Exchange(ref EventQueue, null);
        actions?.Invoke();
        Interlocked.Exchange(ref EventQueue, actions);
    }
}