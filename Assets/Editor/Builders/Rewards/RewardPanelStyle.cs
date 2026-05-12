using UnityEngine;

internal static class RewardPanelStyle
{
    public static readonly Color32 BgFill        = new Color32(0x00, 0x00, 0x00, 255);
    public static readonly Color32 OutlineTint   = new Color32(0xEA, 0xEC, 0xEF, 242);
    public const float OutlinePPUMul             = 3.5f;

    public const float ExitW                = 200f;
    public const float ExitH                = 72f;
    public const float ExitYOffset          = -12f;
    public const float ExitFontSize         = 40f;
    public const float ExitCharSpacing      = 8f;
    public static readonly Color32 ExitTextColor    = new Color32(255, 255, 255, 255);
    public static readonly Color32 ExitInnerShadow  = new Color32(0, 0, 0, 110);
    public static readonly Vector2 ExitInnerDist    = new Vector2(0f, -2f);

    public static readonly Color32 ExitBgTint       = new Color32(0xF2, 0xF5, 0xF9, 255);

    public static readonly Color32 ExitTextGlow     = new Color32(255, 255, 255, 200);
    public static readonly Vector2 ExitTextGlowDist = new Vector2(2.0f, -2.0f);

    public static readonly Color ExitHighlighted = new Color32(0xFF, 0xFF, 0xFF, 255);
    public static readonly Color ExitPressed     = new Color32(0xC0, 0xC6, 0xCE, 255);
    public static readonly Color ExitDisabled    = new Color32(0x7E, 0x86, 0x95, 153);
    public const float ExitFadeDuration         = 0.1f;

    public const float ItemsSpacing   = 9f;
    public const int   ItemsPadTop    = 16;

    public static readonly Vector2 ScrollOffsetMin = new Vector2(0f, 26f);
    public static readonly Vector2 ScrollOffsetMax = new Vector2(0f, -84f);

    public static readonly Vector2 RowSize   = new Vector2(200f, 96f);
    public const float RowMinHeight          = 96f;
    public const int   RowPadLeft            = 0;
    public const float RowSpacing            = 12f;

    public const float IconFrameSize   = 108f;
    public const float IconSize        = 104f;

    public static readonly Vector2 AmountSize = new Vector2(108f, 52f);
    public const float AmountFontSize  = 32f;

    public static readonly Color32 IconFrameTint = new Color32(0x3F, 0x48, 0x54, 230);

    public const float ScrollElasticity        = 0.08f;
    public const float ScrollDecelerationRate  = 0.135f;
    public const float ScrollSensitivity       = 25f;

    public const float ScrollbarWidth            = 7f;
    public const float ScrollbarInset            = 8f;
    public const float ScrollbarVerticalPadding  = 36f;

    public static readonly Color32 ScrollbarTrackColor  = new Color32(0xFF, 0xFF, 0xFF, 0x30);
    public static readonly Color32 ScrollbarHandleColor = new Color32(0xEA, 0xEC, 0xEF, 0xA0);

    public static readonly Color ViewportRaycastColor = new Color(0f, 0f, 0f, 0.004f);

    public const string SpriteBg        = "Assets/Sprites/Wheel/ui_card_panel_zone_bg.png";
    public const string SpriteOutline   = "Assets/Sprites/Wheel/ui_card_frame_12px_neutral.png";
    public const string SpriteExitBg    = "Assets/Sprites/Wheel/UI_button_grey_standard.png";
    public const string SpriteIconFrame = "Assets/Sprites/Wheel/ui_card_panel_zone_bg.png";
}
