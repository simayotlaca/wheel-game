#if UNITY_EDITOR
using UnityEngine;

internal static class MetaProgressStyle
{
    public static readonly Vector2 PanelAnchorMin       = new Vector2(1f, 0.5f);
    public static readonly Vector2 PanelAnchorMax       = new Vector2(1f, 0.5f);
    public static readonly Vector2 PanelPivot           = new Vector2(1f, 0.5f);
    public static readonly Vector2 PanelSize            = new Vector2(360f, 620f);
    public static readonly Vector2 PanelAnchoredPos     = new Vector2(-70f, -40f);

    public static readonly RectOffset RowsContainerPadding = new RectOffset(0, 0, 0, 0);
    public const float RowsContainerSpacing = 28f;

    public const float RowHeight = 140f;

    public const float BorderInset = -6f;
    public static readonly Color32 BorderColorIdle       = new Color32(0xFF, 0xFF, 0xFF, 0);

    public static readonly Color32 BackplateColor       = new Color32(0x1B, 0x15, 0x18, 238);

    public static readonly Color32 BackplateShadowColor  = new Color32(0x00, 0x00, 0x00, 110);
    public static readonly Vector2 BackplateShadowOffset = new Vector2(0f, -4f);

    public const float RarityEdgeWidth     = 5f;
    public const float RarityEdgePaddingV  = 8f;

    public static readonly Vector2 IconGlowSize         = new Vector2(160f, 140f);
    public const float IconGlowAlpha = 75f / 255f;

    public static readonly Vector2 IconFrameSize        = new Vector2(116f, 116f);
    public static readonly Color32 IconFrameColor       = new Color32(0x2A, 0x31, 0x40, 255);
    public static readonly Vector2 IconFrameAnchoredPos = new Vector2(14f, 0f);
    public static readonly Color32 IconFrameShadowColor  = new Color32(0x00, 0x00, 0x00, 120);
    public static readonly Vector2 IconFrameShadowOffset = new Vector2(0f, -2f);

    public static readonly Vector2 WeaponIconSize       = new Vector2(104f, 104f);
    public static readonly Vector2 WeaponIconAnchoredPos = new Vector2(14f, 0f);
    public static readonly Vector2 IconAnchor           = new Vector2(0f, 0.5f);

    public static readonly Vector2 InfoGroupAnchorMin   = new Vector2(0f, 0f);
    public static readonly Vector2 InfoGroupAnchorMax   = new Vector2(1f, 1f);
    public static readonly Vector2 InfoGroupPivot       = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 InfoGroupOffsetMin   = new Vector2(150f, 0f);
    public static readonly Vector2 InfoGroupOffsetMax   = new Vector2(-16f, 0f);

    public static readonly Vector2 NameAnchorMin        = new Vector2(0f, 1f);
    public static readonly Vector2 NameAnchorMax        = new Vector2(1f, 1f);
    public static readonly Vector2 NamePivot            = new Vector2(0f, 1f);
    public static readonly Vector2 NameOffsetMin        = new Vector2(0f, -44f);
    public static readonly Vector2 NameOffsetMax        = new Vector2(0f, -12f);
    public const int   NameFontSize = 28;

    public static readonly Vector2 BarAnchorMin         = new Vector2(0f, 1f);
    public static readonly Vector2 BarAnchorMax         = new Vector2(1f, 1f);
    public static readonly Vector2 BarPivot             = new Vector2(0.5f, 1f);
    public static readonly Vector2 BarOffsetMin         = new Vector2(0f, -72f);
    public static readonly Vector2 BarOffsetMax         = new Vector2(0f, -54f);

    public static readonly Color32 BarBgColor           = new Color32(0x06, 0x09, 0x0F, 200);

    public const float BarHighlightHeight = 3f;
    public static readonly Color32 BarHighlightColor    = new Color32(0xFF, 0xFF, 0xFF, 110);

    public static readonly Vector2 AmountAnchorMin      = new Vector2(0f, 1f);
    public static readonly Vector2 AmountAnchorMax      = new Vector2(1f, 1f);
    public static readonly Vector2 AmountPivot          = new Vector2(1f, 1f);
    public static readonly Vector2 AmountOffsetMin      = new Vector2(0f, -100f);
    public static readonly Vector2 AmountOffsetMax      = new Vector2(0f, -78f);
    public const int   AmountFontSize = 16;
    public static readonly Color32 AmountColor          = new Color32(0xA0, 0xA4, 0xAC, 255);

    public static readonly Vector2 UnlockedAnchorMin    = new Vector2(1f, 1f);
    public static readonly Vector2 UnlockedAnchorMax    = new Vector2(1f, 1f);
    public static readonly Vector2 UnlockedPivot        = new Vector2(1f, 1f);
    public static readonly Vector2 UnlockedSize         = new Vector2(120f, 28f);
    public static readonly Vector2 UnlockedAnchoredPos  = new Vector2(-12f, -10f);
    public const int   UnlockedFontSize = 16;

    public static readonly Color32 FlashColorIdle       = new Color32(0xFF, 0xFF, 0xFF, 0);

    public const string BackplateSprite = "Assets/Sprites/Wheel/ui_card_panel_zone_bg.png";
    public const string BarBgSprite     = "Assets/Sprites/Wheel/ui_card_panel_zone_bg.png";
    public const string BarFillSprite   = "Assets/Sprites/Wheel/ui_card_panel_zone_current_white.png";
    public const string SolidSprite     = "Assets/Sprites/Wheel/ui_card_panel_zone_current_white.png";

    public const float RowAnimDuration = 0.6f;
    public const int   MaxVisibleRows  = 3;

    public static Color32 RarityColor(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Super: return new Color32(0xF4, 0x8A, 0x3A, 255);
            case RewardTier.Safe:  return new Color32(0x4C, 0x9C, 0xF0, 255);
            default:               return new Color32(0x8A, 0x90, 0x9A, 255);
        }
    }

    public static Color32 RarityGlowColor(RewardTier tier)
    {
        Color32 c = RarityColor(tier);
        c.a = (byte)(255 * IconGlowAlpha);
        return c;
    }
}
#endif
