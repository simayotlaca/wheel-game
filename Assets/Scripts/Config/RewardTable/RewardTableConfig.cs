using UnityEngine;

namespace VertigoWheel
{
[System.Serializable]
public class ZoneRewardEntry
{
    public RewardDefinition reward;
}

[CreateAssetMenu(fileName = "RewardTableConfig", menuName = "Vertigo Wheel/Config/Reward Table Config")]
public class RewardTableConfig : ScriptableObject
{
    [System.Serializable]
    public struct PoolRules
    {
        public bool allowDeath;
    }

    [System.Serializable]
    public struct QuotaSet
    {
        [Min(0)] public int deathSlots;
        [Min(0)] public int currencySlots;
        [Min(0)] public int otherSlots;
        [Min(0)] public int allCardsSlots;
        [Min(0)] public int specialSlots;
    }

    [System.Serializable]
    public class ZoneTable
    {
        public PoolRules poolRules;
        public QuotaSet quotas;
        public ZoneRewardEntry[] deathPool;
        public ZoneRewardEntry[] otherPool;
        public ZoneRewardEntry[] allCardsPool;
        public ZoneRewardEntry[] specialPool;
    }

    [System.Serializable]
    public class CategorySortPriorities
    {
        public int priority_gold = 0;
        public int priority_cash = 1;
        public int priority_special = 2;
        public int priority_all_cards = 3;
        public int priority_other = 4;
        public int priority_death = 90;
        public int priority_default = 99;
    }

    public ZoneTable normalZone;
    public ZoneTable safeZone;
    public ZoneTable superZone;

    public CategorySortPriorities sortPriorities = new CategorySortPriorities();

    public ZoneTable GetForTier(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Safe:  return safeZone;
            case RewardTier.Super: return superZone;
            default:               return normalZone;
        }
    }

    //i check the easy mistakes in editor first, like quota not being 8
    //pool count too, so i see it in console before pressing play and wondering why spin broke
    private void OnValidate()
    {
        ValidateZone(normalZone, nameof(normalZone));
        ValidateZone(safeZone, nameof(safeZone));
        ValidateZone(superZone, nameof(superZone));
    }

    private void ValidateZone(ZoneTable zt, string label)
    {
        if (zt == null)
        {
            return;
        }

        int total = zt.quotas.deathSlots + zt.quotas.currencySlots + zt.quotas.otherSlots + zt.quotas.allCardsSlots + zt.quotas.specialSlots;
        if (total != WheelResultPicker.WheelSlotCapacity)
        {
            Debug.LogError($"reward table {label}: quota sum {total}, expected {WheelResultPicker.WheelSlotCapacity}", this);
        }

        if (!zt.poolRules.allowDeath && zt.quotas.deathSlots > 0)
        {
            Debug.LogError($"reward table {label}: deathSlots {zt.quotas.deathSlots} with allowDeath=false", this);
        }
        if (zt.poolRules.allowDeath && zt.quotas.deathSlots <= 0)
        {
            Debug.LogError($"reward table {label}: allowDeath=true but deathSlots {zt.quotas.deathSlots}", this);
        }

        int effectiveDeathQuota = zt.poolRules.allowDeath ? zt.quotas.deathSlots : 0;
        ValidatePool(label, nameof(ZoneTable.deathPool), zt.deathPool, effectiveDeathQuota);
        ValidatePool(label, nameof(ZoneTable.otherPool), zt.otherPool, zt.quotas.otherSlots);
        ValidatePool(label, nameof(ZoneTable.allCardsPool), zt.allCardsPool, zt.quotas.allCardsSlots);
        ValidatePool(label, nameof(ZoneTable.specialPool), zt.specialPool, zt.quotas.specialSlots);
    }

    private void ValidatePool(string label, string poolName, ZoneRewardEntry[] pool, int quota)
    {
        if (quota <= 0)
        {
            return;
        }
        int poolSize = pool != null ? pool.Length : 0;
        if (poolSize < quota)
        {
            Debug.LogError($"reward table {label}: {poolName} size {poolSize} insufficient for quota {quota}", this);
            return;
        }
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == null || pool[i].reward == null)
            {
                Debug.LogError($"reward table {label}: {poolName}[{i}] has null reward", this);
            }
        }
    }
}
}
