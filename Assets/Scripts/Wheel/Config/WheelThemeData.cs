using UnityEngine;

public enum WheelTier : byte
{
    Bronze = 0,
    Silver = 1,
    Gold   = 2
}

[CreateAssetMenu(fileName = "WheelThemeData", menuName = "Wheel/WheelThemeData")]
public class WheelThemeData : ScriptableObject
{
    public WheelTier tier = WheelTier.Bronze;
    public Sprite wheelBase;
    public Sprite wheelFrame;
    public Sprite wheelIndicator;
    public Color frameTint = Color.white;
}
