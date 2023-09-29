using System;
using System.Collections.Generic;
using Server.Collections;

namespace Server.Items;

public partial class TestContainer
{
    public List<T> FindItemsByTypeRunUO<T>(bool recurse = true, Predicate<T> predicate = null) where T : TestItem
    {
        List<T> list = new List<T>();

        RecurseFindItemsByType(this, recurse, list, predicate);

        return list;
    }

    private static void RecurseFindItemsByType<T>(TestItem current, bool recurse, List<T> list, Predicate<T> predicate)
        where T : TestItem
    {
        if (current != null && current.m_Items.Count > 0)
        {
            List<TestItem> items = current.m_Items;

            for (int i = 0; i < items.Count; ++i)
            {
                TestItem item = items[i];

                if (item is T typedItem)
                {
                    if (predicate == null || predicate(typedItem))
                        list.Add(typedItem);
                }

                if (recurse && item is TestContainer)
                    RecurseFindItemsByType(item, true, list, predicate);
            }
        }
    }
    
    public List<T> FindItemsByTypeMUOOld<T>(bool recurse = true, Predicate<T> predicate = null) where T : TestItem
    {
        using var queue = PooledRefQueue<TestContainer>.Create(128);
        queue.Enqueue(this);
        var items = new List<T>();
        while (queue.Count > 0)
        {
            var container = queue.Dequeue();
            foreach (var item in container.m_Items)
            {
                if (item is T typedItem && predicate?.Invoke(typedItem) != false)
                {
                    items.Add(typedItem);
                }
                else if (recurse && item is TestContainer itemContainer)
                {
                    queue.Enqueue(itemContainer);
                }
            }
        }

        return items;
    }
}