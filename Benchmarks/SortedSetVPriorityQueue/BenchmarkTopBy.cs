using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkTopBy
{
    private const int _total = 1000;
    private const int _take = 25;

    // private static Comparer<int> _comparer = Comparer<int>.Create((x, y) => y.CompareTo(x));

    private static int[] nums
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            field = Enumerable.Range(1, _total).ToArray();
            new Random(100).Shuffle(field);
            return field;
        }
    } = null;

    // [Benchmark]
    // public int TopByPriorityQueue() => nums.AsSpan().TopBy(_comparer, _take).Sum();
    //
    // [Benchmark]
    // public int TopByYield() => nums.TopByYield(_comparer, _take).Sum();

    [Benchmark]
    public int TopByOrderDescendingTake() => nums.OrderByDescending(i => i).Take(_take).Sum();
}

public static class EnumerableExtensions
{
    public static T[] GetTopBy<T>(this IEnumerable<T> source, IComparer<T> comparer, int count)
    {
        if (count <= 0) return [];

        var priorityQueue = source.ToPriorityQueue(comparer, count);

        var result = new T[priorityQueue.Count];
        var index = priorityQueue.Count - 1;
        while (priorityQueue.Count > 0)
        {
            result[index--] = priorityQueue.Dequeue();
        }
        return result;
    }

    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source, IComparer<T> comparer, int count)
    {
        if (count <= 0) yield break;

        var priorityQueue = source.ToPriorityQueue(comparer, count);

        while (priorityQueue.Count > 0)
        {
            yield return priorityQueue.Dequeue();
        }
    }

    public static PriorityQueue<T, T> ToPriorityQueue<T>(this IEnumerable<T> source, IComparer<T> comparer, int count)
    {
        var priorityQueue = new PriorityQueue<T, T>(comparer);

        foreach (var item in source)
        {
            if (priorityQueue.Count < count)
            {
                priorityQueue.Enqueue(item, item);
            }
            else if (comparer.Compare(item, priorityQueue.Peek()) < 0)
            {
                priorityQueue.DequeueEnqueue(item, item);
            }
        }

        return priorityQueue;
    }
}