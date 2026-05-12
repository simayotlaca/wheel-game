using UnityEngine;

public enum IconVisualProfile
{
    Auto = 0,
    Currency,
    Chest,
    Consumable,
    Throwable,
    WeaponLong,
    WeaponCompact,
    MeleeLong,
    Cosmetic,
    Death,
    Compact,
    Character,
}

public static class WheelIconVisualProfileResolver
{
    public struct IconStyle
    {
        public Vector2 SizeDelta;
        public Vector2 AnchoredOffset;
    }

    public static IconVisualProfile Resolve(RewardDefinition reward)
    {
        if (reward == null) return IconVisualProfile.Compact;
        if (reward.isDeath) return IconVisualProfile.Death;
        if (reward.iconVisualProfile != IconVisualProfile.Auto)
            return reward.iconVisualProfile;

        switch (reward.slotCategory)
        {
            case SlotCategory.Currency:   return IconVisualProfile.Currency;
            case SlotCategory.Chest:      return IconVisualProfile.Chest;
            case SlotCategory.Consumable: return IconVisualProfile.Consumable;
            case SlotCategory.Throwable:  return IconVisualProfile.Throwable;
            case SlotCategory.Weapon:     return IconVisualProfile.WeaponLong;
            case SlotCategory.Cosmetic:   return IconVisualProfile.Cosmetic;
            case SlotCategory.Death:      return IconVisualProfile.Death;
            case SlotCategory.Compact:    return IconVisualProfile.Compact;
            case SlotCategory.Unassigned:
            default:                      return IconVisualProfile.Compact;
        }
    }

    public static IconStyle Resolve(IconVisualProfile profile, float slotSize)
    {
        IconStyle s;
        s.AnchoredOffset = Vector2.zero;

        switch (profile)
        {
            case IconVisualProfile.Currency:

                s.SizeDelta = Square(slotSize, 0.70f);
                s.AnchoredOffset = new Vector2(0f, slotSize * -0.04f);
                break;

            case IconVisualProfile.Chest:
                s.SizeDelta = Square(slotSize, 0.68f);

                s.AnchoredOffset = new Vector2(0f, slotSize * -0.02f);
                break;

            case IconVisualProfile.Consumable:
                s.SizeDelta = Square(slotSize, 0.62f);
                break;

            case IconVisualProfile.Throwable:

                s.SizeDelta = Square(slotSize, 0.60f);
                s.AnchoredOffset = new Vector2(slotSize * -0.08f, slotSize * -0.04f);
                break;

            case IconVisualProfile.WeaponLong:

                s.SizeDelta = Square(slotSize, 0.71f);
                break;

            case IconVisualProfile.WeaponCompact:
                s.SizeDelta = Square(slotSize, 0.62f);
                break;

            case IconVisualProfile.MeleeLong:
                s.SizeDelta = Square(slotSize, 0.72f);
                break;

            case IconVisualProfile.Cosmetic:
                s.SizeDelta = Square(slotSize, 0.65f);
                break;

            case IconVisualProfile.Death:

                s.SizeDelta = Square(slotSize, 0.78f);
                s.AnchoredOffset = new Vector2(0f, slotSize * -0.10f);
                break;

            case IconVisualProfile.Character:
                s.SizeDelta = Square(slotSize, 0.70f);
                break;

            case IconVisualProfile.Compact:
            case IconVisualProfile.Auto:
            default:
                s.SizeDelta = Square(slotSize, 0.62f);
                break;
        }
        return s;
    }

    private const float GlobalIconScale = 0.94f;

    private static Vector2 Square(float slotSize, float ratio) =>
        new Vector2(slotSize * ratio * GlobalIconScale, slotSize * ratio * GlobalIconScale);
}
