namespace VertigoWheel
{
internal enum RunState
{
    Ready,
    PostReviveLocked,
    Spinning,
    Reward,
    DeathGameOver
}

internal static class GameRules
{
    internal class StateMachine
    {
        internal RunState State { get; private set; }

        internal StateMachine(RunState initial_state)
        {
            State = initial_state;
        }

        internal bool TryChange(RunState next)
        {
            if (next == State || !CanTransition(State, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        internal bool Reset(RunState next)
        {
            if (next == State)
            {
                return false;
            }

            State = next;
            return true;
        }
    }

    internal static int NextZoneIndex(WheelConfig config, int current_zone)
    {
        return current_zone >= config.MaxZoneIndex ? config.FirstZoneIndex : current_zone + 1;
    }

    internal static bool CanLeave(RunState state)
    {
        return state == RunState.Ready;
    }

    internal static bool IsDeathFlowActive(RunState state, SpinResult last_result)
    {
        return state == RunState.DeathGameOver || (state == RunState.Reward && last_result.IsDeath);
    }

    internal static bool CanRevive(RunState state, SpinResult last_result)
    {
        return state == RunState.DeathGameOver
            && last_result.IsDeath
            && CanTransition(state, RunState.PostReviveLocked);
    }

    internal static bool CanCompleteRewardFeedback(RunState state, SpinResult last_result)
    {
        return !last_result.IsDeath && CanTransition(state, RunState.Ready);
    }

    internal static RewardSettlement ResolveRewardSettlement(
        SpinResult result,
        MetaProgressModel meta_progress_model)
    {
        ZoneRewardEntry entry = result.entry;
        MetaProgressModel.ProgressAllocation allocation = meta_progress_model.AllocateProgress(entry.reward, result.amount);

        if (allocation.overflow_amount <= 0)
        {
            return new RewardSettlement(allocation,
                null,
                0,
                reward_list_from_meta_overflow: false);
        }

        if (meta_progress_model.IsRewardTracked(entry.reward))
        {
            return new RewardSettlement(allocation,
                meta_progress_model.OverflowReward,
                allocation.overflow_amount,
                reward_list_from_meta_overflow: true);
        }

        return new RewardSettlement(allocation,
            entry.reward,
            allocation.overflow_amount,
            reward_list_from_meta_overflow: false);
    }

    internal static bool CanTransition(RunState from, RunState to)
    {
        switch (from)
        {
            case RunState.Ready:
                return to == RunState.Spinning;

            case RunState.PostReviveLocked:
                return to == RunState.Spinning;

            case RunState.Spinning:
                return to == RunState.Reward;

            case RunState.Reward:
                return to == RunState.Ready
                    || to == RunState.DeathGameOver;

            case RunState.DeathGameOver:
                return to == RunState.PostReviveLocked;

            default:
                return false;
        }
    }

    internal static WheelVisual GetZoneVisual(WheelConfig config, int zone_idx)
    {
        if (zone_idx % config.superZoneInterval == 0)
        {
            return config.superZone;
        }

        if (zone_idx == config.FirstZoneIndex)
        {
            return config.safeZone;
        }

        if (zone_idx % config.safeZoneInterval == 0)
        {
            return config.safeZone;
        }

        return config.normalZone;
    }

    internal static RewardTier GetZoneTier(WheelConfig config, int zone_idx)
    {
        return GetZoneVisual(config, zone_idx).tier;
    }
}

internal struct RewardSettlement
{
    internal MetaProgressModel.ProgressAllocation allocation;
    internal RewardDefinition reward_list_reward;
    internal int reward_list_amount;
    internal bool reward_list_from_meta_overflow;

    internal RewardSettlement(
        MetaProgressModel.ProgressAllocation allocation,
        RewardDefinition reward_list_reward,
        int reward_list_amount,
        bool reward_list_from_meta_overflow)
    {
        this.allocation = allocation;
        this.reward_list_reward = reward_list_reward;
        this.reward_list_amount = reward_list_amount;
        this.reward_list_from_meta_overflow = reward_list_from_meta_overflow;
    }

    internal bool HasRewardListReward
    {
        get
        {
            return reward_list_amount > 0;
        }
    }
}
}
