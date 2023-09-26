using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Buffers;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net70)]
public class BenchmarkLocalizationInterpolation
{
    private string _contents;
    private string _slash;
    private string _items;
    private string _stones;
    private int _totalItems;
    private int _maxItems;
    private int _totalWeight;
    private LocalizationEntry _entry;

    [GlobalSetup]
    public void Setup()
    {
        _totalItems = 50;
        _maxItems = 150;
        _totalWeight = 250;
        _contents = "Contents: ";
        _slash = "/";
        _items = " items, ";
        _stones = " stones";
        
        var arr = STArrayPool<char>.Shared.Rent(256);
        
        _entry = new LocalizationEntry(
            "enu",
            1073841,
            "Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones"
        );

        STArrayPool<char>.Shared.Return(arr);
    }

    [Benchmark]
    public string BenchmarkStringFormatter()
    {
        return string.Format(_entry.StringFormatter, _totalItems, _maxItems, _totalWeight);
    }

    [Benchmark]
    public string BenchmarkLocalization()
    {
        return _entry.Format($"{_totalItems}{_maxItems}{_totalWeight}");
    }

    [Benchmark]
    public string BenchmarkRawStringInterpolationBaseLine()
    {
        // The static part uses variables since the localization formatter effectively does the same thing
        // as part of splitting the original string.
        return $"{_contents}{_totalItems}{_slash}{_maxItems}{_items}{_totalWeight}{_stones}";
    }
}
