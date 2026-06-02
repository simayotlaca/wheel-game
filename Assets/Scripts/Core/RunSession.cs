using System;
using System.Collections;
using UnityEngine;

namespace VertigoWheel
{
public enum RunState
{
    Ready,
    PostReviveLocked,
    Spinning,
    Landing,
    Reward,
    DeathGameOver
}

public class RunSession : MonoBehaviour
{
    #region setup
    [Header("Config")]
    [SerializeField] private WheelConfig config;

    [Header("RNG (0 = time-based)")]
    [SerializeField] private int seed = 0;

    [Header("Scene References")]
    [SerializeField] private WheelController wheel_controller;

    private WheelResultPicker result_picker;
    private ZoneManager zone_manager;
    private CurrencyWallet inventory;
    private ReviveSystem revive_system;
    private MetaProgressModel meta_progress_model;
    private CurrencyRules currency_rules;

    private RunState current_state;
    private Coroutine state_transition_routine;

    private SpinResult last_result;

    private BusySource active_busy_sources;

    public event Action<int> OnZoneChanged;
    public event Action<RunState> OnStateChanged;
    public event Action OnBusyChanged;
    public event Action<SpinResult, ZoneRewardEntry, MetaProgressModel.ProgressAllocation, RewardRouteInfo> OnRewardEarned;
    public event Action OnDeathHit;
    public event Action OnRewardsBanked;

    public event Action OnRunEnded;

    public event Action OnRevived;

    public WheelConfig Config => config;

    public RunState State => current_state;

    public ZoneManager Zones => zone_manager;

    public CurrencyWallet Inventory => inventory;

    public MetaProgressModel MetaProgress => meta_progress_model;

    public bool CanSpin
    {
        get
        {
            return GameRules.CanSpin(current_state, IsBusy);
        }
    }

    public bool CanLeave => GameRules.CanLeave(current_state, IsBusy);

    public bool IsBusy => active_busy_sources != BusySource.None;

    public void SetBusy(BusySource source, bool on)
    {
        if (source == BusySource.None)
        {
            return;
        }

        bool was_busy = IsBusy;

        if (on)
        {
            active_busy_sources |= source;
        }
        else
        {
            active_busy_sources &= ~source;
        }

        if (IsBusy != was_busy)
            OnBusyChanged?.Invoke();
    }

    public bool HasUnbankedMetaProgress => meta_progress_model.HasUnbankedProgress;

    public int CurrentReviveCost => revive_system.CurrentCost;

    public bool CanAffordRevive()
    {
        return revive_system.CanAfford(inventory);
    }
    #endregion

    void Awake()
    {
        if (wheel_controller == null)
        {
            Debug.LogError("no WheelController assigned; RunSession drives the wheel through it");
        }

        currency_rules = new CurrencyRules(config.currency_config);
        zone_manager = new ZoneManager(config);
        inventory = new CurrencyWallet(config.currency_config, config.rewardTable, config.metaProgressConfig);
        result_picker = new WheelResultPicker(seed, currency_rules, config.rewardTable);
        revive_system = new ReviveSystem(currency_rules);

        current_state = RunState.Ready;

        LoadOrSeedInventory();

        if (config.metaProgressConfig == null)
        {
            throw new System.InvalidOperationException("WheelConfig.metaProgressConfig is not assigned; set the MetaProgressConfig asset on it");
        }
        meta_progress_model = new MetaProgressModel(config.metaProgressConfig.ConvertedArray, config.metaProgressConfig.overflowReward, PlayerProgress.LoadProgression());
    }

    void Start()
    {
        RefreshZoneView();
    }

    private void LoadOrSeedInventory()
    {
        CurrencyConfig currency_cfg = config.currency_config;
        if (currency_cfg.resetSaveOnLaunch)
        {
            PlayerProgress.Clear();
        }

        if (PlayerProgress.Load(out int saved_cash, out int saved_gold, out var saved_banked, out int saved_revives))
        {
            Inventory.RestoreFrom(saved_cash, saved_gold, saved_banked);
            revive_system.RestoreCount(saved_revives);
        }
        else
        {
            Inventory.RestoreFrom(currency_cfg.initialCash, currency_cfg.initialGold, null);
        }
    }

    private void ChangeState(RunState next)
    {
        if (next != current_state)
        {
            StopStateTransition();
            current_state = next;
            OnStateChanged?.Invoke(current_state);
            ScheduleStateTransition(current_state);
        }
    }

    private void ScheduleStateTransition(RunState state)
    {
        if (state == RunState.Landing)
        {
            state_transition_routine = StartCoroutine(WaitThenRun(config.rewardPopupShowDuration, FinishLanding));
        }
        else if (state == RunState.Reward)
        {
            state_transition_routine = StartCoroutine(WaitThenRun(config.rewardPopupHoldDuration, FinishReward));
        }
    }

    private IEnumerator WaitThenRun(float delay, Action callback)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        else
        {
            yield return null;
        }

        state_transition_routine = null;
        callback?.Invoke();
    }

    private void StopStateTransition()
    {
        if (state_transition_routine != null)
        {
            StopCoroutine(state_transition_routine);
            state_transition_routine = null;
        }
    }

    private void FinishLanding()
    {
        ChangeState(RunState.Reward);
        ApplySpinResult();
    }

    private void FinishReward()
    {
        if (last_result.is_death)
        {
            ChangeState(RunState.DeathGameOver);
        }
        else
        {
            ChangeState(RunState.Ready);
            AdvanceZone();
        }
    }

