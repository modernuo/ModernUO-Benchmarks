using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Collections;

namespace Server.Items;

public partial class TestContainer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<TestItem> FindItemsByType(bool recurse = true, Predicate<TestItem> predicate = null)
        => FindItemsByType<TestItem>(recurse, predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<T> FindItemsByType<T>(bool recurse = true, Predicate<T> predicate = null)
        where T : TestItem => new(this, recurse, predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueuedItemsEnumerator<T> EnumerateItemsByType<T>(bool recurse = true, Predicate<T> predicate = null)
        where T : TestItem => new(QueueItemsByType(recurse, predicate));

    public PooledRefQueue<T> QueueItemsByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : TestItem
    {
        var queue = PooledRefQueue<T>.Create();
        foreach (var item in FindItemsByType(recurse, predicate))
        {
            queue.Enqueue(item);
        }

        return queue;
    }

    public PooledRefList<T> ListItemsByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : TestItem
    {
        var list = PooledRefList<T>.Create();
        foreach (var item in FindItemsByType(recurse, predicate))
        {
            list.Add(item);
        }

        return list;
    }

    public ref struct FindItemsByTypeEnumerator<T> where T : TestItem
    {
        private PooledRefQueue<TestContainer> _containers;
        private Span<TestItem> _items;
        private int _index;
        private T _current;
        private bool _recurse;
        private Predicate<T> _predicate;

        public FindItemsByTypeEnumerator(TestContainer container, bool recurse, Predicate<T> predicate)
        {
            _containers = PooledRefQueue<TestContainer>.Create();

            if (container?.m_Items != null)
            {
                _items = CollectionsMarshal.AsSpan(container.m_Items);
            }

            _current = default;
            _index = 0;
            _recurse = recurse;
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => SetNextItem() || _recurse && SetNextContainer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetNextContainer()
        {
            if (!_containers.TryDequeue(out var c))
            {
                return false;
            }

            _items = CollectionsMarshal.AsSpan(c.m_Items);
            _index = 0;
            return SetNextItem();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetNextItem()
        {
            while (_index < _items.Length)
            {
                TestItem item = _items[_index++];
                if (_recurse && item is TestContainer { m_Items.Count: > 0 } c)
                {
                    _containers.Enqueue(c);
                }

                if (item is T t && _predicate?.Invoke(t) != false)
                {
                    _current = t;
                    return true;
                }
            }

            return false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _containers.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FindItemsByTypeEnumerator<T> GetEnumerator() => this;
    }

    public ref struct QueuedItemsEnumerator<T> where T : TestItem
    {
        private PooledRefQueue<T> _queue;
        private T _current;

        public QueuedItemsEnumerator(PooledRefQueue<T> queue)
        {
            _queue = queue;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_queue.TryDequeue(out var item))
            {
                _current = item;
                return true;
            }

            return false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _queue.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedItemsEnumerator<T> GetEnumerator() => this;
    }
}
