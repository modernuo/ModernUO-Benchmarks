using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarks.EntitiesSelectors;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class BenchmarkMapEntitiesSelectors
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

            for (var i = 0; i < 500; ++i)
            {
                sector.BItems.Add(new BItem(loc));
            }

            for (var i = 0; i < 25; ++i)
            {
                sector.Mobiles.Add(new Mobile(loc));
            }
        }
    }

    [ParamsSource(nameof(BoundsArray))]
    public Rectangle2D bounds;

    [Benchmark(Baseline = true)]
    public IEntity SelectEntitiesFor()
    {
        IEntity toRet = null;
        for (var i = sector.Mobiles.Count - 1; i >= 0; --i)
        {
            var mob = sector.Mobiles[i];
            if (mob is { Deleted: false } tMob && bounds.Contains(mob.Location))
            {
                toRet = tMob;
            }
        }

        for (var i = sector.BItems.Count - 1; i >= 0; --i)
        {
            var item = sector.BItems[i];
            if (item is { Deleted: false, Parent: null } tItem && bounds.Contains(item.Location))
            {
                toRet = tItem;
            }
        }

        return toRet;
    }

    [Benchmark]
    public IEntity SelectEntitiesNew()
    {
        IEntity toRet = null;
        foreach (var e in SelectEntitiesNew(sector, bounds))
        {
            toRet = e;
        }

        return toRet;
    }

    [Benchmark]
    public IEntity SelectEntitiesLinq()
    {
        IEntity toRet = null;
        foreach (var e in SelectEntitiesLinq(sector, bounds))
        {
            toRet = e;
        }

        return toRet;
    }

    [Benchmark]
    public IEntity SelectMobilesHyperLinq()
    {
        IEntity toRet = null;
        foreach (var e in SelectEntitiesHyperlinq(sector, bounds))
        {
            toRet = e;
        }

        return toRet;
    }


    public IEnumerable<IEntity> SelectEntitiesLinq(Sector s, Rectangle2D bounds)
    {
        return Enumerable.Empty<IEntity>()
            .Union(s.Mobiles.Where(o => o is { Deleted: false } && bounds.Contains(o.Location)))
            .Union(s.BItems.Where(o => o is { Deleted: false, Parent: null } && bounds.Contains(o.Location)));
    }

    private readonly List<IEntity> entities = new(10);

    public IEnumerable<IEntity> SelectEntitiesNew(Sector s, Rectangle2D bounds)
    {
        entities.Clear();
        entities.EnsureCapacity(s.Mobiles.Count + s.BItems.Count);

        for (int i = s.Mobiles.Count - 1, j = s.BItems.Count - 1; i >= 0 || j >= 0; --i, --j)
        {
            if (j >= 0)
            {
                var BItem = s.BItems[j];
                if (BItem is { Deleted: false, Parent: null } && bounds.Contains(BItem.Location))
                {
                    entities.Add(BItem);
                }
            }
            if (i >= 0)
            {
                var mob = s.Mobiles[i];
                if (mob is { Deleted: false } && bounds.Contains(mob.Location))
                {
                    entities.Add(mob);
                }
            }
        }
        return entities;
    }

    public IEnumerable<IEntity> SelectEntitiesHyperlinq(Sector s, Rectangle2D bounds)
    {
        var mobiles =
            s.Mobiles.AsValueEnumerable().Where(new MobileWhereHyper(bounds)).Select<IEntity, SelectHyper<Mobile, IEntity>>();

        var items =
            s.BItems.AsValueEnumerable().Where(new BItemWhereHyper(bounds)).Select<IEntity, SelectHyper<BItem, IEntity>>();

        return mobiles.Concat(items);
    }
}

public class BItem(Point3D location) : IEntity
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

    Point3D IEntity.Location
    {
        get => Location;
        set => throw new NotImplementedException();
    }

    Map IEntity.Map => throw new NotImplementedException();

    int IPoint3D.Z => throw new NotImplementedException();

    int IPoint2D.X => throw new NotImplementedException();

    int IPoint2D.Y => throw new NotImplementedException();

    DateTime ISerializable.Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

    public void OnStatsQuery(Server.Mobile m)
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

    public byte SerializedThread { get; set; }
    public int SerializedPosition { get; set; }
    public int SerializedLength { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        throw new NotImplementedException();
    }

    public void SetTypeRef(Type type)
    {
        throw new NotImplementedException();
    }

    void IEntity.MoveToWorld(Point3D location, Map map)
    {
        throw new NotImplementedException();
    }

    void IEntity.ProcessDelta()
    {
        throw new NotImplementedException();
    }

    bool IEntity.InRange(Point2D p, int range)
    {
        throw new NotImplementedException();
    }

    bool IEntity.InRange(Point3D p, int range)
    {
        throw new NotImplementedException();
    }

    void ISerializable.Deserialize(IGenericReader reader)
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

    public bool OnMoveOff(Server.Mobile m)
    {
        throw new NotImplementedException();
    }

    public bool OnMoveOver(Server.Mobile m)
    {
        throw new NotImplementedException();
    }

    public void OnMovement(Server.Mobile m, Point3D oldLocation)
    {
        throw new NotImplementedException();
    }
}

public class Mobile(Point3D location) : IEntity
{
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

    Point3D IEntity.Location
    {
        get => Location;
        set => throw new NotImplementedException();
    }

    Map IEntity.Map => throw new NotImplementedException();

    int IPoint3D.Z => throw new NotImplementedException();

    int IPoint2D.X => throw new NotImplementedException();

    int IPoint2D.Y => throw new NotImplementedException();

    DateTime ISerializable.Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

    public void OnStatsQuery(Server.Mobile m)
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

    public byte SerializedThread { get; set; }
    public int SerializedPosition { get; set; }
    public int SerializedLength { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        throw new NotImplementedException();
    }

    public void SetTypeRef(Type type)
    {
        throw new NotImplementedException();
    }

    void IEntity.MoveToWorld(Point3D location, Map map)
    {
        throw new NotImplementedException();
    }

    void IEntity.ProcessDelta()
    {
        throw new NotImplementedException();
    }

    bool IEntity.InRange(Point2D p, int range)
    {
        throw new NotImplementedException();
    }

    bool IEntity.InRange(Point3D p, int range)
    {
        throw new NotImplementedException();
    }

    void ISerializable.Deserialize(IGenericReader reader)
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

    public bool OnMoveOff(Server.Mobile m)
    {
        throw new NotImplementedException();
    }

    public bool OnMoveOver(Server.Mobile m)
    {
        throw new NotImplementedException();
    }

    public void OnMovement(Server.Mobile m, Point3D oldLocation)
    {
        throw new NotImplementedException();
    }
}

public class Sector
{
    public List<BItem> BItems { get; set; } = new();
    public List<Mobile> Mobiles { get; set; } = new();
}

public struct BItemWhereHyper(Rectangle2D bounds) : IFunction<BItem, bool>
{
    private readonly Rectangle2D bounds = bounds;

    public bool Invoke(BItem element)
    {
        return element is { Deleted: false, Parent: null } && bounds.Contains(element.Location);
    }
}

public struct MobileWhereHyper(Rectangle2D bounds) : IFunction<Mobile, bool>
{
    private readonly Rectangle2D bounds = bounds;

    public bool Invoke(Mobile element)
    {
        return element is { Deleted: false } && bounds.Contains(element.Location);
    }
}

public struct SelectHyper<TSource, TDest> : IFunction<TSource, TDest> where TSource : TDest
{
    public TDest Invoke(TSource arg)
    {
        return arg;
    }
}
