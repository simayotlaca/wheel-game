using UnityEngine;

public enum RewardTier
{
    Normal = 0,
    Safe = 1,
    Super = 2,
}

public enum RewardKind
{
    Currency = 0,
    WeaponPoints,
    FullWeapon,
    Consumable,
    Chest,
    Cosmetic,
    Gear,
    Death,
}

public enum RewardVisualCategory
{
    Compact = 0,
    Cash,
    Coin,
    Death,
    Weapon,
    Character,

    Chest,
    Consumable,
    Cosmetic,

    Throwable
}

[CreateAssetMenu(fileName = "RewardDefinition", menuName = "Wheel/RewardDefinition")]
public class RewardDefinition : ScriptableObject
{
    public string rewardId;
    public string displayName;
    public Sprite icon;

    public Sprite wheelIcon;

    public Sprite listIcon;

    public bool isDeath;
    public RewardVisualCategory visualCategory = RewardVisualCategory.Compact;

    public string visualFamily = "";

    public RewardKind rewardKind = RewardKind.Currency;

    public IconVisualProfile iconVisualProfile = IconVisualProfile.Auto;

    public SlotCategory slotCategory = SlotCategory.Unassigned;

    public RewardTier minZoneTier = RewardTier.Normal;

    [Min(1)]
    public int minZoneLevel = 1;

    public bool displayAsMultiplier = true;

    public bool scalesWithZone = true;

    [Min(0)] public int baseAmount = 1;
    [Min(0)] public int amountStepPerZone = 0;

    public int ComputeAmount(int zone, int sliceAmount)
    {
        if (!scalesWithZone) return sliceAmount;
        int z = zone < 1 ? 1 : zone;
        return baseAmount + amountStepPerZone * (z - 1);
    }

#if UNITY_EDITOR

    void OnValidate()
    {
        if (isDeath)
        {
            slotCategory = SlotCategory.Death;
            return;
        }
        if (slotCategory != SlotCategory.Unassigned) return;

        switch (visualCategory)
        {
            case RewardVisualCategory.Cash:
            case RewardVisualCategory.Coin:       slotCategory = SlotCategory.Currency;   break;
            case RewardVisualCategory.Consumable: slotCategory = SlotCategory.Consumable; break;
            case RewardVisualCategory.Throwable:  slotCategory = SlotCategory.Throwable;  break;
            case RewardVisualCategory.Weapon:     slotCategory = SlotCategory.Weapon;     break;
            case RewardVisualCategory.Chest:      slotCategory = SlotCategory.Chest;      break;
            case RewardVisualCategory.Cosmetic:   slotCategory = SlotCategory.Cosmetic;   break;
            case RewardVisualCategory.Death:      slotCategory = SlotCategory.Death;      break;
            case RewardVisualCategory.Compact:    slotCategory = SlotCategory.Compact;    break;

        }
    }
#endif
}
