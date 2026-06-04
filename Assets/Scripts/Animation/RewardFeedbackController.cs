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
    private RunEventPass event_pass;

    void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
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
        event_pass.Subscribe<RunPendingClearedEvent>(HandlePendingCleared);
        event_pass.Subscribe<RunDeathHitEvent>(HandleDeathHit);
    }

    void OnDisable()
    {
        event_pass.ReleaseAll();
        state = FeedbackState.Disabled;
        animation_sequence.Cancel();
    }

    private void HandlePendingCleared(RunPendingClearedEvent _)
    {
        EnterCleanupState();
    }

    private void HandleDeathHit(RunDeathHitEvent _)
    {
        EnterCleanupState();
    }

    private void EnterCleanupState()
    {
        animation_sequence.Cancel();

        if (isActiveAndEnabled)
        {
            state = FeedbackState.Listening;
        }
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
