using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Server.Tests;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class FirstVsForBenchmark
{
    private List<Item> _items;
    private int _targetId;

    [GlobalSetup]
    public void Setup()
    {
        const int max = 4;
        _items = Enumerable.Range(1, max)
            .Select(i => new Item { Id = i, Name = $"Item{i}" })
            .ToList();
        _targetId = max - 1;
    }

    [Benchmark]
    public Item LinqFirst()
    {
        return _items.First(x => x.Id == _targetId);
    }

    [Benchmark]
    public Item ForLoop()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Id == _targetId)
                return _items[i];
        }
        return null;
    }

    [Benchmark]
    public Item SpanForLoop()
    {
        var itemsSpan = CollectionsMarshal.AsSpan(_items);
        for (var i = 0; i < itemsSpan.Length; i++)
        {
            if (itemsSpan[i].Id == _targetId)
                return itemsSpan[i];
        }
        return null;
    }
}