using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VertigoWheel
{
public class RunExitController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RunSession controller;

    [Header("Buttons")]
    [SerializeField] private Button exit_button;
    [SerializeField] private CanvasGroup exit_canvas_group;
    [SerializeField] private Button fresh_start_confirm_button;
    [SerializeField] private Button fresh_start_cancel_button;
    [SerializeField] private Button collect_confirm_button;
    [SerializeField] private Button collect_cancel_button;
    [SerializeField] private Button give_up_button;
    [SerializeField] private Button revive_button;
    [SerializeField] private Button lose_rewards_button;
    [SerializeField] private Button go_back_button;
    [SerializeField] private TMP_Text revive_cost_value;

    [Header("HUD")]
    [SerializeField] private ZoneTrack zone_hud;
    [SerializeField] private CurrencyHUD currency_hud;

    [Header("Overlay")]
    [SerializeField] private GameObject overlay_container;
    [SerializeField] private GameObject death_root;
    [SerializeField] private GameObject death_confirm_root;
    [SerializeField] private GameObject fresh_start_root;
    [SerializeField] private GameObject collect_root;

    [Header("Death Panel")]
    [SerializeField] private RectTransform death_panel_root;
    [SerializeField] private CanvasGroup reward_panel_group;

    [Header("Overlay Layering")]
    [SerializeField] private Canvas reward_list_canvas;
    [SerializeField] private Canvas meta_progress_canvas;

    private const int HudBehindOverlayOrder = 12;
    private const int HudAboveOverlayOrder = 102;

    private enum ExitVisibility { Hidden, Disabled, Normal }

    private ExitFlowStateMachine state_machine = new ExitFlowStateMachine(ExitFlowState.None);
    private bool revive_in_flight;
    private Tween scale_tween;
    private RunEventPass event_pass;

    private ExitFlowState CurrentState
    {
        get
        {
            return state_machine.State;
        }
    }

    private struct ExitContext
    {
        internal ExitVisibility visibility;
        internal bool rewards_at_stake;
        internal bool reward_list_at_stake;
    }

    private void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
        InitializeOverlay();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BindSceneComponent(ref exit_button, "ui_panel_reward_list_exit_header");
        BindSceneComponent(ref fresh_start_confirm_button, "ui_button_exit_fresh_confirm");
        BindSceneComponent(ref fresh_start_cancel_button, "ui_button_exit_fresh_cancel");
        BindSceneComponent(ref collect_confirm_button, "ui_button_exit_collect_confirm");
        BindSceneComponent(ref collect_cancel_button, "ui_button_exit_collect_cancel");
        BindSceneComponent(ref give_up_button, "ui_button_giveup");
        BindSceneComponent(ref revive_button, "ui_button_revive");
        BindSceneComponent(ref lose_rewards_button, "ui_button_death_lose_rewards");
        BindSceneComponent(ref go_back_button, "ui_button_death_go_back");
        BindSceneComponent(ref reward_list_canvas, "ui_group_reward_list");
        BindSceneComponent(ref meta_progress_canvas, "ui_meta_progress_panel");

        if (exit_button != null)
        {
            CanvasGroup canvas_group = exit_button.GetComponent<CanvasGroup>();
            if (canvas_group != null)
            {
                exit_canvas_group = canvas_group;
            }
        }
    }

    private void BindSceneComponent<T>(ref T target, string object_name) where T : Component
    {
        T component = FindSceneComponent<T>(object_name);
        if (component != null)
        {
            target = component;
        }
    }

    private T FindSceneComponent<T>(string object_name) where T : Component
    {
        if (!gameObject.scene.IsValid()) return null;

        T[] components = FindObjectsOfType<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null) continue;
            if (component.gameObject.scene != gameObject.scene) continue;
            if (component.gameObject.name != object_name) continue;
            return component;
        }

        return null;
    }
