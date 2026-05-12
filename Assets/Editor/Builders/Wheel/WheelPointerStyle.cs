using UnityEngine;

internal static class WheelPointerStyle
{
    public static readonly float WheelDiameter = WheelGeometry.DefaultWheelDiameter;

    private const float CalibrationDiameter = 850f;
    private const float WidthRatio  = 134f / CalibrationDiameter;
    private const float HeightRatio = 126f / CalibrationDiameter;
    private const float PosYRatio   = 404f / CalibrationDiameter;

    public static readonly Vector2 Size = new Vector2(
        WheelDiameter * WidthRatio,
        WheelDiameter * HeightRatio);

    public static readonly Vector2 Position = new Vector2(
        0f,
        WheelDiameter * PosYRatio);

    public static readonly Vector2 AnchorMin = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 AnchorMax = new Vector2(0.5f, 0.5f);

    public static readonly Vector2 Pivot = new Vector2(0.5f, 1f);

    public const bool PreserveAspect = false;

    public const string SpriteName = "ui_spin_bronze_indicator";
}
