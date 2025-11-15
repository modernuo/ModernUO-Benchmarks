using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Jobs;

public class ObjectForCWT
{
    // Other data to make the object somewhat realistic
    public long DataField1;
    public string DataField2;
}

public class StateMachine
{
    public long TimeDebt;
    public long LastDecayTime;
    public long LastMovementTime;
}

public class ObjectWithDirectProperties
{
    // Other data
    public long DataField1;
    public string DataField2;

    // State machine properties
    public long TimeDebt;
    public long LastDecayTime;
    public long LastMovementTime;
}

[SimpleJob(RuntimeMoniker.Net10_0)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BenchmarkConditionalWeakTable
{
    private static readonly ConditionalWeakTable<ObjectForCWT, StateMachine> _cwt = new();
    private List<ObjectForCWT> _cwtObjects;
    private List<ObjectWithDirectProperties> _directObjects;

    [Params(10, 100, 1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        _cwtObjects = new List<ObjectForCWT>(N);
        _directObjects = new List<ObjectWithDirectProperties>(N);

        for (int i = 0; i < N; i++)
        {
            var cwtObj = new ObjectForCWT { DataField1 = i, DataField2 = $"Object {i}" };
            var state = new StateMachine { TimeDebt = i, LastDecayTime = i - N, LastMovementTime = i * -N };

            _cwtObjects.Add(cwtObj);
            _cwt.Add(cwtObj, state);

            _directObjects.Add(new ObjectWithDirectProperties
            {
                DataField1 = i,
                DataField2 = $"Object {i}",
                TimeDebt = i,
                LastDecayTime = i - N,
                LastMovementTime = i * -N
            });
        }
    }

    [Benchmark]
    public void ConditionalWeakTable_Access()
    {
        long totalState = 0;
        var span = CollectionsMarshal.AsSpan(_cwtObjects);
        for (var i = 0; i < span.Length; i++)
        {
            var obj = span[i];
            // The core operation: TryGetValue
            if (_cwt.TryGetValue(obj, out var state))
            {
                // Perform a simple read/write
                totalState += state!.TimeDebt++;
            }
        }

        // Use the result to prevent dead code elimination
        if (totalState == -1) throw new Exception();
    }

    [Benchmark(Baseline = true)]
    public void DirectProperty_Access()
    {
        long totalState = 0;
        var span = CollectionsMarshal.AsSpan(_directObjects);
        for (var i = 0; i < span.Length; i++)
        {
            var obj = span[i];
            // The core operation: direct access
            totalState += obj.TimeDebt++;
        }

        // Use the result to prevent dead code elimination
        if (totalState == -1) throw new Exception();
    }
}