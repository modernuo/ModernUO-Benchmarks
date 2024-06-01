using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ClosedFormulas;

[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkClosedFormulas
{
    private static int _count = new Random().Next(10, 12);

    [Benchmark]
    public int BenchmarkLoop()
    {
        var count = _count;
        var maxCount = count;
        var hitDelay = 5;
        var length = hitDelay;

        while (--count >= 0)
        {
            if (hitDelay > 1)
            {
                if (maxCount < 5)
                {
                    --hitDelay;
                }
                else
                {
                    hitDelay = Math.Min((int)Math.Ceiling((1.0 + 5 * count) / maxCount), 5);
                }
            }

            length += hitDelay;
        }

        return length;
    }

    [Benchmark]
    public int BenchmarkClosedFormula()
    {
        // c is never below 4 per your statement
        return 3 * _count + (_count % 5 == 0 ? 5 : 3);
    }
}