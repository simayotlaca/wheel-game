using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VertigoWheel
{
public enum ExitKind { None, FreshStart, Collect }
public enum ExitVisibility { Hidden, Disabled, Normal }
public enum ExitFlowState { None, FreshStartConfirm, CollectConfirm, DeathSkull, GiveUpConfirm }
public enum ExitZoneType { Normal, Demoted, Death }

public class RunExitController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RunSession wheel;
    [SerializeField] private ConfigAnimation anim_config;
    [SerializeField] private RewardListUI reward_list;

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
    [SerializeField] private Canvas reward_list_canvas;
    [SerializeField] private Canvas zone_bar_canvas;
    [SerializeField] private Canvas currency_canvas;
    [SerializeField] private Canvas meta_progress_canvas;
    [SerializeField] private CanvasGroup meta_progress_group;

    [Header("Overlay")]
    [SerializeField] private GameObject overlay_container;
    [SerializeField] private GameObject death_root;
    [SerializeField] private GameObject death_confirm_root;
    [SerializeField] private GameObject fresh_start_root;
    [SerializeField] private GameObject collect_root;

    [Header("Death Panel")]
    [SerializeField] private RectTransform death_panel_root;
    [SerializeField] private CanvasGroup reward_panel_group;
    [SerializeField] private CanvasGroup revive_button_group;

    private ExitFlowState state = ExitFlowState.None;
    private bool revive_in_flight;
    private Tween scale_tween;
    private Tween exit_alpha_tween;
    private bool has_exit_visibility;
    private ExitVisibility last_exit_visibility;

    private const int hud_promoted = 12;
    private const int reward_list_below_overlay = 5;
    private const int overlay_promoted = 101;

    public ExitFlowState State => state;
    public event Action<ExitFlowState> OnStateChanged;

    private bool HasRewardListAtStake => wheel != null && wheel.Inventory.HasPending;
    private bool HasMetaProgressAtStake => wheel != null && wheel.HasUnbankedMetaProgress;
    private bool HasAnythingAtStake => HasRewardListAtStake || HasMetaProgressAtStake;

    private void Awake() => InitializeOverlay();

    private void OnEnable()
    {
        if (wheel != null)
        {
            wheel.OnDeathHit += HandleDeathHit;
            wheel.OnStateChanged += HandleRunStateChanged;
            wheel.OnBusyChanged += HandleRunAvailabilityChanged;
            wheel.OnZoneChanged += HandleZoneChanged;
        }
        BindButtons(true);
        ApplyState(state, state);
        RefreshExitVisibility(true);
    }

    private void OnDisable()
    {
        if (wheel != null)
        {
            wheel.OnDeathHit -= HandleDeathHit;
            wheel.OnStateChanged -= HandleRunStateChanged;
            wheel.OnBusyChanged -= HandleRunAvailabilityChanged;
            wheel.OnZoneChanged -= HandleZoneChanged;
        }
        BindButtons(false);
        TweenLifetime.StopIfAlive(exit_alpha_tween);
    }

    private void OnDestroy()
    {
        TweenLifetime.StopIfAlive(scale_tween);
        TweenLifetime.StopIfAlive(exit_alpha_tween);
    }

    private void BindButtons(bool add)
    {
        Bind(exit_button, PressExit, add);
        Bind(fresh_start_confirm_button, ConfirmFreshStart, add);
        Bind(fresh_start_cancel_button, CancelExit, add);
        Bind(collect_confirm_button, ConfirmCollect, add);
        Bind(collect_cancel_button, CancelExit, add);
        Bind(give_up_button, OnGiveUpClicked, add);
        Bind(revive_button, OnReviveClicked, add);
        Bind(lose_rewards_button, OnLoseRewardsClicked, add);
        Bind(go_back_button, CancelGiveUp, add);
    }

    private static void Bind(Button b, UnityAction a, bool add)
    {
        if (b == null) return;
        if (add) b.onClick.AddListener(a);
        else b.onClick.RemoveListener(a);
    }

    public void PressExit()
    {
        if (state != ExitFlowState.None) return;
        ExitKind kind = GameRules.ClassifyExit(wheel);
        if (kind == ExitKind.FreshStart) SetState(ExitFlowState.FreshStartConfirm);
        else if (kind == ExitKind.Collect) SetState(ExitFlowState.CollectConfirm);
    }

    public void ConfirmFreshStart() => CloseAndRestart();

    public void ConfirmCollect()
    {
        SetState(ExitFlowState.None);
        if (wheel.RequestLeave()) reward_list.HideAll();
    }

    public void CancelExit() => SetState(ExitFlowState.None);

    public void ConfirmLoseRewards() => CloseAndRestart();

    public void CancelGiveUp()
    {
        if (state == ExitFlowState.GiveUpConfirm) SetState(ExitFlowState.DeathSkull);
    }

    public bool PressRevive()
    {
        if (revive_in_flight) return false;
        revive_in_flight = true;
        if (!wheel.TryRevive()) { revive_in_flight = false; return false; }
        SetState(ExitFlowState.None);
        return true;
    }

    private void HandleDeathHit()
    {
        revive_in_flight = false;
        SetState(ExitFlowState.DeathSkull);
        RefreshExitVisibility(true);
    }

    private void HandleRunStateChanged(RunState _) => RefreshExitVisibility();
    private void HandleRunAvailabilityChanged() => RefreshExitVisibility();
    private void HandleZoneChanged(int _) => RefreshExitVisibility();

    private void SetState(ExitFlowState next)
    {
        if (state == next) return;
        ExitFlowState previous = state;
        state = next;
        ApplyState(previous, state);
        OnStateChanged?.Invoke(state);
    }

    private void ApplyState(ExitFlowState previous, ExitFlowState next)
    {
        ApplyOverlayState(next);

        if (previous != ExitFlowState.DeathSkull && next == ExitFlowState.DeathSkull) ShowDeathAnimated();
        else if (previous == ExitFlowState.DeathSkull && next != ExitFlowState.DeathSkull) ResetDeathVisuals();
        if (previous != ExitFlowState.GiveUpConfirm && next == ExitFlowState.GiveUpConfirm) ResetDeathConfirmButtons();
    }

    private void ApplyOverlayState(ExitFlowState next)
    {
        bool fresh = next == ExitFlowState.FreshStartConfirm;
        bool collect = next == ExitFlowState.CollectConfirm;
        bool death = next == ExitFlowState.DeathSkull || next == ExitFlowState.GiveUpConfirm;
        bool overlay_open = next != ExitFlowState.None;

        if (overlay_open) SetActive(overlay_container, true);
        ShowRoots(death, next == ExitFlowState.GiveUpConfirm, fresh, collect);
        ApplyHudMode(death ? ExitZoneType.Death : overlay_open ? ExitZoneType.Demoted : ExitZoneType.Normal);
        SetHudInteractive(!overlay_open);
        if (!overlay_open) SetActive(overlay_container, false);
    }

    private void ShowRoots(bool death, bool death_confirm, bool fresh, bool collect)
    {
        SetActive(death_root, death);
        SetActive(death_confirm_root, death_confirm);
        SetActive(fresh_start_root, fresh);
        SetActive(collect_root, collect);
    }

    private void SetHudInteractive(bool active)
    {
        if (zone_hud != null) zone_hud.SetInteractive(active);
        if (currency_hud != null) currency_hud.SetInteractive(active);
    }

    private void ShowDeathAnimated()
    {
        if (give_up_button != null) give_up_button.interactable = true;
        if (revive_button != null) revive_button.interactable = true;
        RefreshReviveVisuals();
        SetRewardPanelInteractive(!HasAnythingAtStake);
        TweenLifetime.StopIfAlive(scale_tween);
        if (death_panel_root != null)
        {
            death_panel_root.localScale = Vector3.zero;
            scale_tween = Tween.Scale(death_panel_root, Vector3.one, anim_config.deathPanelShowDuration, Ease.OutBack);
        }
    }

    private void ResetDeathVisuals()
    {
        TweenLifetime.StopIfAlive(scale_tween);
        if (death_panel_root != null) death_panel_root.localScale = Vector3.zero;
        SetRewardPanelInteractive(true);
    }

    private void RefreshReviveVisuals()
    {
        if (revive_button != null) revive_button.interactable = wheel.CanAffordRevive();
        if (revive_button_group != null) revive_button_group.alpha = wheel.CanAffordRevive() ? 1f : anim_config.reviveDisabledAlpha;
        if (revive_cost_value != null) revive_cost_value.SetText("{0}", wheel.CurrentReviveCost);
    }

    private void OnGiveUpClicked()
    {
        if (state != ExitFlowState.DeathSkull) return;
        if (give_up_button != null) give_up_button.interactable = false;
        if (HasAnythingAtStake) SetState(ExitFlowState.GiveUpConfirm);
        else ConfirmLoseRewards();
    }

    private void ResetDeathConfirmButtons()
    {
        if (lose_rewards_button != null) lose_rewards_button.interactable = true;
        if (go_back_button != null) go_back_button.interactable = true;
    }

    private void OnLoseRewardsClicked()
    {
        if (lose_rewards_button != null) lose_rewards_button.interactable = false;
        ConfirmLoseRewards();
    }

    private void CloseAndRestart()
    {
        SetState(ExitFlowState.None);
        wheel.Restart();
    }

    private void OnReviveClicked()
    {
        if (revive_button != null) revive_button.interactable = false;
        if (!PressRevive()) RefreshReviveVisuals();
    }

    private void SetRewardPanelInteractive(bool interactive)
    {
        if (reward_panel_group == null) return;
        reward_panel_group.interactable = interactive;
        reward_panel_group.blocksRaycasts = interactive;
    }

    private void RefreshExitVisibility(bool force = false)
    {
        if (exit_button == null || exit_canvas_group == null) return;
        ExitVisibility visibility = GameRules.ResolveExitVisibility(wheel);
        if (!force && has_exit_visibility && visibility == last_exit_visibility) return;
        has_exit_visibility = true;
        last_exit_visibility = visibility;
        bool should_show = visibility != ExitVisibility.Hidden;
        exit_button.gameObject.SetActive(should_show);
        if (!should_show) { TweenLifetime.StopIfAlive(exit_alpha_tween); return; }
        bool normal = visibility == ExitVisibility.Normal;
        float target_alpha = normal ? 1f : anim_config.exitDisabledAlpha;
        exit_button.interactable = normal;
        exit_canvas_group.blocksRaycasts = normal;
        TweenLifetime.StopIfAlive(exit_alpha_tween);
        if (force || anim_config.exitDisabledFadeDuration <= 0f || Mathf.Approximately(exit_canvas_group.alpha, target_alpha))
        {
            exit_canvas_group.alpha = target_alpha;
            return;
        }
        exit_alpha_tween = Tween.Alpha(exit_canvas_group, target_alpha, anim_config.exitDisabledFadeDuration, Ease.OutCubic);
    }

    private void ApplyHudMode(ExitZoneType mode)
    {
        ApplyExitFlowLayering(mode);

        if (reward_panel_group != null)
            reward_panel_group.alpha = (mode == ExitZoneType.Death) ? (HasRewardListAtStake ? 1f : 0f) : 1f;
        if (meta_progress_group != null)
            meta_progress_group.alpha = (mode == ExitZoneType.Death) ? (HasMetaProgressAtStake ? 1f : 0f) : 1f;
    }

    private void ApplyExitFlowLayering(ExitZoneType mode)
    {
        int hud_order = reward_list_below_overlay;
        int reward_order = hud_promoted;
        int meta_order = hud_promoted;

        if (mode == ExitZoneType.Normal)
        {
            hud_order = hud_promoted;
        }
        else if (mode == ExitZoneType.Demoted)
        {
            reward_order = reward_list_below_overlay;
            meta_order = reward_list_below_overlay;
        }
        else if (mode == ExitZoneType.Death)
        {
            reward_order = overlay_promoted;
            meta_order = hud_order;
        }

        ApplyCanvasOrder(reward_list_canvas, reward_order, true);
        ApplyCanvasOrder(zone_bar_canvas, hud_order, false);
        ApplyCanvasOrder(currency_canvas, overlay_promoted, false);
        ApplyCanvasOrder(meta_progress_canvas, meta_order, true);
    }

    private static void ApplyCanvasOrder(Canvas canvas, int order, bool force_override)
    {
        if (canvas == null)
        {
            return;
        }
        if (force_override)
        {
            canvas.overrideSorting = true;
        }
        canvas.sortingOrder = order;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    private void InitializeOverlay()
    {
        SetActive(overlay_container, true);
        ShowRoots(false, false, false, false);
        SetActive(overlay_container, false);
    }
}
}
