using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Items;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkPredicates
{
    private TestContainer _container1;
    
    [GlobalSetup]
    public void Setup()
    {
        _container1 = new TestContainer();
        for (var i = 0; i < 50; i++)
        {
            _container1.m_Items.Add(new TestItem1());
            _container1.m_Items.Add(new TestItem2());
        }
        var container2 = new TestContainer();
        _container1.m_Items.Add(container2);
        
        // Nested 1-deep
        container2.m_Items.Add(new TestItem1());
        container2.m_Items.Add(new TestItem1());
        container2.m_Items.Add(new TestItem1());
        container2.m_Items.Add(new TestItem1());
        container2.m_Items.Add(new TestItem1());
        container2.m_Items.Add(new TestItem2(1));
        container2.m_Items.Add(new TestItem2());
        container2.m_Items.Add(new TestItem2());
        var container3 = new TestContainer();
        container2.m_Items.Add(container3);
        
        // Nested 2-deep
        container3.m_Items.Add(new TestItem1());
        container3.m_Items.Add(new TestItem1());
        container3.m_Items.Add(new TestItem1());
        container3.m_Items.Add(new TestItem1());
        container3.m_Items.Add(new TestItem1());
        container3.m_Items.Add(new TestItem2(1));
        
        // Nested 1-deep
        for (var i = 0; i < 50; i++)
        {
            container2.m_Items.Add(new TestItem1());
            container2.m_Items.Add(new TestItem2(1));
        }
    }
    
    [Benchmark]
    public TestItem TestFindItemsNone()
    {
        TestItem testItem = null;
        foreach (var item in _container1.FindItemsByType<TestItem2>())
        {
            if (item.Hue == 1)
            {
                testItem = item;
            }
        }

        return testItem;
    }

    private static bool IsHue1Static(TestItem2 item) => item.Hue == 1;

    [Benchmark]
    public TestItem TestFindItemsWithStatic()
    {
        TestItem testItem = null;
        foreach (var item in _container1.FindItemsByType<TestItem2>(predicate: IsHue1Static))
        {
            testItem = item;
        }

        return testItem;
    }
    
    [Benchmark]
    public TestItem TestFindItemsWithLocal()
    {
        TestItem testItem = null;
        foreach (var item in _container1.FindItemsByType<TestItem2>(predicate: IsHue1Local))
        {
            testItem = item;
        }

        return testItem;

        static bool IsHue1Local(TestItem2 item) => item.Hue == 1;
    }
    
    [Benchmark]
    public TestItem TestFindItemsWithLambda()
    {
        TestItem testItem = null;
        foreach (var item in _container1.FindItemsByType<TestItem2>(predicate: item => item.Hue == 1))
        {
            testItem = item;
        }

        return testItem;
    }
}