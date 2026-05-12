using System;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly int capacity;
    private readonly T[] free_stack;
    private readonly T[] all_owned;
    private int free_top;

    public int Capacity => capacity;
    public int FreeCount => free_top;

    public ObjectPool(int capacity, Func<T> factory)
    {
        this.capacity = Mathf.Max(1, capacity);
        free_stack = new T[this.capacity];
        all_owned  = new T[this.capacity];
        if (factory == null) return;

        for (int i = 0; i < this.capacity; i++)
        {
            T obj = factory();
            if (obj == null) continue;
            obj.gameObject.SetActive(false);
            free_stack[free_top++] = obj;
            all_owned[i] = obj;
        }
    }

    public T Acquire()
    {
        if (free_top <= 0) return null;
        T obj = free_stack[--free_top];
        free_stack[free_top] = null;
        if (obj != null) obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(false);
        if (free_top < capacity) free_stack[free_top++] = obj;
    }

    public void DeactivateAll()
    {
        if (all_owned == null) return;
        for (int i = 0; i < all_owned.Length; i++)
        {
            T item = all_owned[i];
            if (item == null) continue;
            if (item.gameObject.activeSelf) item.gameObject.SetActive(false);
            free_stack[i] = item;
        }
        free_top = capacity;
    }
}
