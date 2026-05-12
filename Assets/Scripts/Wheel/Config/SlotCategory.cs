public enum SlotCategory
{
    Unassigned,
    Compact,
    Death,
    Currency,
    Consumable,
    Throwable,
    Weapon,
    Chest,
    Cosmetic,
    Gold,
}

public static class SlotCategoryHelper
{

    public static SlotCategory For(RewardDefinition reward)
    {
        if (reward == null)
            throw new System.ArgumentNullException(nameof(reward),
                "SlotCategoryHelper.For requires a non-null reward; filter null slices upstream.");
        return reward.isDeath ? SlotCategory.Death : reward.slotCategory;
    }

    public static int QuotaFor(in ZonePoolRules rules, SlotCategory cat)
    {
        switch (cat)
        {
            case SlotCategory.Death:      return rules.EffectiveDeathSlots;
            case SlotCategory.Currency:   return rules.currencySlots;
            case SlotCategory.Consumable: return rules.consumableSlots;
            case SlotCategory.Throwable:  return rules.throwableSlots;
            case SlotCategory.Weapon:     return rules.weaponSlots;
            case SlotCategory.Compact:    return rules.compactSlots;
            case SlotCategory.Chest:      return rules.chestSlots;
            case SlotCategory.Cosmetic:   return rules.cosmeticSlots;
            case SlotCategory.Gold:       return rules.goldSlots;
            case SlotCategory.Unassigned: return 0;
        }
        throw new System.ArgumentOutOfRangeException(nameof(cat),
            $"SlotCategory.{cat} has no quota mapping in ZonePoolRules — extend ZonePoolRules and QuotaFor when adding a category.");
    }
}