#endif

    private void OnEnable()
    {
        event_pass.Subscribe<RunDeathHitEvent>(HandleDeathHit);
        event_pass.Subscribe<RunStateChangedEvent>(HandleRunStateChanged);
        SetButtonListeners(true);
        ApplyState(CurrentState, CurrentState);
        RefreshExitVisibility();
    }

    private void OnDisable()
    {
        event_pass.ReleaseAll();
        SetButtonListeners(false);
    }

    private void OnDestroy()
    {
        TweenLifetime.StopIfAlive(scale_tween);
    }

    private void SetButtonListeners(bool active)
    {
        SetButtonListener(exit_button, PressExit, active);
        SetButtonListener(fresh_start_confirm_button, CloseAndRestart, active);
        SetButtonListener(fresh_start_cancel_button, CloseExitOverlay, active);
        SetButtonListener(collect_confirm_button, CollectAndLeave, active);
        SetButtonListener(collect_cancel_button, CloseExitOverlay, active);
        SetButtonListener(give_up_button, OnGiveUpClicked, active);
        SetButtonListener(revive_button, OnReviveClicked, active);
        SetButtonListener(lose_rewards_button, OnLoseRewardsClicked, active);
        SetButtonListener(go_back_button, CancelGiveUp, active);
    }

    private static void SetButtonListener(Button button, UnityAction handler, bool active)
    {
        if (active) button.onClick.AddListener(handler);
        else button.onClick.RemoveListener(handler);
    }

    private void PressExit()
    {
        if (CurrentState == ExitFlowState.None)
        {
            ExitContext context = CaptureContext();
            if (context.visibility == ExitVisibility.Normal)
            {
                SetState(context.rewards_at_stake ? ExitFlowState.CollectConfirm : ExitFlowState.FreshStartConfirm);
            }
        }
    }

    private void CollectAndLeave()
    {
        SetState(ExitFlowState.None);
        controller.RequestLeave();
    }

    private void CloseExitOverlay()
    {
        SetState(ExitFlowState.None);
    }

    private void CancelGiveUp()
    {
        if (CurrentState == ExitFlowState.GiveUpConfirm) SetState(ExitFlowState.DeathSkull);
    }

    private bool TryReviveFromDeathPanel()
    {
        if (revive_in_flight) return false;
        revive_in_flight = true;
        if (!controller.TryRevive()) { revive_in_flight = false; return false; }
        SetState(ExitFlowState.None);
        return true;
    }

    private void HandleDeathHit(RunDeathHitEvent _)
    {
        revive_in_flight = false;
        SetState(ExitFlowState.DeathSkull);
        RefreshExitVisibility();
    }

    private void HandleRunStateChanged(RunStateChangedEvent _)
    {
        RefreshExitVisibility();
    }

    private void SetState(ExitFlowState next)
    {
        ExitFlowState previous = CurrentState;
        if (state_machine.TryChange(next))
        {
            ApplyState(previous, CurrentState);
            controller.NotifyExitFlowStateChanged(CurrentState);
        }
    }

    private void ApplyState(ExitFlowState previous, ExitFlowState next)
    {
        ExitContext context = CaptureContext();
        ApplyOverlayState(next);

        if (previous != ExitFlowState.DeathSkull && next == ExitFlowState.DeathSkull) ShowDeathAnimated(context);
        else if (previous == ExitFlowState.DeathSkull && next != ExitFlowState.DeathSkull) ResetDeathVisuals();
        if (previous != ExitFlowState.GiveUpConfirm && next == ExitFlowState.GiveUpConfirm) ResetDeathConfirmButtons();
    }

    private void ApplyOverlayState(ExitFlowState next)
    {
        bool overlay_open = next != ExitFlowState.None;

        if (overlay_open) SetActive(overlay_container, true);
        ShowRoots(next);
        ApplyHudLayering(next);
        SetHudInteractive(!overlay_open);
        if (!overlay_open) SetActive(overlay_container, false);
    }

    private void ShowRoots(ExitFlowState state)
    {
        bool death = IsDeathState(state);
        SetActive(death_root, death);
        SetActive(death_confirm_root, state == ExitFlowState.GiveUpConfirm);
        SetActive(fresh_start_root, state == ExitFlowState.FreshStartConfirm);
        SetActive(collect_root, state == ExitFlowState.CollectConfirm);
    }

    private void SetHudInteractive(bool active)
    {
        zone_hud.SetInteractive(active);
        currency_hud.SetInteractive(active);
    }

    private void ShowDeathAnimated(ExitContext context)
    {
        give_up_button.interactable = true;
        RefreshReviveVisuals();
        SetRewardPanelVisible(context.reward_list_at_stake);
        SetRewardPanelInteractive(false);
        TweenLifetime.StopIfAlive(scale_tween);
        death_panel_root.localScale = Vector3.zero;
        scale_tween = Tween.Scale(death_panel_root, Vector3.one, controller.ExitFlowTiming.death_panel_show_duration, Ease.OutBack);
    }

    private void ResetDeathVisuals()
    {
        TweenLifetime.StopIfAlive(scale_tween);
        death_panel_root.localScale = Vector3.zero;
        SetRewardPanelVisible(true);
        SetRewardPanelInteractive(true);
    }

    private void RefreshReviveVisuals()
    {
        bool can_revive = controller.CanAffordRevive();
        revive_button.interactable = can_revive;
        TextTransformer.SetNumber(revive_cost_value, controller.CurrentReviveCost);
    }

    private void OnGiveUpClicked()
    {
        if (CurrentState == ExitFlowState.DeathSkull)
        {
            give_up_button.interactable = false;
            ExitContext context = CaptureContext();
            if (context.rewards_at_stake) SetState(ExitFlowState.GiveUpConfirm);
            else CloseAndRestart();
        }
    }

    private void ResetDeathConfirmButtons()
    {
        lose_rewards_button.interactable = true;
        go_back_button.interactable = true;
    }

    private void OnLoseRewardsClicked()
    {
        lose_rewards_button.interactable = false;
        CloseAndRestart();
    }

    private void CloseAndRestart()
    {
        SetState(ExitFlowState.None);
        controller.Restart();
    }

    private void OnReviveClicked()
    {
        revive_button.interactable = false;
        if (!TryReviveFromDeathPanel()) RefreshReviveVisuals();
    }

    private void SetRewardPanelInteractive(bool interactive)
    {
        reward_panel_group.interactable = interactive;
        reward_panel_group.blocksRaycasts = interactive;
    }

    private void SetRewardPanelVisible(bool visible)
    {
        reward_panel_group.alpha = visible ? 1f : 0f;
        if (!visible)
        {
            SetRewardPanelInteractive(false);
        }
    }

    private void RefreshExitVisibility()
    {
        ExitVisibility visibility = CaptureContext().visibility;
        bool should_show = visibility != ExitVisibility.Hidden;
        bool normal = visibility == ExitVisibility.Normal;

        exit_button.gameObject.SetActive(should_show);
        exit_button.interactable = should_show && normal;
        exit_canvas_group.blocksRaycasts = should_show && normal;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go.activeSelf != active) go.SetActive(active);
    }

    private void ApplyHudLayering(ExitFlowState state)
    {
        SetCanvasLayer(reward_list_canvas, IsDeathState(state) ? HudAboveOverlayOrder : HudBehindOverlayOrder);
        SetCanvasLayer(meta_progress_canvas, HudBehindOverlayOrder);
    }

    private static void SetCanvasLayer(Canvas target, int sorting_order)
    {
        if (target != null)
        {
            target.overrideSorting = true;
            target.sortingOrder = sorting_order;
        }
    }

    private void InitializeOverlay()
    {
        SetActive(overlay_container, true);
        ShowRoots(ExitFlowState.None);
        SetActive(overlay_container, false);
    }

    private ExitContext CaptureContext()
    {
        bool can_exit = controller.CanLeave;
        bool rewards_at_stake = controller.HasPendingRewards;
        bool reward_list_at_stake = controller.HasPendingRewardList;

        return new ExitContext
        {
            visibility = controller.IsDeathFlowActive ? ExitVisibility.Hidden : can_exit ? ExitVisibility.Normal : ExitVisibility.Disabled,
            rewards_at_stake = rewards_at_stake,
            reward_list_at_stake = reward_list_at_stake
        };
    }

    private static bool IsDeathState(ExitFlowState state)
    {
        return state == ExitFlowState.DeathSkull || state == ExitFlowState.GiveUpConfirm;
    }
}

internal enum ExitFlowState { None, FreshStartConfirm, CollectConfirm, DeathSkull, GiveUpConfirm }

internal class ExitFlowStateMachine
{
    private ExitFlowState state;

    internal ExitFlowState State
    {
        get
        {
            return state;
        }
    }

    internal ExitFlowStateMachine(ExitFlowState initial_state)
    {
        state = initial_state;
    }

    internal bool TryChange(ExitFlowState next)
    {
        if (next == state || !CanTransition(state, next))
        {
            return false;
        }

        state = next;
        return true;
    }

    private static bool CanTransition(ExitFlowState from, ExitFlowState to)
    {
        if (to == ExitFlowState.None || to == ExitFlowState.DeathSkull)
        {
            return true;
        }

        switch (from)
        {
            case ExitFlowState.None:
                return to == ExitFlowState.FreshStartConfirm
                    || to == ExitFlowState.CollectConfirm;

            case ExitFlowState.DeathSkull:
                return to == ExitFlowState.GiveUpConfirm;

            case ExitFlowState.GiveUpConfirm:
                return to == ExitFlowState.DeathSkull;

            default:
                return false;
        }
    }
}
}
