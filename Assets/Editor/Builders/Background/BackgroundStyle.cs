using UnityEngine;

internal static class BackgroundStyle
{
    public const string BgReferenceSprite = "Assets/Sprites/Wheel/bg_reference.png";
    public const float  BgAspectRatio     = 1920f / 1080f;
    public static readonly Color32 BgReferenceTint = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    public static readonly Color32 DimWashColor = new Color32(0x00, 0x00, 0x00, 60);

    public static readonly Color32 VignetteTintColor = new Color32(0x00, 0x00, 0x00, 200);
}
