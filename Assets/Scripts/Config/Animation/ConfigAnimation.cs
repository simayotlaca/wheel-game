using UnityEngine;

namespace VertigoWheel
{
[System.Serializable]
public struct WheelSliceVisualLayout
{
    public Vector2 iconSize;
    public Vector2 iconAnchoredPos;
    public Vector2 amountAnchoredPos;
    public float amountFontSize;
}

[System.Serializable]
public struct WheelSliceLayoutRule
{
    public string rewardId;
    public string visualFamily;
    public RewardVisualCategory[] visualCategories;
    public WheelSliceVisualLayout layout;

    public bool Matches(RewardDefinition reward)
    {
        if (reward == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(rewardId))
        {
            return reward.rewardId == rewardId;
        }

        if (!string.IsNullOrEmpty(visualFamily))
        {
            return reward.visualFamily == visualFamily;
        }

        if (visualCategories != null)
        {
            for (int i = 0; i < visualCategories.Length; i++)
            {
                if (reward.visualCategory == visualCategories[i])
                {
                    return true;
                }
            }
        }

        return false;
    }
}

[System.Serializable]
public struct RewardListVisualLayout
{
    public Vector2 iconSize;
    public Vector2 iconAnchoredPos;
    public Vector2 amountAnchoredPos;
    public float amountFontSize;
}

[System.Serializable]
public struct RewardListLayoutRule
{
    public string reward_id;
    public string visual_family;
    public RewardListVisualLayout layout;

