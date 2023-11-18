using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Collections;

namespace UOMap;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net70)]
public class BenchmarkSectorValueLink
{
    private static Map map = new(0, 0, 0, 4096, 4096, 0, "Test Map", MapRules.FeluccaRules);

    [GlobalSetup]
    public static void Init()
    {
        for (var x = 0; x < 9; x++)
        {
            for (var y = 0; y < 9; y++)
            {
                var loc = new Point3D(x, y, 0);

                for (var i = 0; i < 10; ++i)
                {
                    var item = new Item(World.NewItem);
                    item.MoveToWorld(loc, map);
                }
            }
        }
    }

    [Benchmark]
    public IEntity SelectEntities()
    {
        var p = new Point3D();
        IEntity toRet = null;
        foreach (var item in map.GetObjectsInRange(p, 8))
        {
            toRet = item;
        }

        return toRet;
    }
    
    [Benchmark]
    public IEntity GetItemsValueLink()
    {
        var p = new Point3D();
        IEntity toRet = null;
        foreach (var item in map.GetItemsInRange(p, 8))
        {
            toRet = item;
        }

        return toRet;
    }
    
    [Benchmark]
    public IEntity GetItemsInSector()
    {
        var p = new Point3D();
        var sector = map.GetSector(p);
        
        IEntity toRet = null;
        foreach (var item in sector.Items)
        {
            toRet = item;
        }

        return toRet;
    }
}