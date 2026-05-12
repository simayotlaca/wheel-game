using UnityEngine;

public enum ZoneType : byte
{
    Normal = 0,
    Safe = 1,
    Super = 2
}

[CreateAssetMenu(fileName = "ZoneConfig", menuName = "Wheel/ZoneConfig")]
public class ZoneConfig : ScriptableObject
{
    public ZoneType type = ZoneType.Normal;
    public string headerLabel;
    public Color headerColor = new Color(1f, 0.85f, 0.3f, 1f);
    public string subtitle;
    public Sprite wheelBase;
    public Sprite wheelFrame;
    public Sprite wheelIndicator;
    public Color frameTint = Color.white;

    public ZonePoolRules poolRules;

    public SliceDefinition[] slices;

#if UNITY_EDITOR
    void Reset()
    {
        poolRules.enforceUniqueIcons = true;
        poolRules.useWeightedRandom = true;
    }

    void OnValidate()
    {
        if (!poolRules.allowDeath && poolRules.deathSlots > 0)
            poolRules.deathSlots = 0;

        if (slices == null) return;

        var seenRewards = new System.Collections.Generic.HashSet<RewardDefinition>();

        for (int i = 0; i < slices.Length; i++)
        {
            var slice = slices[i];
            if (slice == null)
            {
                DebugLogger.LogWarning($"[ZoneConfig:{name}] slice #{i} is empty.", this);
                continue;
            }
            if (slice.reward == null)
            {
                DebugLogger.LogWarning($"[ZoneConfig:{name}] slice #{i} ({slice.name}) has no reward assigned.", slice);
                continue;
            }
            if (slice.weight <= 0f)
                DebugLogger.LogWarning($"[ZoneConfig:{name}] slice #{i} ({slice.name}) weight must be > 0 (got {slice.weight}).", slice);
            if (slice.amount < 0)
                DebugLogger.LogWarning($"[ZoneConfig:{name}] slice #{i} ({slice.name}) amount must be >= 0 (got {slice.amount}).", slice);

            if (!seenRewards.Add(slice.reward))
                DebugLogger.LogWarning($"[ZoneConfig:{name}] slice #{i} reuses reward '{slice.reward.name}' — duplicate icon will appear on the wheel.", this);
        }
    }
#endif
}
