using UnityEngine;

namespace VertigoWheel
{
public class RunSession : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WheelConfig config;

    [Header("Scene References")]
    [SerializeField] private WheelController wheel_controller;
    [SerializeField] private RewardFeedbackController reward_feedback;
    private WheelResultPicker result_picker;
    private ZoneManager zone_manager;
    private CurrencyWallet inventory;
    private ReviveSystem revive_system;
    private MetaProgressModel meta_progress_model;

    private RunEventBus events = new RunEventBus();
    private GameRules.StateMachine state_machine = new GameRules.StateMachine(RunState.Ready);
    private SpinResult last_result;
    private RewardSettlement last_settlement;

    internal IRunEventReader Events
    {
        get
        {
            return events;
        }
    }

    internal CurrencyConfig CurrencyConfig
    {
        get
        {
            return config.currency_config;
        }
    }

    internal MetaProgressModel MetaProgress
    {
        get
        {
            return meta_progress_model;
        }
    }

    internal MetaProgressConfig MetaProgressConfig
    {
        get
        {
            return config.metaProgressConfig;
        }
    }

    internal CurrencyHudTiming CurrencyHudTiming
    {
        get
        {
            return config.feedbackConfig.currencyHudTiming;
        }
    }

    internal RewardListItemTiming RewardListItemTiming
    {
        get
        {
            return config.feedbackConfig.rewardListItemTiming;
        }
    }

    internal ExitFlowTiming ExitFlowTiming
    {
        get
        {
            return config.feedbackConfig.exitFlowTiming;
        }
    }

    internal bool CanSpin
    {
        get
        {
            return GameRules.CanTransition(state_machine.State, RunState.Spinning);
        }
    }

    internal bool CanLeave
    {
        get
        {
            return GameRules.CanLeave(state_machine.State);
        }
    }

    internal bool HasPendingRewards
    {
        get
        {
            return inventory.HasPending || meta_progress_model.HasUnbankedProgress;
        }
    }

    internal bool HasPendingRewardList
    {
        get
        {
            return inventory.HasPending;
        }
    }

    internal int CurrentZone
    {
        get
        {
            return zone_manager.CurrentZone;
        }
    }

    internal int FirstZoneIndex
    {
        get
        {
            return zone_manager.FirstZoneIndex;
        }
    }

    internal int MaxZoneIndex
    {
        get
        {
            return zone_manager.MaxZoneIndex;
        }
    }

    internal int CurrentReviveCost
    {
        get
        {
            return revive_system.CurrentCost;
        }
    }

    internal bool CanAffordRevive()
    {
        return revive_system.CanAfford(inventory);
    }

    internal RewardTier GetZoneTier(int zone_idx)
    {
        return zone_manager.GetZoneTier(zone_idx);
    }

    void Awake()
    {
        InitializeRuntime();
    }

    void Start()
    {
        BeginRun();
    }

    private void InitializeRuntime()
    {
        CurrencyRules currency_rules = new CurrencyRules(config.currency_config);
        zone_manager = new ZoneManager(config);
        inventory = new CurrencyWallet(config.currency_config);
        result_picker = new WheelResultPicker(currency_rules, config.rewardTable);
        revive_system = new ReviveSystem(currency_rules);
        CurrencyConfig currency_cfg = config.currency_config;
        inventory.ResetTo(currency_cfg.initialCash, currency_cfg.initialGold);
        meta_progress_model = new MetaProgressModel(config.metaProgressConfig.Entries,
            config.metaProgressConfig.overflowReward);
    }

    private void BeginRun()
    {
        LoadCurrentZone();
        PublishCurrencyChanged();
    }

    private bool ChangeState(RunState next)
    {
        if (state_machine.TryChange(next))
        {
            events.Publish(new RunStateChangedEvent(state_machine.State));
            return true;
        }
        return false;
    }

    internal void RequestSpin()
    {
        if (ChangeState(RunState.Spinning))
        {
            last_result = result_picker.Spin();
            last_settlement = last_result.IsDeath
                ? default(RewardSettlement)
                : GameRules.ResolveRewardSettlement(last_result, meta_progress_model);

            wheel_controller.SpinToSlice(last_result.slice_idx, config.spinTiming);
        }
    }

    internal void NotifyWheelSpinCompleted()
    {
        HandleWheelSpinCompleted();
    }

    internal void NotifyRewardFeedbackCompleted()
    {
        HandleRewardFeedbackCompleted();
    }

    internal void NotifyExitFlowStateChanged(ExitFlowState current_state)
    {
        events.Publish(new ExitFlowStateChangedEvent(current_state));
    }

    internal void RequestLeave()
    {
        if (!CanLeave)
        {
            return;
        }

        inventory.BankPending();
        meta_progress_model.Commit();
        PublishCurrencyChanged();
        events.Publish(new RunPendingClearedEvent());
        zone_manager.Reset();
        LoadCurrentZone();
    }

    internal bool IsDeathFlowActive
    {
        get
        {
            return GameRules.IsDeathFlowActive(state_machine.State, last_result);
        }
    }

    internal bool TryRevive()
    {
        if (GameRules.CanRevive(state_machine.State, last_result))
        {
            if (!revive_system.TryRevive(inventory))
            {
                return false;
            }

            if (!ChangeState(RunState.PostReviveLocked))
            {
                return false;
            }

            LoadZone(zone_manager.GetZoneVisual(zone_manager.CurrentZone), zone_manager.CurrentZone, true);
            PublishCurrencyChanged();
            return true;
        }
        return false;
    }

    internal void Restart()
    {
        revive_system.Reset();
        inventory.ClearPending();
        zone_manager.Reset();
        meta_progress_model.Revert();
        events.Publish(new RunPendingClearedEvent());

        LoadCurrentZone();
        if (state_machine.Reset(RunState.Ready))
        {
            events.Publish(new RunStateChangedEvent(state_machine.State));
        }
    }

    private void HandleRewardFeedbackCompleted()
    {
        if (ChangeState(RunState.Ready))
        {
            AdvanceZone();
        }
    }

    private void HandleWheelSpinCompleted()
    {
        if (ChangeState(RunState.Reward))
        {
            wheel_controller.RevealSlice(last_result.slice_idx);
            ApplySpinResult();
        }
    }

    private void ApplySpinResult()
    {
        if (last_result.IsDeath)
        {
            if (ChangeState(RunState.DeathGameOver))
            {
                events.Publish(new RunDeathHitEvent());
            }
        }
        else
        {
            reward_feedback.PlayReward(last_result, last_settlement);
        }
    }

    internal int ApplyRewardListArrival(RewardDefinition reward, int amount)
    {
        return inventory.AddPendingAndGetTotal(reward, amount);
    }

    private void AdvanceZone()
    {
        zone_manager.Advance();
        LoadCurrentZone();
    }

    private void LoadCurrentZone()
    {
        LoadZone(zone_manager.GetZoneVisual(zone_manager.CurrentZone), zone_manager.CurrentZone);
        events.Publish(new RunZoneChangedEvent(zone_manager.CurrentZone));
    }

    private void PublishCurrencyChanged()
    {
        events.Publish(new RunCurrencyChangedEvent(inventory.Cash, inventory.Gold));
    }

    private void LoadZone(WheelVisual zone, int zone_idx, bool revive = false)
    {
        result_picker.LoadZone(zone, zone_idx, revive);
        wheel_controller.BuildForZone(zone, result_picker.WheelSlots);
    }
}
}
