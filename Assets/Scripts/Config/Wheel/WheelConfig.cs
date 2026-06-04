using System;
using UnityEngine;

namespace VertigoWheel
{
[Serializable]
public class WheelVisual
{
    public RewardTier tier;
    public Sprite wheelBase;
    public Sprite wheelIndicator;
}

[Serializable]
public struct WheelSpinTiming
{
    [Min(0.1f)] public float duration;
    [Min(0f)] public float minFullRotations;
    [Min(0f)] public float maxFullRotations;
    public AnimationCurve curve;
}

[CreateAssetMenu(fileName = "WheelConfig", menuName = "Vertigo Wheel/Config/Wheel Config")]
public class WheelConfig : ScriptableObject
{
    [Header("Zone Range")]
    [Min(1)] public int firstZoneIndex;
    [Min(1)] public int maxZoneIndex;

    [Header("Zone Rules")]
    [Min(1)] public int safeZoneInterval;
    [Min(1)] public int superZoneInterval;

    [Header("Zone Variants")]
    public WheelVisual normalZone;
    public WheelVisual safeZone;
    public WheelVisual superZone;

    [Header("Reward Table")]
    public RewardTableConfig rewardTable;

    [Header("Meta Progression")]
    public MetaProgressConfig metaProgressConfig;

    [Header("Spin Animation")]
    public WheelSpinTiming spinTiming;

    [Header("Feedback")]
    public FeedbackConfig feedbackConfig;

    [Header("Currency")]
    public CurrencyConfig currency_config;

    internal int FirstZoneIndex
    {
        get
        {
            return firstZoneIndex;
        }
    }

    internal int MaxZoneIndex
    {
        get
        {
            return maxZoneIndex;
        }
    }
}
}
