using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
internal class RewardAnimationSequence
{
    private WheelController wheel_controller;
    private RewardListUI reward_list;
    private MetaProgressPanel meta_progress_panel;
    private RewardFlyIconPool reward_fly_pool;
    private Action<RewardDefinition, int, Action> on_reward_list_arrived;
    private Action on_sequence_completed;

    private List<FeedbackStep> feedback_steps = new List<FeedbackStep>();
    private FeedbackStep active_step;
    private Vector3 feedback_source_world;
    private int next_step_idx;
    private bool is_running;

    private enum FeedbackStepKind
    {
        MetaProgress,
        RewardList,
    }

    private enum RewardFlyRoute
    {
        DirectToList,
        OverflowToList,
    }

    private class FeedbackStep
    {
        internal FeedbackStepKind kind;
        internal int card_index;
        internal int amount;
        internal MetaCompletionKind completion_kind;
        internal RewardDefinition reward;
        internal RewardFlyRoute route;
        internal Vector3 source_world;
    }

    internal RewardAnimationSequence(
        WheelController wheel_controller,
        RewardListUI reward_list,
        MetaProgressPanel meta_progress_panel,
        RewardFlyIconPool reward_fly_pool,
        Action<RewardDefinition, int, Action> on_reward_list_arrived,
        Action on_sequence_completed)
    {
        this.wheel_controller = wheel_controller;
        this.reward_list = reward_list;
        this.meta_progress_panel = meta_progress_panel;
        this.reward_fly_pool = reward_fly_pool;
        this.on_reward_list_arrived = on_reward_list_arrived;
        this.on_sequence_completed = on_sequence_completed;
    }

    internal void Begin(SpinResult result, RewardSettlement settlement)
    {
        Vector3 source = wheel_controller.GetSliceWorldPosition(result.slice_idx);
        BuildFeedbackSteps(settlement, source);
        active_step = null;
        next_step_idx = 0;
        is_running = true;

        ContinueSequence();
    }

    private void HandleCardResolved(int card_index, Vector3 source_world)
    {
        if (active_step != null && active_step.kind == FeedbackStepKind.MetaProgress && active_step.card_index == card_index)
        {
            UpdateNextRewardListSource(source_world);
            CompleteActiveStep();
        }
    }

    private void HandleMetaAnimationComplete()
    {
        if (active_step != null && active_step.kind == FeedbackStepKind.MetaProgress && active_step.completion_kind == MetaCompletionKind.None)
        {
            CompleteActiveStep();
        }
    }

    internal void Cancel()
    {
        feedback_steps.Clear();
        active_step = null;
        next_step_idx = 0;
        is_running = false;
        reward_fly_pool.CancelAll();
    }

    private void BuildFeedbackSteps(RewardSettlement settlement, Vector3 source)
    {
        feedback_steps.Clear();
        feedback_source_world = source;

        for (int i = 0; i < settlement.allocation.meta_chunks.Count; i++)
        {
            MetaProgressModel.MetaChunk chunk = settlement.allocation.meta_chunks[i];
            feedback_steps.Add(CreateMetaProgressStep(chunk));
        }

        if (settlement.HasRewardListReward)
        {
            if (settlement.reward_list_from_meta_overflow && feedback_steps.Count > 0)
            {
                FeedbackStep last_step = feedback_steps[feedback_steps.Count - 1];
                last_step.completion_kind = MetaCompletionKind.ConvertsToOverflow;
                feedback_steps.Add(CreateRewardListStep(
                    settlement.reward_list_reward,
                    settlement.reward_list_amount,
                    RewardFlyRoute.OverflowToList,
                    source));
            }
            else
            {
                feedback_steps.Add(CreateRewardListStep(
                    settlement.reward_list_reward,
                    settlement.reward_list_amount,
                    RewardFlyRoute.DirectToList,
                    source));
            }
        }
    }

    private static FeedbackStep CreateMetaProgressStep(MetaProgressModel.MetaChunk chunk)
    {
        return new FeedbackStep
        {
            kind = FeedbackStepKind.MetaProgress,
            card_index = chunk.card_index,
            amount = chunk.amount,
            completion_kind = chunk.completes_card ? MetaCompletionKind.SkinReady : MetaCompletionKind.None,
        };
    }

    private static FeedbackStep CreateRewardListStep(
        RewardDefinition reward,
        int amount,
        RewardFlyRoute route,
        Vector3 source_world)
    {
        return new FeedbackStep
        {
            kind = FeedbackStepKind.RewardList,
            reward = reward,
            amount = amount,
            route = route,
            source_world = source_world,
        };
    }

    private void CompleteActiveStep()
    {
        active_step = null;
        ContinueSequence();
    }

    private void ContinueSequence()
    {
        if (!is_running || active_step != null)
        {
            return;
        }

        if (next_step_idx >= feedback_steps.Count)
        {
            is_running = false;
            on_sequence_completed?.Invoke();
            return;
        }

        StartNextFeedbackStep();
    }

    private void StartNextFeedbackStep()
    {
        active_step = feedback_steps[next_step_idx];
        next_step_idx++;
        if (active_step.kind == FeedbackStepKind.MetaProgress)
        {
            PlayMetaProgressFly(active_step, feedback_source_world);
        }
        else
        {
            PlayRewardListFly(active_step);
        }
    }

    private void UpdateNextRewardListSource(Vector3 source_world)
    {
        if (next_step_idx < feedback_steps.Count)
        {
            FeedbackStep next_step = feedback_steps[next_step_idx];
            if (next_step.kind == FeedbackStepKind.RewardList
                && next_step.route == RewardFlyRoute.OverflowToList)
            {
                next_step.source_world = source_world;
            }
        }
    }

    private void PlayMetaProgressFly(FeedbackStep step, Vector3 source)
    {
        meta_progress_panel.PrepareFlyTarget(step.card_index);

        if (meta_progress_panel.TryGetFlyTarget(step.card_index, out RectTransform puzzle_rt, out Sprite sprite))
        {
            reward_fly_pool.Play(sprite, source, puzzle_rt, step.amount, HandlePuzzleFlightArrived);
            return;
        }

        ApplyMetaProgress(step);
    }

    private void HandlePuzzleFlightArrived(int amount)
    {
        if (active_step != null && active_step.kind == FeedbackStepKind.MetaProgress)
        {
            active_step.amount = amount;
            ApplyMetaProgress(active_step);
        }
    }

    private void ApplyMetaProgress(FeedbackStep step)
    {
        if (meta_progress_panel.AddProgressFromFlyAtIndex(
            step.card_index,
            step.amount,
            step.completion_kind,
            HandleMetaAnimationComplete,
            HandleCardResolved))
        {
            return;
        }

        CompleteActiveStep();
    }

    private void PlayRewardListFly(FeedbackStep step)
    {
        Sprite sprite = step.reward.ResolveWheelIcon();
        reward_list.PrepareFlyTarget(step.reward);
        RectTransform target_rt = reward_list.GetFlyTarget(step.reward);
        reward_fly_pool.Play(sprite, step.source_world, target_rt, step.amount, HandleRewardListFlightArrived);
    }

    private void HandleRewardListFlightArrived(int amount)
    {
        if (active_step != null && active_step.kind == FeedbackStepKind.RewardList)
        {
            active_step.amount = amount;
            on_reward_list_arrived.Invoke(active_step.reward, active_step.amount, HandleRewardListCountComplete);
        }
    }

    private void HandleRewardListCountComplete()
    {
        if (active_step != null && active_step.kind == FeedbackStepKind.RewardList)
        {
            CompleteActiveStep();
        }
    }

}
}
