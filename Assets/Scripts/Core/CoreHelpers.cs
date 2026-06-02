using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace VertigoWheel
{
public static class NumberFormatter
{
    public static string FormatThousands(int value)
    {
        return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    //i keep this compact formatter integer only, floats made the text feel less stable
    public static string FormatCompact(int value)
    {
        if (value < 1000)
        {
            return value.ToString();
        }
        if (value < 100_000)
        {
            int hundreds = value / 100;
            int whole = hundreds / 10;
            int frac  = hundreds % 10;
            if (frac == 0)
            {
                return whole + "K";
            }
            return whole + "." + frac + "K";
        }
        if (value < 1_000_000)
        {
            return (value / 1000) + "K";
        }
        if (value < 100_000_000)
        {
            int hundred_k = value / 100_000;
            int whole = hundred_k / 10;
            int frac  = hundred_k % 10;
            if (frac == 0)
            {
                return whole + "M";
            }
            return whole + "." + frac + "M";
        }
        return (value / 1_000_000) + "M";
    }
}

public static class TweenLifetime
{
    public static void StopIfAlive(Sequence s)
    {
        if (s.isAlive)
        {
            s.Stop();
        }
    }

    public static void StopIfAlive(Tween t)
    {
        if (t.isAlive)
        {
            t.Stop();
        }
    }
}

public class ObjectPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly List<T> pool = new List<T>();
    private readonly List<T> active_items = new List<T>();

    public int ActiveCount => active_items.Count;

    public ObjectPool(T prefab, Transform parent, int init_cap)
    {
        this.prefab = prefab;
        this.parent = parent;

        EnsureCapacity(init_cap);
    }

    public void EnsureCapacity(int needed)
    {
        int target = Mathf.Max(1, needed);
        while (pool.Count < target)
        {
            if (CreateNew() == null)
            {
                return;
            }
        }
    }

    private T CreateNew()
    {
        if (prefab == null)
        {
            return null;
        }

        T obj = Object.Instantiate(prefab, parent, false);
        obj.gameObject.SetActive(false);
        pool.Add(obj);
        return obj;
    }

    public T Acquire()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            T item = pool[i];
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                active_items.Add(item);
                return item;
            }
        }

        T obj = CreateNew();
        if (obj != null)
        {
            obj.gameObject.SetActive(true);
            active_items.Add(obj);
        }
        return obj;
    }

    public T GetActive(int index)
    {
        if (index >= 0 && index < active_items.Count)
        {
            return active_items[index];
        }
        return null;
    }

    public void Release(T obj)
    {
        if (obj == null)
        {
            return;
        }

        active_items.Remove(obj);
        obj.gameObject.SetActive(false);
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < active_items.Count; i++)
        {
            T item = active_items[i];
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }
        active_items.Clear();
    }

    public void DeactivateAll()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            T item = pool[i];
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }
        active_items.Clear();
    }
}
}
