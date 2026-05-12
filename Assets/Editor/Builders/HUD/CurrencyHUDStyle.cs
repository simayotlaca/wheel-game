using UnityEngine;

internal static class CurrencyHUDStyle
{
    public static readonly Vector2 ContainerSize    = new Vector2(420f, 55f);
    public static readonly Vector2 ContainerOffset  = new Vector2(-55f, -58f);
    public static readonly Vector3 ContainerScale   = new Vector3(1.08f, 1.08f, 1f);
    public const float ContainerSpacing = 4f;

    public const int PillPadL          = 10;
    public const int PillPadR          = 10;
    public const float PillSpacing     = 7f;
    public const float PillMinHeight   = 60f;
    public const float IconSize        = 56f;

    public const float TextMinWidth    = 20f;

    public static readonly Color32 CashColor = new Color32(0x8E, 0xF0, 0xA5, 0xFF);
    public static readonly Color32 CoinColor = new Color32(0xF6, 0xC6, 0x5B, 0xFF);
    public const float CurrencyFontSize = 36f;

    public const string SpriteCashIcon = "Assets/Sprites/Wheel/UI_icon_cash.png";
    public const string SpriteCoinIcon = "Assets/Sprites/Wheel/UI_icon_gold.png";
}
