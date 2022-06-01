using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class BenchmarkLocalizationInterpolation
{
    private string contents;
    private string slash;
    private string items;
    private string stones;
    private int totalItems;
    private int maxItems;
    private int totalWeight;

    [GlobalSetup]
    public void Setup()
    {
        totalItems = 50;
        maxItems = 150;
        totalWeight = 250;
        contents = "Contents: ";
        slash = "/";
        items = " items, ";
        stones = " stones";
        
        var arr = ArrayPool<char>.Shared.Rent(256);
        Localization.Initialize();
        
        ArrayPool<char>.Shared.Return(arr);
    }

    [Benchmark]
    public string BenchmarkStringFormatter()
    {
        if (Localization.TryGetLocalization(1073841, out var entry))
        {
            return string.Format(entry.Formatter, totalItems, maxItems, totalWeight);
        }
    
        throw new Exception("Don't go there!");
    }

    [Benchmark]
    public string BenchmarkLocalization()
    {
        return Localization.Formatted(1073841, $"{totalItems}{maxItems}{totalWeight}");
    }

    [Benchmark]
    public string BenchmarkRawStringInterpolationBaseLine()
    {
        if (Localization.TryGetLocalization(1073841, out var entry))
        {
            return $"{contents}{totalItems}{slash}{maxItems}{items}{totalWeight}{stones}";
        }
        
        throw new Exception("Don't go there!");
    }
}
