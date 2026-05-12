using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class WheelSliceContentLayout
{
    private static readonly Color32 AMOUNT_COLOR = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    public static void Apply(SliceView sliceView, RewardDefinition reward)
    {
        if (sliceView == null) return;
        WheelSlotContentAreas.Areas A = WheelSlotContentAreas.Compute(WheelGeometry.SlotSize);
        IconVisualProfile profile = WheelIconVisualProfileResolver.Resolve(reward);
        ApplyUprightGroup(sliceView.UprightGroup);
        ApplyIcon(sliceView.IconImage, profile, A);
        ApplyAmount(sliceView.AmountText, profile, A);
    }

    public static RewardVisualCategory ResolveCategory(RewardDefinition reward)
    {
        if (reward == null) return RewardVisualCategory.Compact;
        if (reward.isDeath) return RewardVisualCategory.Death;
        return reward.visualCategory;
    }

    private static void ApplyUprightGroup(RectTransform up)
    {
        if (up == null) return;
        up.anchorMin = new Vector2(0.5f, 0.5f);
        up.anchorMax = new Vector2(0.5f, 0.5f);
        up.pivot     = new Vector2(0.5f, 0.5f);
        up.sizeDelta = new Vector2(WheelGeometry.SlotSize, WheelGeometry.SlotSize);
        up.anchoredPosition = new Vector2(0f, WheelGeometry.SlotRadialOffset);
    }

    private static void ApplyIcon(Image icon, IconVisualProfile profile,
                                  WheelSlotContentAreas.Areas A)
    {
        if (icon == null) return;

        icon.preserveAspect = true;

        WheelIconVisualProfileResolver.IconStyle s =
            WheelIconVisualProfileResolver.Resolve(profile, WheelGeometry.SlotSize);

        RectTransform rt = icon.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = s.SizeDelta;
        rt.anchoredPosition = A.IconCenter + s.AnchoredOffset;
    }

    private static void ApplyAmount(TMP_Text amount,
                                    IconVisualProfile profile,
                                    WheelSlotContentAreas.Areas A)
    {
        if (amount == null) return;

        WheelAmountTextStyle.Style s = WheelAmountTextStyle.Resolve(amount.text, WheelGeometry.SlotSize, A);

        RectTransform rt = amount.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = s.SizeDelta;

        Vector2 amountOffset = profile == IconVisualProfile.WeaponLong ? new Vector2(0f, -2f) : Vector2.zero;
        rt.anchoredPosition = A.AmountCenter + amountOffset;

        amount.enableAutoSizing = s.EnableAutoSizing;
        amount.fontSizeMax = s.FontSizeMax;
        amount.fontSizeMin = s.FontSizeMin;
        amount.fontSize = s.FontSizeMax;
        amount.fontStyle |= FontStyles.Bold;
        amount.color = AMOUNT_COLOR;
        amount.alignment = s.Alignment;
        amount.enableWordWrapping = false;

        amount.overflowMode = TextOverflowModes.Overflow;
    }
}
