using UnityEngine;

namespace VertigoWheel
{
//i start real categories from 1 so empty default 0 does not become a real bucket
public enum SlotCategory
{
    Death = 1,
    Currency,
    Other,
    AllCards,
    Special,
}

public static class SlotCategoryHelper
{
    public static int QuotaFor(in RewardTableConfig.QuotaSet quotas, bool allowDeath, SlotCategory cat)
    {
        switch (cat)
        {
            case SlotCategory.Death:    return allowDeath ? quotas.deathSlots : 0;
            case SlotCategory.Currency: return quotas.currencySlots;
            case SlotCategory.Other:    return quotas.otherSlots;
            case SlotCategory.AllCards: return quotas.allCardsSlots;
            case SlotCategory.Special:  return quotas.specialSlots;
        }
        return 0;
    }

    public static ZoneRewardEntry[] PoolEntriesFor(RewardTableConfig.ZoneTable zt, SlotCategory cat)
    {
        if (zt == null)
        {
            return null;
        }
        switch (cat)
        {
            case SlotCategory.Death:    return zt.deathPool;
            case SlotCategory.Other:    return zt.otherPool;
            case SlotCategory.AllCards: return zt.allCardsPool;
            case SlotCategory.Special:  return zt.specialPool;
        }
        return null;
    }
}

public enum RewardTier
{
    Normal = 0,
    Safe = 1,
    Super = 2,
}

public enum RewardVisualCategory
{
    Compact,
    Cash,
    Coin,
    Death,
    Weapon,
    Chest = 6,
    Consumable,
    Cosmetic,
}

[CreateAssetMenu(fileName = "RewardDefinition", menuName = "Vertigo Wheel/Rewards/Reward Definition")]
public class RewardDefinition : ScriptableObject
{
    public string rewardId;
    public Sprite icon;
    public Sprite wheelIcon;
    public Sprite listIcon;

    public bool isDeath;
    public SlotCategory slotCategory = SlotCategory.AllCards;
    public RewardVisualCategory visualCategory = RewardVisualCategory.Compact;
    public RewardTier minZoneTier = RewardTier.Normal;

    public string visualFamily = "";

    public bool displayAsMultiplier = true;
}
}
