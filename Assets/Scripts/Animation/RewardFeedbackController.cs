using System;
using UnityEngine;

namespace VertigoWheel
{
public class RewardFeedbackController : MonoBehaviour
{
    private enum FeedbackState
    {
        Disabled,
        Listening,
        HandlingReward,
    }

    [SerializeField] private RunSession controller;
    [SerializeField] private WheelController wheel_controller;
    [SerializeField] private RewardListUI reward_list;
    [SerializeField] private MetaProgressPanel meta_progress_panel;
    [SerializeField] private RewardFlyIconPool reward_fly_pool;

    private FeedbackState state = FeedbackState.Disabled;
    private RewardAnimationSequence animation_sequence;

    void Awake()
    {
        animation_sequence = CreateAnimationSequence();
    }

    private RewardAnimationSequence CreateAnimationSequence()
    {
        return new RewardAnimationSequence(
            wheel_controller,
            reward_list,
            meta_progress_panel,
            reward_fly_pool,
            HandleRewardListArrival,
            HandleRewardAnimationCompleted);
    }

    void OnEnable()
    {
        state = FeedbackState.Listening;
    }

    void OnDisable()
    {
        state = FeedbackState.Disabled;
        animation_sequence.Cancel();
    }

    internal void PlayReward(
        SpinResult result,
        RewardSettlement settlement)
    {
        if (!BeginReward())
        {
            return;
        }

        animation_sequence.Begin(result, settlement);
    }

    private void HandleRewardAnimationCompleted()
    {
        if (state == FeedbackState.HandlingReward)
        {
            state = FeedbackState.Listening;
            controller.NotifyRewardFeedbackCompleted();
        }
    }

    private void HandleRewardListArrival(RewardDefinition reward, int amount, Action count_complete)
    {
        int total = controller.ApplyRewardListArrival(reward, amount);
        reward_list.ApplyEarnedReward(reward, total, count_complete);
    }

    private bool BeginReward()
    {
        if (state != FeedbackState.Listening)
        {
            return false;
        }

        state = FeedbackState.HandlingReward;
        return true;
    }
}
}
