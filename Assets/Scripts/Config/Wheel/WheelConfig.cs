using System;
using UnityEngine;

namespace VertigoWheel
{
[Serializable]
public class WheelVisual
{
    public RewardTier tier = RewardTier.Normal;
    public Sprite wheelBase;
    public Sprite wheelIndicator;
}

[Serializable]
public class RunExitRules
{
    [SerializeField] private bool allow_exit_on_normal_zones = true;
    public bool AllowExitOnNormalZones => allow_exit_on_normal_zones;
}

[CreateAssetMenu(fileName = "WheelConfig", menuName = "Vertigo Wheel/Config/Wheel Config")]
public class WheelConfig : ScriptableObject
{
    [Header("Zone Range")]
    [Min(1)] public int firstZoneIndex = 1;
    [Min(1)] public int maxZoneIndex = 60;

    [Header("Zone Rules")]
    [Min(1)] public int safeZoneInterval = 5;
    [Min(1)] public int superZoneInterval = 30;

    [Header("Zone Variants")]
    public WheelVisual normalZone;
    public WheelVisual safeZone;
    public WheelVisual superZone;

    [Header("Reward Table")]
    public RewardTableConfig rewardTable;

    [Header("Meta Progression")]
    public MetaProgressConfig metaProgressConfig;

    [Header("Spin Animation")]
    [Min(0.1f)] public float spinDuration = 3.5f;
    [Min(0.01f)] public float minSpinDurationSeconds = 0.1f;
    [Min(0f)] public float minFullRotations = 4f;
    [Min(0f)] public float maxFullRotations = 6f;

    [Header("Reward Popup")]
    [Min(0f)] public float rewardPopupShowDuration = 0.35f;
    [Min(0f)] public float rewardPopupHoldDuration = 1.0f;

    [Header("Currency IDs")]
    [SerializeField] public CurrencyConfig currency_config;

    [Header("Exit Rules")]
    [SerializeField] private RunExitRules exit_rules = new RunExitRules();
    public RunExitRules ExitRules => exit_rules;

    public int FirstZoneIndex => Mathf.Max(1, firstZoneIndex);
    public int MaxZoneIndex => Mathf.Max(FirstZoneIndex, maxZoneIndex);
}
}
