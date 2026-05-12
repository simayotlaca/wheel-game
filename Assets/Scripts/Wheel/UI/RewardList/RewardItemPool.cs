using UnityEngine;

public class RewardItemPool
{
    private readonly int capacity;

    private readonly RewardListItemUI[] free_stack;
    private int free_top;

    private readonly RewardListItemUI[] active_arr;
    private int active_count;

    public int ActiveCount => active_count;

    public RewardItemPool(RewardListItemUI prefab, Transform container, int capacity)
    {
        this.capacity = Mathf.Max(1, capacity);

        free_stack = new RewardListItemUI[this.capacity];
        active_arr = new RewardListItemUI[this.capacity];

        if (prefab == null || container == null) return;
        for (int i = 0; i < this.capacity; i++)
        {
            RewardListItemUI item = Object.Instantiate(prefab, container);
            item.gameObject.SetActive(false);
            free_stack[free_top++] = item;
        }
    }

    public RewardListItemUI Acquire()
    {
        if (free_top <= 0 || active_count >= capacity) return null;

        RewardListItemUI item = free_stack[--free_top];
        free_stack[free_top] = null;

        item.gameObject.SetActive(true);

        item.transform.SetAsLastSibling();
        active_arr[active_count++] = item;
        return item;
    }

    public RewardListItemUI GetActive(int index)
    {
        if (index < 0 || index >= active_count) return null;
        return active_arr[index];
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < active_count; i++)
        {
            RewardListItemUI item = active_arr[i];
            active_arr[i] = null;
            if (item == null) continue;
            item.Clear();
            item.gameObject.SetActive(false);
            if (free_top < capacity)
                free_stack[free_top++] = item;
        }
        active_count = 0;
    }
}
