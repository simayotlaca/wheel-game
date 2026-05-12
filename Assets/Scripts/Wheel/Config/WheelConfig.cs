using UnityEngine;

[CreateAssetMenu(fileName = "WheelConfig", menuName = "Wheel/WheelConfig")]
public class WheelConfig : ScriptableObject
{
    [Header("Zone Rules")]
    [Min(1)] public int safeZoneInterval = 5;
    [Min(1)] public int superZoneInterval = 30;

    [Header("Zone Variants")]
    public ZoneConfig normalZone;
    public ZoneConfig safeZone;
    public ZoneConfig superZone;

    [Header("Tier Visuals")]
    public WheelThemeData bronzeTheme;
    public WheelThemeData silverTheme;
    public WheelThemeData goldTheme;
    [Min(1)] public int silverStartZone = 10;
    [Min(1)] public int goldStartZone   = 20;

    [Header("Spin Animation")]
    [Min(0.1f)] public float spinDuration = 3.5f;
    [Min(0f)] public float minFullRotations = 4f;
    [Min(0f)] public float maxFullRotations = 6f;

    [Header("Reward Popup")]
    [Min(0f)] public float rewardPopupShowDuration = 0.35f;
    [Min(0f)] public float rewardPopupHoldDuration = 1.0f;

    [Header("Revive")]
    [Min(0)] public int reviveCurrencyCost = 25;

    [Header("Currency IDs")]
    [SerializeField] public CurrencyConfig currencyConfig;

    [Header("Animation")]
    public WheelAnimationConfig animConfig;

    [Header("Exit Rules")]
    [SerializeField] private RunExitRules exitRules;
    public RunExitRules ExitRules => exitRules;

    public void ValidateRequiredReferences()
    {
        if (normalZone == null)
            throw new System.InvalidOperationException("WheelConfig: normalZone not assigned.");
        if (safeZone == null)
            throw new System.InvalidOperationException("WheelConfig: safeZone not assigned.");
        if (superZone == null)
            throw new System.InvalidOperationException("WheelConfig: superZone not assigned.");
        if (currencyConfig == null)
            throw new System.InvalidOperationException("WheelConfig: currencyConfig not assigned.");
        if (animConfig == null)
            throw new System.InvalidOperationException("WheelConfig: animConfig not assigned.");
        if (exitRules == null)
            throw new System.InvalidOperationException("WheelConfig: exitRules not assigned.");
    }

    public ZoneConfig PickZoneFor(int zoneIndex)
    {
        if (zoneIndex <= 0) return normalZone;

        if (superZoneInterval > 0 && zoneIndex % superZoneInterval == 0)
            return superZone;

        if (zoneIndex == 1)
            return safeZone;

        if (safeZoneInterval > 0 && zoneIndex % safeZoneInterval == 0)
            return safeZone;

        return normalZone;
    }

    public ZoneType GetZoneType(int zoneIndex)
    {
        ZoneConfig zc = PickZoneFor(zoneIndex);
        if (zc == null)
            throw new System.InvalidOperationException($"WheelConfig: zone config not assigned for zone {zoneIndex}.");
        return zc.type;
    }

}
