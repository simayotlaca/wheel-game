using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
public enum RewardFlyRoute
{
    RewardList,
    CompletedPuzzleToList,
    OverflowToList,
}

public class RewardFeedbackController : MonoBehaviour
{
    [SerializeField] private RunSession controller;
    [SerializeField] private WheelController wheel_controller;
    [SerializeField] private RewardListUI reward_list;
    [SerializeField] private MetaProgressPanel meta_progress_panel;
    [SerializeField] private RewardFlyIconPool reward_fly_pool;

    private bool last_meta_busy;
    private int pending_meta_preparations;
    private int pending_meta_flights;
    private int meta_prepare_cancel_version;

    private List<MetaProgressModel.MetaChunk> cascade_chunks;
    private int cascade_index;
    private Vector3 cascade_source;

    void OnEnable()
    {
        controller.OnRewardEarned += HandleRewardEarned;
        controller.OnDeathHit += ReleaseAll;
        controller.OnRunEnded += ReleaseAll;
        controller.OnRewardsBanked += ReleaseAll;
        meta_progress_panel.OnCardResolved += HandleCardResolved;
        meta_progress_panel.OnOverflowReady += HandleOverflowReady;
        meta_progress_panel.OnAnimationComplete += HandleMetaAnimationComplete;
        reward_fly_pool.OnAllComplete += HandleFlyPoolComplete;
    }

    void OnDisable()
    {
        controller.OnRewardEarned -= HandleRewardEarned;
        controller.OnDeathHit -= ReleaseAll;
        controller.OnRunEnded -= ReleaseAll;
        controller.OnRewardsBanked -= ReleaseAll;
        meta_progress_panel.OnCardResolved -= HandleCardResolved;
        meta_progress_panel.OnOverflowReady -= HandleOverflowReady;
        meta_progress_panel.OnAnimationComplete -= HandleMetaAnimationComplete;
        reward_fly_pool.OnAllComplete -= HandleFlyPoolComplete;
        ReleaseAll();
        controller.SetBusy(BusySource.Fly, false);
        controller.SetBusy(BusySource.Meta, false);
    }

    private void HandleFlyPoolComplete()
    {
        controller.SetBusy(BusySource.Fly, false);
    }

    private void HandleMetaAnimationComplete()
    {
        TryFinishMetaBusy();
    }

    void HandleRewardEarned(SpinResult result, ZoneRewardEntry entry, MetaProgressModel.ProgressAllocation alloc, RewardRouteInfo route)
    {
        if (entry != null && entry.reward != null && !string.IsNullOrEmpty(entry.reward.rewardId))
        {
            Vector3 source = transform.position;
            bool has_source = wheel_controller.TryGetSliceWorldPosition(result.slice_idx, out Vector3 slot_world);
            if (has_source)
            {
                source = slot_world;
            }

            if (route.defer_overflow_until_meta_complete && alloc.overflow_amount > 0 && alloc.meta_chunks.Count > 0)
            {
                int last_card_idx = alloc.meta_chunks[alloc.meta_chunks.Count - 1].card_index;
                meta_progress_panel.SetDeferredOverflow(last_card_idx, alloc.overflow_amount);
            }

            cascade_chunks = alloc.meta_chunks;
            cascade_source = source;
            cascade_index = 0;
            SpawnNextCascadeChunk();

            if (alloc.overflow_amount > 0 && !route.defer_overflow_until_meta_complete)
            {
                RewardFlyRoute overflow_route = route.is_tracked_by_meta
                    ? RewardFlyRoute.CompletedPuzzleToList
                    : RewardFlyRoute.RewardList;
                PlayRewardListFly(route.reward_for_reward_list, alloc.overflow_amount, source, overflow_route);
            }
        }
    }

    private void SpawnNextCascadeChunk()
    {
        if (cascade_chunks == null || cascade_index >= cascade_chunks.Count)
        {
            return;
        }
        MetaProgressModel.MetaChunk chunk = cascade_chunks[cascade_index];
        cascade_index++;
        PlayMetaProgressFly(chunk.card_index, chunk.amount, cascade_source);
    }

    private void HandleCardResolved(int card_idx)
    {
        SpawnNextCascadeChunk();
        TryFinishMetaBusy();
    }

    private void HandleOverflowReady(RewardDefinition reward, int delta, Vector3 src_world)
    {
        if (reward != null && !string.IsNullOrEmpty(reward.rewardId) && delta > 0)
        {
            PlayRewardListFly(reward, delta, src_world, RewardFlyRoute.OverflowToList);
        }
    }

