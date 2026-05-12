using UnityEngine;

[System.Serializable]
public struct ZonePoolRules
{
    [Min(1)]
    public int slotCount;

    public bool allowDeath;
    public bool enforceUniqueIcons;
    public bool useWeightedRandom;

    [Min(0)] public int deathSlots;
    [Min(0)] public int currencySlots;
    [Min(0)] public int consumableSlots;
    [Min(0)] public int throwableSlots;
    [Min(0)] public int weaponSlots;
    [Min(0)] public int compactSlots;
    [Min(0)] public int chestSlots;
    [Min(0)] public int cosmeticSlots;
    [Min(0)] public int goldSlots;

    [Min(0)] public int flexSlots;

    public bool useRecentIconMemory;
    [Range(0f, 1f)]
    public float recentIconPenalty;

    public bool allowFallbackWhenPoolInsufficient;

    public int EffectiveDeathSlots => allowDeath ? deathSlots : 0;
}
