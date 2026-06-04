using System;
using UnityEngine;

namespace VertigoWheel
{
[Serializable]
public struct CurrencyHudTiming
{
    [Min(0f)] public float currency_count_up_duration;
}

[Serializable]
public struct RewardListItemTiming
{
    [Min(0f)] public float reward_amount_count_up_duration;
    [Min(0f)] public float reward_text_tick_pulse_strength;
    [Min(0f)] public float reward_text_tick_pulse_duration;
    [Min(0f)] public float reward_text_tick_pulse_min_interval;
}

[Serializable]
public struct ExitFlowTiming
{
    [Min(0f)] public float death_panel_show_duration;
}

[CreateAssetMenu(fileName = "FeedbackConfig", menuName = "Vertigo Wheel/Config/Feedback Config")]
public class FeedbackConfig : ScriptableObject
{
    [Header("Currency HUD")]
    public CurrencyHudTiming currencyHudTiming;

    [Header("Reward List")]
    public RewardListItemTiming rewardListItemTiming;

    [Header("Exit Flow")]
    public ExitFlowTiming exitFlowTiming;
}
}
