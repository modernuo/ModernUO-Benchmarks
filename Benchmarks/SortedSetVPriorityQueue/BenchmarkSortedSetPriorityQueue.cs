using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkSortedSetPriorityQueue
{
    private const int NumItems = 10000;
    private List<(int Item, int Priority)> _items;

    [GlobalSetup]
    public void Setup()
    {
        // Populate items in descending order (worst-case for SortedSet insertion)
        _items = Enumerable.Range(0, NumItems)
            .Select(i => (Item: i, Priority: NumItems - i))
            .ToList();
    }

    [Benchmark]
    public void PriorityQueue_Enqueue_Dequeue()
    {
        var pq = new PriorityQueue<int, int>(10000);

        var span = CollectionsMarshal.AsSpan(_items);
        for (var i = 0; i < span.Length; i++)
        {
            var (item, priority) = span[i];
            pq.Enqueue(item, priority);
        }

        while (pq.Count > 0)
        {
            var peeked = pq.Peek();
            var dequeued = pq.Dequeue();
        }
    }

    [Benchmark]
    public void SortedSet_Add_RemoveMin()
    {
        var set = new SortedSet<(int Priority, int Item)>(
            Comparer<(int Priority, int Item)>.Create((a, b) =>
            {
                int cmp = a.Priority.CompareTo(b.Priority);
                return cmp != 0 ? cmp : a.Item.CompareTo(b.Item); // Avoid duplicates
            }));

        var span = CollectionsMarshal.AsSpan(_items);
        for (var i = 0; i < span.Length; i++)
        {
            var (item, priority) = span[i];
            set.Add((priority, item));
        }

        while (set.Count > 0)
        {
            var min = set.Min;
            set.Remove(min);
        }
    }
}