    public bool Matches(RewardDefinition reward)
    {
        if (reward == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(reward_id))
        {
            return reward.rewardId == reward_id;
        }

        if (!string.IsNullOrEmpty(visual_family))
        {
            return reward.visualFamily == visual_family;
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "ConfigAnimation", menuName = "Vertigo Wheel/Config/Animation Config")]
public class ConfigAnimation : ScriptableObject
{
    [Header("Wheel Spin")]
    public AnimationCurve wheelSpinCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 3f, 3f),
        new Keyframe(0.5f, 0.85f, 0.6f, 0.6f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Wheel Slice Layout")]
    public WheelSliceVisualLayout wheelSliceDefaultLayout;
    public WheelSliceLayoutRule[] wheelSliceLayoutRules;

    [Header("Slice Dim (non-winner fade during reveal)")]
    [Min(0f)] public float dimInDuration = 0.25f;
    [Min(0f)] public float dimOutDuration = 0.55f;
    [Range(0f, 1f)] public float dimMaxAlpha = 0.55f;
    public Color dimTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Slice Glow (winner halo)")]
    [Min(0f)] public float glowSpeed = 8f;
    [Range(0f, 1f)] public float glowMaxAlpha = 0.85f;
    [Min(0f)] public float glowHoldSeconds = 0.45f;

    [Header("Indicator Kick")]
    [Range(2f, 30f)] public float kickAngle = 15f;
    [Min(0.02f)] public float kickDuration = 0.08f;

    [Header("Reward List Layout")]
    public RewardListVisualLayout reward_list_default_layout;
    public RewardListLayoutRule[] reward_list_layout_rules;

    [Header("Currency HUD")]
    [Min(0f)] public float currencyCountUpDuration = 0.22f;

    private const float RewardTextTickPulsePunchCurvePeakFallback = 0.58f;

    [Header("Reward Amount Text")]
    [Min(0f)] public float rewardAmountCountUpDuration = 0.4f;
    [Range(1f, 1.15f)] public float rewardTextTickPulseScale = 1.04f;
    [Range(0.01f, 1f)] public float rewardTextTickPulsePunchCurvePeak = RewardTextTickPulsePunchCurvePeakFallback;
    [Min(0f)] public float rewardTextTickPulseDuration = 0.09f;
    [Min(0f)] public float rewardTextTickPulseMinInterval = 0.10f;

    [Header("Meta Progress Cards")]
    [Min(0f)] public float metaCardActivateFadeTime = 0.18f;
    [Range(0.1f, 1f)] public float metaCardActivateStartScale = 0.96f;
    [Min(0f)] public float metaCardPopupFadeIn = 0.15f;
    [Min(0f)] public float metaCardPopupHold = 0.60f;
    [Min(0f)] public float metaCardPopupFadeOut = 0.15f;
    [Min(0f)] public float metaCardPuzzlePunchScale = 0.2f;
    [Min(0f)] public float metaCardPuzzlePunchDuration = 0.4f;
    public Color metaCardProgressIdleColor = Color.white;
    [Min(0f)] public float metaFillDuration = 0.35f;
    [Min(0f)] public float metaCountDuration = 0.4f;

    [Header("Exit Flow")]
    [Range(0f, 1f)] public float exitDisabledAlpha = 0.4f;
    [Min(0f)] public float exitDisabledFadeDuration = 0.18f;

    [Header("Death Panel")]
    [Min(0f)] public float deathPanelShowDuration = 0.22f;
    [Range(0f, 1f)] public float reviveDisabledAlpha = 0.5f;

    [Header("Stack Fly")]
    public RewardFlyIcon stackFlyIconPrefab;
    [Range(1, 12)] public int stackFlyMinVisibleIcons = 5;
    [Range(1, 12)] public int stackFlyMaxVisibleIcons = 5;
    [Min(0.05f)] public float stackFlyDuration = 0.5f;
    public bool stackFlyUseUnscaledTime = true;
    [Range(0f, 1f)] public float stackFlySpreadEndT = 0.2f;
    [Range(0f, 1f)] public float stackFlyTravelStartT = 0.35f;
    public PrimeTween.Ease stackFlySpreadEase = PrimeTween.Ease.OutCubic;
    public PrimeTween.Ease stackFlyMoveEase = PrimeTween.Ease.InOutCubic;
    public PrimeTween.Ease stackFlyMergeEase = PrimeTween.Ease.InOutCubic;
    [Min(0f)] public float stackFlyHorizontalSpacing = 36f;
    [Min(0f)] public float stackFlyVerticalArc = 16f;
    [Min(0f)] public float stackFlyDownStep = 10f;
    [Min(0f)] public float stackFlySideJitter = 10f;
    public Vector2 stackFlyIconSize = new Vector2(110f, 110f);
    public Color stackFlyIconTint = Color.white;
    [Range(0.1f, 1.5f)] public float stackFlyEndScale = 1f;
    [Range(0f, 1f)] public float stackFlyFadeStartT = 1f;
    [Range(1, 8)] public int stackFlyMaxConcurrentFlights = 4;

    public WheelSliceVisualLayout ResolveWheelSliceLayout(RewardDefinition reward)
    {
        if (wheelSliceLayoutRules != null)
        {
            for (int i = 0; i < wheelSliceLayoutRules.Length; i++)
            {
                if (wheelSliceLayoutRules[i].Matches(reward))
                {
                    return wheelSliceLayoutRules[i].layout;
                }
            }
        }
        return wheelSliceDefaultLayout;
    }

    public RewardListVisualLayout ResolveRewardListLayout(RewardDefinition reward)
    {
        if (reward_list_layout_rules != null)
        {
            for (int i = 0; i < reward_list_layout_rules.Length; i++)
            {
                if (reward_list_layout_rules[i].Matches(reward))
                {
                    return reward_list_layout_rules[i].layout;
                }
            }
        }
        return reward_list_default_layout;
    }

    public float ResolveRewardTextTickPunchStrength()
    {
        float punch_curve_peak = rewardTextTickPulsePunchCurvePeak > 0f
            ? rewardTextTickPulsePunchCurvePeak
            : RewardTextTickPulsePunchCurvePeakFallback;
        return Mathf.Max(0f, (rewardTextTickPulseScale - 1f) / punch_curve_peak);
    }
}
}
