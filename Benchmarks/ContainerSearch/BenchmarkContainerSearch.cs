using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Items;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkContainerSearch
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
        container2.m_Items.Add(new TestItem2());
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
        container3.m_Items.Add(new TestItem2());
        
        // Nested 1-deep
        for (var i = 0; i < 50; i++)
        {
            container2.m_Items.Add(new TestItem1());
            container2.m_Items.Add(new TestItem2());
        }
    }
    
    [Benchmark]
    public bool FindItemsByTypeNew()
    {
        TestItem item = null;
        foreach (var i in _container1.FindItemsByType<TestItem2>()) // BFS
        {
            item = i;
        }

        return item != null;
    }
    
    [Benchmark]
    public bool FindItemsByTypeRunUO()
    {
        TestItem item = null;
        foreach (var i in _container1.FindItemsByTypeRunUO<TestItem2>()) // DFS
        {
            item = i;
        }

        return item != null;
    }
    
    [Benchmark]
    public bool FindItemsByTypeMUOOld()
    {
        TestItem item = null;
        foreach (var i in _container1.FindItemsByTypeMUOOld<TestItem2>()) // BFS
        {
            item = i;
        }

        return item != null;
    }
}
