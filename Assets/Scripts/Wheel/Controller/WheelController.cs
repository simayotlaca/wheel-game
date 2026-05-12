using System;
using UnityEngine;
using UnityEngine.Profiling;

public enum WheelState
{
    Idle,
    Spinning,
    Landing,
    Reward,
    DeathGameOver
}

public class WheelController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WheelConfig config;

    [Header("View")]
    [SerializeField] private WheelView wheelView;

    [Header("RNG (0 = time-based)")]
    [SerializeField] private int seed = 0;

    private WheelLogic logic;
    private ZoneManager zone_manager;
    private RewardInventory inventory;

    private WheelStateBase ready_state;
    private WheelStateBase revive_lock_state;
    private WheelStateBase turning_state;
    private WheelStateBase landing_state;
    private WheelStateBase reward_state;
    private WheelStateBase death_state;

    private WheelStateBase current_state;

    private SpinResult last_result;

    private int revive_count;

    private bool is_collecting;

    private bool meta_busy;

    public event Action<int, ZoneType> OnZoneChanged;
    public event Action<SpinResult, SliceDefinition> OnRewardEarned;
    public event Action OnDeathHit;
    public event Action OnRewardsBanked;

    public event Action OnRunEnded;
    public event Action OnRevived;

    public WheelConfig Config => config;
    public WheelState State => current_state != null ? current_state.GetStateEnum() : WheelState.Idle;
    public ZoneManager Zones => zone_manager;
    public RewardInventory Inventory => inventory;

    public bool CanSpin  => current_state != null && current_state.CheckSpin();
    public bool CanLeave => current_state != null && current_state.CheckLeave();

    public bool MetaBusy => meta_busy;
    public void SetMetaBusy(bool value) { meta_busy = value; }

    public bool IsCollecting      => is_collecting;
    public bool check_revive_lock  => current_state == revive_lock_state;
    public bool LastResultIsDeath => last_result.isDeath;

    public float PopupShowDuration  => config.rewardPopupShowDuration;
    public float RewardHoldDuration => config.rewardPopupHoldDuration;

    private const int first_multiple_value = 1;

    public int CurrentReviveCost
    {
        get
        {
            return config.reviveCurrencyCost * (first_multiple_value + revive_count);
        }
    }

    public bool CanAffordRevive()
    {
        if (inventory == null) return false;
        return inventory.Gold >= CurrentReviveCost;
    }

    void Start()
    {
        BuildStates();
        BuildModels();
        LoadOrSeedInventory();
        LoadCurrentZoneIntoView();
    }

    private void BuildStates()
    {
        if (config == null)
            throw new System.InvalidOperationException("WheelController: config not assigned — wire WheelConfig in inspector.");
        config.ValidateRequiredReferences();
        if (wheelView == null)
            throw new System.InvalidOperationException("WheelController: wheelView not assigned — wire WheelView in inspector.");

        ready_state           = new ReadyState(this);
        revive_lock_state = new PostReviveReadyState(this);
        turning_state         = new TurningState(this);
        landing_state         = new LandingState(this);
        reward_state          = new RewardState(this);
        death_state           = new DeathState(this);

        current_state = ready_state;
    }

    private void BuildModels()
    {
        logic = new WheelLogic(seed);
        inventory = new RewardInventory(config.currencyConfig);
        zone_manager = new ZoneManager(config);
    }

    private void LoadOrSeedInventory()
    {

        CurrencyConfig currencyCfg = config.currencyConfig;
        if (currencyCfg.resetSaveOnLaunch)
            PlayerProgress.Clear();

        if (PlayerProgress.Load(out int savedCash, out int savedCoins, out var savedBanked))
        {
            inventory.RestoreFrom(savedCash, savedCoins, savedBanked);
        }
        else
        {

            inventory.RestoreFrom(currencyCfg.initialCash, currencyCfg.initialGold, null);
        }
    }

    void Update()
    {
        Profiler.BeginSample("WheelController.Tick");
        if (current_state != null) current_state.Tick();
        Profiler.EndSample();
    }

    private void ChangeState(WheelStateBase next)
    {
        if (next == null || next == current_state) return;
        if (current_state != null) current_state.Exit();
        current_state = next;
        current_state.Enter();
    }

    public void FinishLanding()
    {
        ChangeState(reward_state);
        ApplySpinResult();
    }

    public void FinishReward()
    {
        if (last_result.isDeath)
        {
            ChangeState(death_state);
            return;
        }

        ChangeState(ready_state);
        AdvanceZone();
    }

    public void GoIdle()
    {
        ChangeState(ready_state);
    }

    public void RequestSpin()
    {
        if (current_state == null || !current_state.CheckSpin())
        {
            if (meta_busy) DebugLogger.Log("[WheelController] Spin blocked: meta progress animation busy");
            return;
        }
        if (zone_manager == null)
            throw new System.InvalidOperationException("WheelController: zone_manager not initialized.");

        ZoneConfig zone = zone_manager.CurrentZoneConfig;
        if (zone == null)
            throw new System.InvalidOperationException($"WheelController: no ZoneConfig for zone {zone_manager.CurrentZone}.");

        logic.LoadZone(zone, zone_manager.CurrentZone);
        SpinResult result = logic.Spin(zone_manager.CurrentZone);
        if (!result.isValid) return;

        last_result = result;

        ChangeState(turning_state);

        wheelView.SpinTo(
            result.sliceIndex,
            config.spinDuration,
            config.minFullRotations,
            config.maxFullRotations,
            OnSpinAnimationComplete);
    }

    public bool TrySkipSpin()
    {
        if (current_state == null || !current_state.CheckSkipSpin()) return false;
        wheelView.TrySkipToEnd();
        return true;
    }

    public void RequestLeave()
    {
        if (current_state == null || !current_state.CheckLeave()) return;

        is_collecting = true;
        inventory.BankPending();
        OnRewardsBanked?.Invoke();
        zone_manager.Reset();
        LoadCurrentZoneIntoView();
        GoIdle();
        PersistProgress();
    }

    public void FinishCollecting()
    {
        is_collecting = false;
    }

    public bool IsInDeathFlow =>
        State == WheelState.DeathGameOver
        || (State == WheelState.Reward && last_result.isDeath);

    public bool TryRevive()
    {
        if (current_state == null || !current_state.CheckRevive()) return false;
        if (!inventory.TrySpendGold(CurrentReviveCost)) return false;

        revive_count++;
        PersistProgress();
        wheelView.UndimAll();
        AdvanceZone();
        logic.forceNoBombNextSpin = true;
        ChangeState(revive_lock_state);
        OnRevived?.Invoke();
        return true;
    }

    public void Restart()
    {
        is_collecting = false;
        revive_count = 0;
        inventory.ClearPending();
        if (zone_manager != null) zone_manager.Reset();
        OnRunEnded?.Invoke();
        LoadCurrentZoneIntoView();
        GoIdle();
        PersistProgress();
    }

    private void PersistProgress()
    {
        if (inventory == null) return;
        PlayerProgress.Save(inventory.Cash, inventory.Gold, inventory.Banked);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) PersistProgress();
    }

    void OnApplicationQuit()
    {
        PersistProgress();
    }

    private void OnSpinAnimationComplete()
    {

        wheelView.HighlightSlice(last_result.sliceIndex);
        wheelView.DimNonWinners(last_result.sliceIndex);
        ChangeState(landing_state);
    }

    private void ApplySpinResult()
    {
        SliceDefinition slice = logic.GetSlice(last_result.sliceIndex);

        if (last_result.isDeath)
        {

            OnDeathHit?.Invoke();
        }
        else
        {
            if (slice != null && slice.reward != null)
            {
                last_result.amount = slice.reward.ComputeAmount(zone_manager.CurrentZone, last_result.amount);
                inventory.AddPending(slice.reward, last_result.amount);
            }
            OnRewardEarned?.Invoke(last_result, slice);
        }
    }

    private void AdvanceZone()
    {
        zone_manager.Advance();
        LoadCurrentZoneIntoView();
    }

    private void ResetWheelVisualState()
    {
        wheelView.UndimAll();
    }

    private void LoadCurrentZoneIntoView()
    {
        ResetWheelVisualState();
        if (zone_manager == null)
            throw new System.InvalidOperationException("WheelController: zone_manager not initialized.");
        ZoneConfig zone = zone_manager.CurrentZoneConfig;
        if (zone == null)
            throw new System.InvalidOperationException($"WheelController: no ZoneConfig for zone {zone_manager.CurrentZone}.");

        logic.LoadZone(zone, zone_manager.CurrentZone);
        wheelView.BuildForZone(zone, zone_manager.CurrentZone, logic.WheelSlots);
        OnZoneChanged?.Invoke(zone_manager.CurrentZone, zone_manager.CurrentZoneType);
    }
}