    public void RequestSpin()
    {
        if (!CanSpin)
        {
            return;
        }
        SpinResult result = result_picker.Spin();
        if (!result.is_valid)
        {
            Debug.LogError("wheel spin requested but no valid slots loaded, config invariant broken");
            return;
        }
        last_result = result;

        ChangeState(RunState.Spinning);

        wheel_controller.SpinTo(
            result.slice_idx,
            config.spinDuration,
            config.minSpinDurationSeconds,
            config.minFullRotations,
            config.maxFullRotations,
            OnSpinAnimationComplete);
    }

    public bool RequestLeave()
    {
        if (CanLeave)
        {
            Inventory.BankPending();
            meta_progress_model.Commit();
            OnRewardsBanked?.Invoke();
            Zones.Reset();
            RefreshZoneView();
            ChangeState(RunState.Ready);
            PersistProgress();
            return true;
        }
        return false;
    }

    public bool IsTransitioningToDeath
    {
        get
        {
            return GameRules.IsTransitioningToDeath(current_state, last_result);
        }
    }

    public bool TryRevive()
    {
        if (GameRules.CanRevive(current_state, last_result, CanAffordRevive()) && revive_system.TryRevive(inventory))
        {
            PersistProgress();
            WheelVisual revive_zone = config.safeZone;
            if (!LoadWheel(revive_zone, Zones.CurrentZone))
            {
                return false;
            }

            ChangeState(RunState.PostReviveLocked);
            OnRevived?.Invoke();
            return true;
        }
        return false;
    }

    public void Restart()
    {
        revive_system.Reset();
        Inventory.ClearPending();
        Zones.Reset();
        meta_progress_model.Revert();
        OnRunEnded?.Invoke();

        RefreshZoneView();
        ChangeState(RunState.Ready);
        PersistProgress();
    }

    private void PersistProgress()
    {
        PlayerProgress.Save(inventory.Cash, inventory.Gold, inventory.BankedForSave, revive_system.ReviveCount);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PersistProgress();
        }
    }

    void OnApplicationQuit()
    {
        PersistProgress();
    }

    private void OnSpinAnimationComplete()
    {
        wheel_controller.HighlightSlice(last_result.slice_idx);
        wheel_controller.ShineSlice(last_result.slice_idx);
        ChangeState(RunState.Landing);
    }

    #region Reward Progress

    //i split the reward here, meta gets first chance if it tracks this reward
    //anything extra goes to reward list later, otherwise the reward goes straight there
    //defer_overflow just means wait for the card fill before flying the leftover
    private void ApplySpinResult()
    {
        WheelResultPicker.ComputedSlot slot = result_picker.GetSlot(last_result.slice_idx);

        if (last_result.is_death)
        {
            OnDeathHit?.Invoke();
        }
        else
        {
            ZoneRewardEntry entry = slot.entry;
            MetaProgressModel.ProgressAllocation alloc = meta_progress_model.AllocateProgress(entry.reward, last_result.amount);
            RewardRouteInfo route = GameRules.BuildRewardRoute(meta_progress_model, entry, alloc);
            if (alloc.overflow_amount > 0)
            {
                Inventory.AddPending(route.reward_for_reward_list, alloc.overflow_amount);
            }

            OnRewardEarned?.Invoke(last_result, slot.entry, alloc, route);
        }
    }

    #endregion

    private void AdvanceZone()
    {
        Zones.Advance();
        RefreshZoneView();
    }

    private void RefreshZoneView()
    {
        WheelVisual zone = Zones.CurrentZoneVisual;

        if (!LoadWheel(zone, Zones.CurrentZone))
        {
            return;
        }
        OnZoneChanged?.Invoke(Zones.CurrentZone);
    }

    private bool LoadWheel(WheelVisual zone, int zone_idx)
    {
        wheel_controller.ClearShine();
        if (!result_picker.LoadZone(zone, zone_idx))
        {
            return false;
        }
        wheel_controller.BuildForZone(zone, result_picker.WheelSlots);
        return true;
    }
}

[Flags]
public enum BusySource
{
    None = 0,
    Meta = 1 << 0,
    Fly = 1 << 1,
}

public struct RewardRouteInfo
{
    public bool is_tracked_by_meta;
    public bool defer_overflow_until_meta_complete;
    public RewardDefinition reward_for_reward_list;
}

public class ZoneManager
{
    private readonly WheelConfig config;
    private int current_zone;

    public int CurrentZone => current_zone;

    public int FirstZoneIndex => config.FirstZoneIndex;

    public int MaxZoneIndex => config.MaxZoneIndex;

    public WheelVisual CurrentZoneVisual => GetZoneVisual(current_zone);

    public RewardTier CurrentZoneTier => GetZoneTier(current_zone);

    public bool CanExitCurrentZone
    {
        get
        {
            return GameRules.CanExitZone(CurrentZoneTier, config.ExitRules);
        }
    }

    public ZoneManager(WheelConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }
        this.config = config;
        current_zone = FirstZoneIndex;
    }

    public void Reset()
    {
        current_zone = FirstZoneIndex;
    }

    public void Advance()
    {
        current_zone = GameRules.NextZoneIndex(config, current_zone);
    }

    public WheelVisual GetZoneVisual(int zone_idx)
    {
        return GameRules.GetZoneVisual(config, zone_idx);
    }

    public RewardTier GetZoneTier(int zone_idx)
    {
        return GameRules.GetZoneTier(config, zone_idx);
    }
}
}
