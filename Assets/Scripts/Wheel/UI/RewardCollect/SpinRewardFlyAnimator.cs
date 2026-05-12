using System;
using UnityEngine;

public partial class SpinRewardFlyAnimator : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private WheelView wheelView;
    [SerializeField] private RewardListUI rewardList;
    [SerializeField] private RectTransform flyContainer;

    [Header("Animation")]
    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private int poolCapacity = 32;

    private WheelAnimationConfig cfg;

    private float StartDelay          => cfg.spinFlyStartDelay;
    private float PostLandHoldSeconds => cfg.spinPostLandHold;
    private int   BurstMaxParticles   => cfg.burstMaxParticles;
    private float BurstStartSpeedMin  => cfg.burstStartSpeedMin;
    private float BurstStartSpeedMax  => cfg.burstStartSpeedMax;
    private float BurstSpreadDegrees  => cfg.burstSpreadDegrees;
    private float BurstDrag           => cfg.burstDrag;
    private float BurstStaggerStep    => cfg.burstStaggerStep;
    private float BurstLifetime       => cfg.burstParticleLifetime;
    private float BurstAttractorRamp  => cfg.burstAttractorRamp;
    private float BurstParticleSize   => cfg.burstParticleSize;
    private float BurstEndScale       => cfg.burstEndScale;
    private float BurstArrivalThresh  => cfg.burstArrivalThreshold;
    private float RowPunchScale       => cfg.rowArrivalPunchScale;
    private float RowPunchDuration    => cfg.rowArrivalPunchDuration;
    private float CountUpStartOffset  => cfg.countUpStartOffset;

    private RewardFlyIconPool pool;

    public bool IsBusy => AnyBurstActive();

    void Awake()
    {
        if (animConfig == null)
            throw new InvalidOperationException(
                "SpinRewardFlyAnimator: animConfig is not assigned — wire WheelAnimationConfig in inspector.");
        cfg = animConfig;
        Transform container = flyContainer != null ? flyContainer : transform;
        pool = new RewardFlyIconPool(container, Mathf.Max(GameRules.MaxParticles, poolCapacity));
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned += HandleRewardEarned;

            controller.OnDeathHit += ReleaseAll;
            controller.OnRunEnded += ReleaseAll;
            controller.OnRewardsBanked += ReleaseAll;
        }
        if (rewardList != null) rewardList.SetDeferredUpdate(true);
        if (pool != null) pool.DeactivateAll();
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnRewardEarned -= HandleRewardEarned;
            controller.OnDeathHit -= ReleaseAll;
            controller.OnRunEnded -= ReleaseAll;
            controller.OnRewardsBanked -= ReleaseAll;
        }
        if (rewardList != null) rewardList.SetDeferredUpdate(false);
        ReleaseAll();
    }

    void HandleRewardEarned(SpinResult result, SliceDefinition slice)
    {
        if (slice == null || slice.reward == null) return;
        string id = slice.reward.rewardId;
        if (string.IsNullOrEmpty(id)) return;

        if (wheelView == null || !wheelView.TryGetSliceWorldPosition(result.sliceIndex, out Vector3 slotWorld))
        { ApplyRowImmediately(slice.reward); return; }

        int idx = AcquireFreeBurst();
        if (idx < 0) { ApplyRowImmediately(slice.reward); return; }

        int delta = Mathf.Max(1, result.amount);
        int baseAmount = 0;
        int totalAmount = delta;
        if (controller != null && controller.Inventory != null
            && controller.Inventory.Pending.TryGetValue(id, out int pendingTotal))
        {
            totalAmount = pendingTotal;
            baseAmount = Mathf.Max(0, pendingTotal - delta);
        }

        RewardListItemUI rowItem = null;
        if (rewardList != null) rowItem = rewardList.ReserveOrGetRow(slice.reward);

        int n = Mathf.Clamp(BurstMaxParticles, 1, GameRules.MaxParticles);

        n = Mathf.Min(n, Mathf.Max(1, delta));

        bursts[idx].used = true;
        bursts[idx].phase = BurstPhase.Queued;
        bursts[idx].fireAt = Time.unscaledTime + StartDelay;
        bursts[idx].rewardId = id;
        bursts[idx].reward = slice.reward;
        bursts[idx].sprite = slice.reward.wheelIcon != null ? slice.reward.wheelIcon : slice.reward.icon;
        bursts[idx].sourceWorld = slotWorld;
        bursts[idx].rowItem = rowItem;
        bursts[idx].particleCount = n;
        bursts[idx].particlesSpawned = 0;
        bursts[idx].particlesLanded = 0;
        bursts[idx].delta = delta;
        bursts[idx].baseAmount = baseAmount;
        bursts[idx].totalAmount = totalAmount;
        bursts[idx].amountAwarded = baseAmount;
        bursts[idx].countUpStarted = false;
        bursts[idx].startedAt = 0f;
    }

    void ApplyRowImmediately(RewardDefinition reward)
    {
        if (rewardList == null || controller == null || controller.Inventory == null) return;
        if (reward == null || string.IsNullOrEmpty(reward.rewardId)) return;
        if (controller.Inventory.Pending.TryGetValue(reward.rewardId, out int total))
            rewardList.ApplyEarnedReward(reward, total);
    }

    void ApplyRowPartial(RewardDefinition reward, int runningAmount)
    {
        if (rewardList == null || reward == null) return;
        rewardList.ApplyEarnedReward(reward, runningAmount);
    }

    void Update()
    {
        if (pool == null) return;

        float now = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        TickAllParticles(dt);
        TickAllBursts(now);
    }

    void ReleaseAll()
    {
        if (pool == null) return;
        for (int p = 0; p < GameRules.MaxParticles; p++)
        {
            RewardFlyIcon icon = particles[p].icon;
            if (particles[p].used && icon != null) pool.Release(icon);
            particles[p].used = false;
            particles[p].icon = null;
        }
        for (int b = 0; b < GameRules.MaxSimultaneousBursts; b++) ClearBurst(b);
        if (wheelView != null) wheelView.UndimAll();
    }

}
