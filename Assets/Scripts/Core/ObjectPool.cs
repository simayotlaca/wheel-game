using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
internal class ObjectPool<T> where T : Component
{
    private T prefab;
    private Transform parent;
    private List<T> pool = new List<T>();
    private List<T> active_items = new List<T>();

    internal int ActiveCount
    {
        get
        {
            return active_items.Count;
        }
    }

    internal ObjectPool(T prefab, Transform parent, int init_cap)
    {
        this.prefab = prefab;
        this.parent = parent;

        EnsureCapacity(init_cap);
    }

    internal void EnsureCapacity(int needed)
    {
        while (pool.Count < needed)
        {
            CreateNew();
        }
    }

    private T CreateNew()
    {
        T obj = Object.Instantiate(prefab, parent, false);
        obj.gameObject.SetActive(false);
        pool.Add(obj);
        return obj;
    }

    internal T Acquire()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            T item = pool[i];
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                active_items.Add(item);
                return item;
            }
        }

        T obj = CreateNew();
        obj.gameObject.SetActive(true);
        active_items.Add(obj);
        return obj;
    }

    internal T GetActive(int index)
    {
        return active_items[index];
    }

    internal void ReleaseAll()
    {
        for (int i = 0; i < active_items.Count; i++)
        {
            T item = active_items[i];
            item.gameObject.SetActive(false);
        }
        active_items.Clear();
    }

}
}
