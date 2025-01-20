using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarks.MultiTilesSelectors;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class BenchmarkMapMultiTilesSelectors
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

    [Benchmark]
    public int SelectMultiTilesNew()
    {
        var toRet = 0;

        foreach (var tiles in SelectMultiTilesNew(sector, bounds))
        {
            for (var i = 0; i < tiles.Length; ++i)
            {
                toRet = tiles[i].ID;
            }
        }

        return toRet;
    }

    [Benchmark(Baseline = true)]
    public int SelectMultiTilesLinq()
    {
        var toRet = 0;

        foreach (var tiles in SelectMultiTilesLinq(sector, bounds))
        {
            for (var i = 0; i < tiles.Length; ++i)
            {
                toRet = tiles[i].ID;
            }
        }

        return toRet;
    }

    public IEnumerable<StaticTile[]> SelectMultiTilesLinq(Sector s, Rectangle2D bounds)
    {
        foreach (var o in s.Multis.Where(o => o != null && !o.Deleted))
        {
            var c = o.Components;

            int x, y, xo, yo;
            StaticTile[] t, r;

            for (x = bounds.Start.X; x < bounds.End.X; x++)
            {
                xo = x - (o.X + c.Min.X);

                if (xo < 0 || xo >= c.Width)
                {
                    continue;
                }

                for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                {
                    yo = y - (o.Y + c.Min.Y);

                    if (yo < 0 || yo >= c.Height)
                    {
                        continue;
                    }

                    t = c.Tiles[xo][yo];

                    if (t.Length <= 0)
                    {
                        continue;
                    }

                    r = new StaticTile[t.Length];

                    for (var i = 0; i < t.Length; i++)
                    {
                        r[i] = t[i];
                        r[i].Z += o.Z;
                    }

                    yield return r;
                }
            }
        }
    }

    public IEnumerable<StaticTile[]> SelectMultiTilesNew(Sector s, Rectangle2D bounds)
    {
        var multis = s.Multis;

        for (var l = multis.Count - 1; l >= 0; --l)
        {
            if (multis[l] is not { Deleted: false } o)
            {
                continue;
            }

            var c = o.Components;

            int x, y, xo, yo;
            StaticTile[] t, r;

            for (x = bounds.Start.X; x < bounds.End.X; x++)
            {
                xo = x - (o.X + c.Min.X);

                if (xo < 0 || xo >= c.Width)
                {
                    continue;
                }

                for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                {
                    yo = y - (o.Y + c.Min.Y);

                    if (yo < 0 || yo >= c.Height)
                    {
                        continue;
                    }

                    t = c.Tiles[xo][yo];

                    if (t.Length <= 0)
                    {
                        continue;
                    }

                    r = new StaticTile[t.Length];

                    for (var i = 0; i < t.Length; i++)
                    {
                        r[i] = t[i];
                        r[i].Z += o.Z;
                    }

                    yield return r;
                }
            }
        }
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
