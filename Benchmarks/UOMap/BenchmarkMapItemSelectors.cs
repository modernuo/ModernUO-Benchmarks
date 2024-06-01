using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using StructLinq;
using StructLinq.Array;
using StructLinq.List;
using StructLinq.Select;
using StructLinq.Where;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Server.Collections;
using static NetFabric.Hyperlinq.ArrayExtensions;

namespace Benchmarks.ItemSelectors;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkMapItemSelectors
{
    private static readonly Sector sector = new();
    private static readonly Point3D[] locations = { new(0, 0, 0), new(50, 50, 0) };

    public static Rectangle2D[] BoundsArray() => new[]
    {
        // new Rectangle2D(70, 70, 100, 100),
        // new Rectangle2D(30, 30, 100, 100),
        new Rectangle2D(0, 0, 100, 100)
    };

    [GlobalSetup]
    public static void Init()
    {
        for (var j = 0; j < locations.Length; j++)
        {
            var loc = locations[j];

            for (var i = 0; i < 100; ++i)
            {
                var item = new BItemDerived(loc);
                sector.BItems.Add(item);
                sector.BItemsLinkList.AddLast(item);
            }
        }
    }

    [ParamsSource(nameof(BoundsArray))]
    public Rectangle2D bounds;

    [Benchmark(Baseline = true)]
    public BItemDerived SelectBItemsFor()
    {
        BItemDerived toRet = null;
        for (var i = sector.BItems.Count - 1; i >= 0; --i)
        {
            var bItem = sector.BItems[i];
            if (bItem is BItemDerived { Deleted: false, Parent: null } tItem && bounds.Contains(bItem.Location))
            {
                toRet = tItem;
            }
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsNew()
    {
        BItemDerived toRet = null;
        foreach (var i in SelectBItems<BItemDerived>(sector, bounds))
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsLinq()
    {
        BItemDerived toRet = null;
        foreach (var i in SelectBItemsLinq<BItemDerived>(sector, bounds))
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsLinqStruct()
    {
        BItemDerived toRet = null;
        foreach (var i in SelectBItemsLinqStruct<BItemDerived>(sector, bounds))
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsLinqStructInterface()
    {
        BItemDerived toRet = null;
        IEnumerable<BItemDerived> enumerable = SelectBItemsLinqStruct<BItemDerived>(sector, bounds).ToEnumerable();

        foreach (var i in enumerable)
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsHyperLinq()
    {
        BItemDerived toRet = null;
        foreach (var i in SelectBItemsHyperlinq<BItemDerived>(sector, bounds))
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsHyperLinqInterface()
    {
        BItemDerived toRet = null;
        IEnumerable<BItemDerived> enumerable = SelectBItemsHyperlinq<BItemDerived>(sector, bounds);

        foreach (var i in enumerable)
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsHyperLinqArrayPool()
    {
        BItemDerived toRet = null;
        using var lease = SelectBItemsHyperlinq<BItemDerived>(sector, bounds).ToArray(ArrayPool<BItemDerived>.Shared);

        foreach (var i in lease)
        {
            toRet = i;
        }

        return toRet;
    }

    [Benchmark]
    public BItemDerived SelectBItemsValueLink()
    {
        BItemDerived toRet = null;

        foreach (var i in sector.BItemsLinkList)
        {
            if (i is BItemDerived { Deleted: false, Parent: null } o && bounds.Contains(i.Location))
            {
                toRet = o;
            }
        }
        
        return toRet;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> SelectBItemsLinq<T>(Sector s, Rectangle2D bounds) where T : BItem
    {
        return s.BItems.OfType<T>().Where(o => o is { Deleted: false, Parent: null } && bounds.Contains(o.Location));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> SelectBItems<T>(Sector s, Rectangle2D bounds) where T : BItem
    {
        var items = s.BItems;
        List<T> entities = new(items.Count);

        for (var i = items.Count - 1; i >= 0; --i)
        {
            if (items[i] is T { Deleted: false, Parent: null } tItem && bounds.Contains(tItem.Location))
            {
                entities.Add(tItem);
            }
        }
        return entities;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SelectEnumerable<BItem, T, WhereEnumerable<BItem, ListEnumerable<BItem>, ArrayStructEnumerator<BItem>, BItemWhere<T>>,
            WhereEnumerator<BItem, ArrayStructEnumerator<BItem>, BItemWhere<T>>, BItemSelect<T>>
        SelectBItemsLinqStruct<T>(Sector s, Rectangle2D bounds) where T : BItem
    {
        BItemWhere<T> bitemWhere = new(bounds);
        BItemSelect<T> bitemSelect = new();

        return s.BItems.ToStructEnumerable()
            .Where(ref bitemWhere, x => x)
            .Select(ref bitemSelect, x => x, x => x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegmentWhereSelectEnumerable<BItem, T, BItemWhereHyper<T>, SelectHyper<BItem, T>>
        SelectBItemsHyperlinq<T>(Sector s, Rectangle2D bounds) where T : BItem
    {
        return s.BItems.AsValueEnumerable()
            .Where(new BItemWhereHyper<T>(bounds))
            .Select<T, SelectHyper<BItem, T>>();
    }
}

public class BItem(Point3D location) : IEntity, IValueLinkListNode<BItem>
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

    Point3D IEntity.Location => Location;

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

    public BItem Next { get; set; }
    public BItem Previous { get; set; }
    public bool OnLinkList { get; set; }
}

public class BItemDerived(Point3D location) : BItem(location);

public class Sector
{
    public List<BItem> BItems { get; set; } = new();
    private ValueLinkList<BItem> _linkList;

    public ref ValueLinkList<BItem> BItemsLinkList => ref _linkList;
}

public struct BItemWhere<T>(Rectangle2D bounds) : StructLinq.IFunction<BItem, bool>
    where T : BItem
{
    private readonly Rectangle2D bounds = bounds;

    public bool Eval(BItem element)
    {
        return element is T { Deleted: false, Parent: null } && bounds.Contains(element.Location);
    }
}

public struct BItemSelect<T> : StructLinq.IFunction<BItem, T> where T : BItem
{
    public T Eval(BItem element)
    {
        return (T)element;
    }
}

public struct BItemWhereHyper<T>(Rectangle2D bounds) : NetFabric.Hyperlinq.IFunction<BItem, bool>
    where T : BItem
{
    private readonly Rectangle2D bounds = bounds;

    public bool Invoke(BItem element)
    {
        return element is T { Deleted: false, Parent: null } && bounds.Contains(element.Location);
    }
}

public struct SelectHyper<TSource, TDest> : NetFabric.Hyperlinq.IFunction<TSource, TDest> where TDest : TSource
{
    public TDest Invoke(TSource arg)
    {
        return (TDest)arg;
    }
}
