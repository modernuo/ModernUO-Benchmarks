using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using static NetFabric.Hyperlinq.ArrayExtensions;

namespace Benchmarks.MobileSelectors;

[SimpleJob(RuntimeMoniker.Net70)]
[MemoryDiagnoser]
public class BenchmarkMapMobileSelectors
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
                sector.Mobiles.Add(new MobileDerived(loc));
            }
        }
    }

    [ParamsSource(nameof(BoundsArray))]
    public Rectangle2D bounds;

    [Benchmark(Baseline = true)]
    public MobileDerived SelectMobilesFor()
    {
        MobileDerived toRet = null;
        for (var i = sector.Mobiles.Count - 1; i >= 0; --i)
        {
            var mob = sector.Mobiles[i];
            if (mob is MobileDerived { Deleted: false } tMob && bounds.Contains(mob.Location))
            {
                toRet = tMob;
            }
        }

        return toRet;
    }

    [Benchmark]
    public MobileDerived SelectMobilesNew()
    {
        MobileDerived toRet = null;
        foreach (var m in SelectMobiles<MobileDerived>(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    [Benchmark]
    public MobileDerived SelectMobilesLinq()
    {
        MobileDerived toRet = null;
        foreach (var m in SelectMobilesLinq<MobileDerived>(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    [Benchmark]
    public MobileDerived SelectMobilesHyperLinq()
    {
        MobileDerived toRet = null;
        foreach (var m in SelectMobilesHyperlinq<MobileDerived>(sector, bounds))
        {
            toRet = m;
        }

        return toRet;
    }

    public IEnumerable<T> SelectMobilesLinq<T>(Sector s, Rectangle2D bounds) where T : Mobile
    {
        return s.Mobiles.OfType<T>().Where(o => o is { Deleted: false } && bounds.Contains(o.Location));
    }

    public IEnumerable<T> SelectMobiles<T>(Sector s, Rectangle2D bounds) where T : Mobile
    {
        var mobiles = s.Mobiles;
        List<T> entities = new(mobiles.Count);

        for (var i = mobiles.Count - 1; i >= 0; --i)
        {
            if (mobiles[i] is T { Deleted: false } tMob && bounds.Contains(tMob.Location))
            {
                entities.Add(tMob);
            }
        }
        return entities;
    }

    public ArraySegmentWhereSelectEnumerable<Mobile, T, MobileWhereHyper<T>, SelectHyper<Mobile, T>>
        SelectMobilesHyperlinq<T>(Sector s, Rectangle2D bounds) where T : Mobile
    {
        return s.Mobiles.AsValueEnumerable()
            .Where(new MobileWhereHyper<T>(bounds))
            .Select<T, SelectHyper<Mobile, T>>();
    }
}

public class Mobile(Point3D location) : IPoint3D, IEntity
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

    int IPoint3D.Z => throw new NotImplementedException();

    int IPoint2D.X => throw new NotImplementedException();

    int IPoint2D.Y => throw new NotImplementedException();

    DateTime ISerializable.Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    long ISerializable.SavePosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    BufferWriter ISerializable.SaveBuffer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    Serial ISerializable.Serial => throw new NotImplementedException();

    bool ISerializable.Deleted => throw new NotImplementedException();

    public void MoveToWorld(Point3D location, Map map)
    {
        throw new NotImplementedException();
    }

    public void ProcessDelta()
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

    public void RemoveItem(Item item)
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

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public void SetTypeRef(Type type)
    {
        throw new NotImplementedException();
    }
}

public class MobileDerived(Point3D location) : Mobile(location);

public class Sector
{
    public List<Mobile> Mobiles { get; set; } = new();
}

public struct MobileWhereHyper<T>(Rectangle2D bounds) : IFunction<Mobile, bool>
    where T : Mobile
{
    private readonly Rectangle2D bounds = bounds;

    public bool Invoke(Mobile element)
    {
        return element is T { Deleted: false } && bounds.Contains(element.Location);
    }
}

public struct SelectHyper<TSource, TDest> : IFunction<TSource, TDest> where TDest : TSource
{
    public TDest Invoke(TSource arg)
    {
        return (TDest)arg;
    }
}
