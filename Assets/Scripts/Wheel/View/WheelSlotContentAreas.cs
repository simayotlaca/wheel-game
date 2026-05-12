using UnityEngine;

public static class WheelSlotContentAreas
{
    public const float IconCenterXRatio   = 0f;
    public const float IconCenterYRatio   = 0f;
    public const float IconAreaSizeRatio  = 1.0f;

    public const float AmountCenterXRatio    = 0f;
    public const float AmountCenterYRatio    = -0.28f;
    public const float AmountAreaWidthRatio  = 1.0f;
    public const float AmountAreaHeightRatio = 0.22f;

    public struct Areas
    {
        public Vector2 IconCenter;
        public Vector2 IconSize;
        public Vector2 AmountCenter;
        public Vector2 AmountSize;
    }

    public static Areas Compute(float slotSize)
    {
        Areas a;
        a.IconCenter   = new Vector2(slotSize * IconCenterXRatio,   slotSize * IconCenterYRatio);
        a.IconSize     = new Vector2(slotSize * IconAreaSizeRatio,  slotSize * IconAreaSizeRatio);
        a.AmountCenter = new Vector2(slotSize * AmountCenterXRatio, slotSize * AmountCenterYRatio);
        a.AmountSize   = new Vector2(slotSize * AmountAreaWidthRatio, slotSize * AmountAreaHeightRatio);
        return a;
    }
}
