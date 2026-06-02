namespace VertigoWheel
{
public static class GameRules
{
    public static int NextZoneIndex(WheelConfig config, int current_zone)
    {
        return current_zone >= config.MaxZoneIndex ? config.FirstZoneIndex : current_zone + 1;
    }

    public static bool CanSpin(RunState state, bool busy)
    {
        return (state == RunState.Ready || state == RunState.PostReviveLocked) && !busy;
    }

    public static bool CanLeave(RunState state, bool busy)
    {
        return state == RunState.Ready && !busy;
    }

    public static bool IsTransitioningToDeath(RunState state, SpinResult last_result)
    {
        return state == RunState.DeathGameOver || (state == RunState.Reward && last_result.is_death);
    }

    public static bool CanRevive(RunState state, SpinResult last_result, bool can_afford)
    {
        return IsTransitioningToDeath(state, last_result) && can_afford;
    }

    public static ExitKind ClassifyExit(RunSession controller)
    {
        if (controller.State != RunState.Ready || controller.IsBusy)
        {
            return ExitKind.None;
        }
        if (!controller.Zones.CanExitCurrentZone)
        {
            return ExitKind.None;
        }

        bool has_pending = controller.Inventory.HasPending;
        if (has_pending || controller.HasUnbankedMetaProgress)
        {
            return ExitKind.Collect;
        }
        return ExitKind.FreshStart;
    }

    public static ExitVisibility ResolveExitVisibility(RunSession controller)
    {
        if (controller.IsTransitioningToDeath)
        {
            return ExitVisibility.Hidden;
        }
        if (controller.State != RunState.Ready || controller.IsBusy)
        {
            return ExitVisibility.Disabled;
        }
        if (!controller.Zones.CanExitCurrentZone)
        {
            return ExitVisibility.Disabled;
        }
        return ExitVisibility.Normal;
    }

    public static RewardRouteInfo BuildRewardRoute(MetaProgressModel meta, ZoneRewardEntry entry, MetaProgressModel.ProgressAllocation alloc)
    {
        RewardRouteInfo route = default;
        route.is_tracked_by_meta = meta.IsRewardTracked(entry.reward);
        route.defer_overflow_until_meta_complete = route.is_tracked_by_meta && alloc.meta_chunks.Count > 0;
        route.reward_for_reward_list = route.is_tracked_by_meta ? meta.OverflowReward : entry.reward;
        return route;
    }

    public static bool CanExitZone(RewardTier tier, RunExitRules exit_rules)
    {
        return tier == RewardTier.Safe || tier == RewardTier.Super || exit_rules.AllowExitOnNormalZones;
    }

    public static int ResolveRewardListPriority(
        RewardDefinition reward,
        CurrencyConfig currency_config,
        RewardTableConfig.CategorySortPriorities priorities)
    {
        if (priorities == null)
        {
            return 99;
        }
        if (reward == null)
        {
            return priorities.priority_default;
        }
        if (currency_config != null && reward == currency_config.goldReward)
        {
            return priorities.priority_gold;
        }

        switch (reward.slotCategory)
        {
            case SlotCategory.Currency: return priorities.priority_cash;
            case SlotCategory.Special:  return priorities.priority_special;
            case SlotCategory.AllCards: return priorities.priority_all_cards;
            case SlotCategory.Other:    return priorities.priority_other;
            case SlotCategory.Death:    return priorities.priority_death;
            default:                    return priorities.priority_default;
        }
    }

    public static WheelVisual GetZoneVisual(WheelConfig config, int zone_idx)
    {
        //super wins here, because zone 30 is also safe and i wanted it to look special
        if (config.superZoneInterval > 0 && zone_idx % config.superZoneInterval == 0)
        {
            return config.superZone;
        }

        if (zone_idx == config.FirstZoneIndex)
        {
            return config.safeZone;
        }

        if (config.safeZoneInterval > 0 && zone_idx % config.safeZoneInterval == 0)
        {
            return config.safeZone;
        }

        return config.normalZone;
    }

    public static RewardTier GetZoneTier(WheelConfig config, int zone_idx)
    {
        return GetZoneVisual(config, zone_idx).tier;
    }
}
}
