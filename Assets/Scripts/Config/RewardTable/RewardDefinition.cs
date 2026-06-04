using System.Globalization;
using UnityEngine;

namespace VertigoWheel
{
public enum SlotCategory
{
    Death = 1,
    Currency,
    Other,
    AllCards,
    Special,
}

internal static class SlotCategoryHelper
{
    private static ZoneRewardEntry[] EmptyEntries = new ZoneRewardEntry[0];

    internal static int QuotaFor(RewardTableConfig.QuotaSet quotas, bool allow_death, SlotCategory cat)
    {
        switch (cat)
        {
            case SlotCategory.Death:    return allow_death ? quotas.deathSlots : 0;
            case SlotCategory.Currency: return quotas.currencySlots;
            case SlotCategory.Other:    return quotas.otherSlots;
            case SlotCategory.AllCards: return quotas.allCardsSlots;
            case SlotCategory.Special:  return quotas.specialSlots;
        }

        return 0;
    }

    internal static ZoneRewardEntry[] PoolEntriesFor(RewardTableConfig.ZoneTable zt, SlotCategory cat)
    {
        switch (cat)
        {
            case SlotCategory.Death:    return zt.deathPool;
            case SlotCategory.Other:    return zt.otherPool;
            case SlotCategory.AllCards: return zt.allCardsPool;
            case SlotCategory.Special:  return zt.specialPool;
        }

        return EmptyEntries;
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

public enum RewardAmountMode
{
    Fixed = 0,
    CashProgression = 1,
    GoldProgression = 2,
    CardProgression = 3,
}

[CreateAssetMenu(fileName = "RewardDefinition", menuName = "Vertigo Wheel/Rewards/Reward Definition")]
public class RewardDefinition : ScriptableObject
{
    public string rewardId;
    public Sprite icon;
    public Sprite wheelIcon;
    public Sprite listIcon;

    public bool isDeath;
    public SlotCategory slotCategory;
    public RewardVisualCategory visualCategory;
    public RewardTier minZoneTier;

    public string visualFamily;

    public bool displayAsMultiplier;

    [Header("Amount")]
    public RewardAmountMode amountMode;
    [Min(0)] public int fixedAmount;

    internal Sprite ResolveWheelIcon()
    {
        return wheelIcon != null ? wheelIcon : icon;
    }

    internal Sprite ResolveListIcon()
    {
        return listIcon != null ? listIcon : icon;
    }

    internal string ResolveAmountText(int amount)
    {
        if (isDeath)
        {
            return string.Empty;
        }

        if (displayAsMultiplier)
        {
            return "x" + NumberFormatter.FormatCompact(amount);
        }

        switch (visualCategory)
        {
            case RewardVisualCategory.Coin:
            case RewardVisualCategory.Cash:
                return NumberFormatter.FormatCompact(amount);

            case RewardVisualCategory.Weapon:
                return "+" + NumberFormatter.FormatCompact(amount);

            case RewardVisualCategory.Compact:
            case RewardVisualCategory.Chest:
            case RewardVisualCategory.Consumable:
            case RewardVisualCategory.Cosmetic:
                if (amount <= 1)
                {
                    return string.Empty;
                }
                return "x" + amount.ToString(CultureInfo.InvariantCulture);

            default:
                return amount.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal bool CanAppearIn(RewardTier zone_tier, bool allow_death)
    {
        if ((int)minZoneTier > (int)zone_tier)
        {
            return false;
        }

        return !isDeath || allow_death;
    }
}
}