    private void PlayRewardListFly(RewardDefinition reward, int amount, Vector3 src_world, RewardFlyRoute route)
    {
        if (reward == null || string.IsNullOrEmpty(reward.rewardId) || amount <= 0)
        {
            return;
        }

        if (!reward_list.TryGetFlyTarget(reward, out Vector3 target_world))
        {
            target_world = src_world;
        }

        Sprite sprite = ResolveBurstSprite(reward, route);
        bool started = reward_fly_pool.Play(sprite, src_world, target_world, amount, null);
        if (started)
        {
            controller.SetBusy(BusySource.Fly, true);
        }
        ApplyChunkImmediately(reward);
    }

    private void PlayMetaProgressFly(int card_idx, int amount, Vector3 source)
    {
        if (!last_meta_busy)
        {
            last_meta_busy = true;
            controller.SetBusy(BusySource.Meta, true);
        }

        StartCoroutine(PlayMetaProgressFlyAfterLayout(card_idx, amount, source, meta_prepare_cancel_version));
    }

    private IEnumerator PlayMetaProgressFlyAfterLayout(int card_idx, int amount, Vector3 source, int cancel_ver)
    {
        pending_meta_preparations++;

        bool prepared = meta_progress_panel.PrepareFlyTarget(card_idx);
        if (prepared)
        {
            yield return null;
        }

        if (cancel_ver != meta_prepare_cancel_version)
        {
            pending_meta_preparations = Mathf.Max(0, pending_meta_preparations - 1);
            TryFinishMetaBusy();
            yield break;
        }

        bool started = false;
        if (prepared && meta_progress_panel.TryGetFlyTarget(card_idx, out RectTransform puzzle_rt, out Sprite sprite))
        {
            int captured_card_idx = card_idx;
            started = reward_fly_pool.Play(sprite, source, puzzle_rt, amount, amt => OnPuzzleFlightArrived(captured_card_idx, amt));
            if (started)
            {
                pending_meta_flights++;
                controller.SetBusy(BusySource.Fly, true);
            }
        }

        if (!started)
        {
            ApplyMetaProgress(card_idx, amount);
        }

        pending_meta_preparations = Mathf.Max(0, pending_meta_preparations - 1);
        TryFinishMetaBusy();
    }

    private void OnPuzzleFlightArrived(int card_idx, int amount)
    {
        if (pending_meta_flights > 0)
        {
            pending_meta_flights--;
        }
        ApplyMetaProgress(card_idx, amount);
        TryFinishMetaBusy();
    }

    private void ApplyMetaProgress(int card_idx, int amount)
    {
        meta_progress_panel.AddProgressFromFlyAtIndex(card_idx, amount);
        meta_progress_panel.NotifyGainArrivedAtIndex(card_idx, amount);
    }

    private void ApplyChunkImmediately(RewardDefinition reward)
    {
        if (controller.Inventory.TryGetPending(reward, out int total))
        {
            reward_list.ApplyEarnedReward(reward, total);
        }
    }

    private Sprite ResolveBurstSprite(RewardDefinition reward, RewardFlyRoute route)
    {
        Sprite reward_sprite = reward.icon;
        if (reward.wheelIcon != null)
        {
            reward_sprite = reward.wheelIcon;
        }
        if (route == RewardFlyRoute.CompletedPuzzleToList
            || route == RewardFlyRoute.OverflowToList)
        {
            Sprite puzzle_sprite = meta_progress_panel.GetPuzzleSpriteForReward(reward);
            if (puzzle_sprite != null)
            {
                return puzzle_sprite;
            }
        }
        return reward_sprite;
    }

    void ReleaseAll()
    {
        meta_prepare_cancel_version++;
        pending_meta_preparations = 0;
        pending_meta_flights = 0;
        cascade_chunks = null;
        cascade_index = 0;
        if (last_meta_busy)
        {
            last_meta_busy = false;
            controller.SetBusy(BusySource.Meta, false);
        }

        reward_fly_pool.ReleaseAll();
        controller.SetBusy(BusySource.Fly, false);
        wheel_controller.ClearShine();
    }

    private void TryFinishMetaBusy()
    {
        if (last_meta_busy && pending_meta_preparations == 0 && pending_meta_flights == 0 && !meta_progress_panel.IsAnimating)
        {
            last_meta_busy = false;
            controller.SetBusy(BusySource.Meta, false);
        }
    }
}
}
