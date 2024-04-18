using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using static NetFabric.Hyperlinq.ArrayExtensions;

namespace Benchmarks.MultiSelectors;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class BenchmarkMapMultiSelectors
{
    private static readonly Sector sector = new();
    private static readonly Point3D[] locations = { new(0, 0, 0), new(50, 50, 0) };

    public static Rectangle2D[] BoundsArray() => new[]
    {
        new Rectangle2D(70, 70, 100, 100),
        new Rectangle2D(30, 30, 100, 100),
        new Rectangle2D(0, 0, 100, 100),
    };

    [GlobalSetup]
    public static void Init()
    {
        for (var j = 0; j < locations.Length; j++)
        {
            var loc = locations[j];

            for (var i = 0; i < 25; ++i)
            {
                sector.Multis.Add(new BaseMulti(loc));
            }
        }
    }

    [ParamsSource(nameof(BoundsArray))]
    public Rectangle2D bounds;

    [Benchmark(Baseline = true)]
    public BaseMulti SelectMultiFor()
    {
        BaseMulti toRet = null;
        for (var i = sector.Multis.Count - 1; i >= 0; --i)
        {
            var multi = sector.Multis[i];
            if (multi is { Deleted: false } tMulti && bounds.Contains(multi.Location))
            {
                toRet = tMulti;
            }
        }

        return toRet;
    }

    [Benchmark]
    public BaseMulti SelectMultiNew()
    {
        BaseMulti toRet = null;
        foreach (var m in SelectMultiNew(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    [Benchmark]
    public BaseMulti SelectMultiLinq()
    {
        BaseMulti toRet = null;
        foreach (var m in SelectMultiLinq(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    [Benchmark]
    public BaseMulti SelectMultiHyperLinq()
    {
        BaseMulti toRet = null;
        foreach (var m in SelectMultiHyperlinq(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    public IEnumerable<BaseMulti> SelectMultiLinq(Sector s, Rectangle2D bounds)
    {
        return s.Multis.Where(o => o is { Deleted: false } && bounds.Contains(o.Location));
    }

    public IEnumerable<BaseMulti> SelectMultiNew(Sector s, Rectangle2D bounds)
    {
        List<BaseMulti> entities = new(s.Multis.Count);

        for (var i = s.Multis.Count - 1; i >= 0; --i)
        {
            var multiItem = s.Multis[i];
            if (multiItem is { Deleted: false } && bounds.Contains(multiItem.Location))
            {
                entities.Add(multiItem);
            }
        }
        return entities;
    }

    public ArraySegmentWhereEnumerable<BaseMulti, MultiWhereHyper>
        SelectMultiHyperlinq(Sector s, Rectangle2D bounds)
    {
        return s.Multis.AsValueEnumerable().Where(new MultiWhereHyper(bounds));
    }
}

public class BItem(Point3D location) : IPoint3D, IEntity
{
    public object Parent { get; set; } = null;

    public bool Deleted { get; set; } = false;

    public int Z { get; set; } = 1;

    public int X { get; set; } = 1;

    public int Y { get; set; } = 1;

    public Serial Serial => throw new NotImplementedException();

    public Point3D Location { get; } = location;

    public Map Map { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Region Region => throw new NotImplementedException();

    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int Hue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public Direction Direction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DateTime Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int TypeRef => throw new NotImplementedException();

    int IPoint3D.Z => throw new NotImplementedException();

    int IPoint2D.X => throw new NotImplementedException();

    int IPoint2D.Y => throw new NotImplementedException();

    DateTime ISerializable.Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    long ISerializable.SavePosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    BufferWriter ISerializable.SaveBuffer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    Serial ISerializable.Serial => throw new NotImplementedException();

    bool ISerializable.Deleted => throw new NotImplementedException();

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public void ProcessDelta()
    {
        throw new NotImplementedException();
    }

    public void OnStatsQuery(Mobile m)
    {
        throw new NotImplementedException();
    }

    public void InvalidateProperties()
    {
        throw new NotImplementedException();
    }

    public int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(IEntity other)
    {
        throw new NotImplementedException();
    }

    public void MoveToWorld(Point3D location, Map map)
    {
        throw new NotImplementedException();
    }

    public bool InRange(Point2D p, int range)
    {
        throw new NotImplementedException();
    }

    public bool InRange(Point3D p, int range)
    {
        throw new NotImplementedException();
    }

    public void RemoveBItem(BItem BItem)
    {
        throw new NotImplementedException();
    }

    public void BeforeSerialize()
    {
        throw new NotImplementedException();
    }

    public void Deserialize(IGenericReader reader)
    {
        throw new NotImplementedException();
    }

    public void Serialize(IGenericWriter writer)
    {
        throw new NotImplementedException();
    }

    public void SetTypeRef(Type type)
    {
        throw new NotImplementedException();
    }

    void ISerializable.Deserialize(IGenericReader reader)
    {
        throw new NotImplementedException();
    }

    void ISerializable.Serialize(IGenericWriter writer)
    {
        throw new NotImplementedException();
    }

    void ISerializable.Delete()
    {
        throw new NotImplementedException();
    }

    public void RemoveItem(Item item)
    {
        throw new NotImplementedException();
    }

    public bool OnMoveOff(Mobile m)
    {
        throw new NotImplementedException();
    }

    public bool OnMoveOver(Mobile m)
    {
        throw new NotImplementedException();
    }

    public void OnMovement(Mobile m, Point3D oldLocation)
    {
        throw new NotImplementedException();
    }
}

public class BaseMulti : BItem
{
    public MultiComponentList Components = MultiComponentList.Empty;

    public BaseMulti(Point3D location) : base(location)
    {
        for (var i = 0; i < 20; ++i)
        {
            for (var j = 0; j < 20; ++j)
            {
                for (var z = 0; z < 20; ++z)
                {
                    Components.Add(123, i, j, z);
                }
            }
        }
    }
}

public class Sector
{
    public List<BaseMulti> Multis { get; set; } = new();
}

public struct MultiWhereHyper(Rectangle2D bounds) : IFunction<BaseMulti, bool>
{
    private readonly Rectangle2D bounds = bounds;

    public bool Invoke(BaseMulti element)
    {
        return element is { Deleted: false } && bounds.Contains(element.Location);
    }
}
