using UnityEngine;
using UnityEngine.UI;

public class RewardFlyIconPool
{
    private readonly ObjectPool<RewardFlyIcon> pool;

    public int Capacity => pool.Capacity;
    public int FreeCount => pool.FreeCount;

    public RewardFlyIconPool(Transform container, int capacity)
    {
        pool = new ObjectPool<RewardFlyIcon>(
            capacity,
            container == null ? null : (System.Func<RewardFlyIcon>)(() => CreateIcon(container)));
    }

    public RewardFlyIcon Acquire() => pool.Acquire();

    public void Release(RewardFlyIcon fly) => pool.Release(fly);

    public void DeactivateAll() => pool.DeactivateAll();

    private static RewardFlyIcon CreateIcon(Transform container)
    {
        GameObject go = new GameObject(
            "RewardFlyIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(RewardFlyIcon));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(container, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(40f, 40f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        return go.GetComponent<RewardFlyIcon>();
    }
}
