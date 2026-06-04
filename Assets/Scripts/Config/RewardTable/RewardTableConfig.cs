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

    public ZoneTable normalZone;
    public ZoneTable normalReviveZone;
    public ZoneTable safeZone;
    public ZoneTable superZone;

    internal ZoneTable GetForTier(RewardTier tier, bool revive)
    {
        if (revive && tier == RewardTier.Normal)
        {
            return normalReviveZone;
        }

        switch (tier)
        {
            case RewardTier.Safe:  return safeZone;
            case RewardTier.Super: return superZone;
            default:               return normalZone;
        }
    }
}
}
