using System;
using PrimeTween;
using UnityEngine;

namespace VertigoWheel
{
public class MetaProgressPanel : MonoBehaviour
{
    [SerializeField] private RunSession controller;
    [SerializeField] private UnityEngine.UI.ScrollRect scroll_rect;
    [SerializeField] private RectTransform rows_container;

    [SerializeField] private MetaProgressCardUI card_prefab;

    [SerializeField] private CanvasGroup panel_canvas_group;
    private MetaProgressModel model;
    private RuntimeCard[] runtime_cards;
    private bool panel_visible;
    private RunEventPass event_pass;

    void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
    }

    void Start()
    {
        model = controller.MetaProgress;
        BuildCards();
        SetPanelVisible(false);
    }

    void OnEnable()
    {
        event_pass.Subscribe<RunPendingClearedEvent>(HandleRunReset);
        event_pass.Subscribe<ExitFlowStateChangedEvent>(HandleExitStateChanged);
    }

    void OnDisable()
    {
        event_pass.ReleaseAll();

        if (runtime_cards != null)
        {
            for (int i = 0; i < runtime_cards.Length; i++)
            {
                TweenLifetime.StopIfAlive(runtime_cards[i].hide_tween);
                runtime_cards[i].pending_completion_kind = MetaCompletionKind.None;
                runtime_cards[i].on_progress_animation_complete = null;
                runtime_cards[i].on_card_resolved = null;
            }
        }
    }

    private void HandleExitStateChanged(ExitFlowStateChangedEvent evt)
    {
        scroll_rect.enabled = evt.current_state == ExitFlowState.None;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panel_visible != visible)
        {
            panel_visible = visible;
            panel_canvas_group.alpha = visible ? 1f : 0f;
            panel_canvas_group.blocksRaycasts = visible;
            panel_canvas_group.interactable = visible;
        }
    }
    private class RuntimeCard
    {
        internal MetaProgressPanel panel;
        internal int card_index;
        internal MetaProgressCardUI ui;
        internal bool is_active;
        internal MetaCompletionKind pending_completion_kind;
        internal Tween hide_tween;
        internal Action on_progress_animation_complete;
        internal Action<int, Vector3> on_card_resolved;

        internal void HandleProgressAnimationComplete()
        {
            panel.HandleProgressAnimationComplete(this);
        }

        internal void HandleCardResolved(Vector3 source_world)
        {
            Action<int, Vector3> handler = on_card_resolved;
            on_card_resolved = null;
            handler?.Invoke(card_index, source_world);
        }
    }

    internal void PrepareFlyTarget(int card_idx)
    {
        RuntimeCard card = runtime_cards[card_idx];
        SetPanelVisible(true);
        if (!card.is_active)
        {
            ActivateCard(card);
            ApplyCardImmediate(card);
        }
    }

    internal bool TryGetFlyTarget(int card_idx, out RectTransform target, out Sprite sprite)
    {
        RuntimeCard card = runtime_cards[card_idx];
        target = card.ui.PuzzleTarget;
        sprite = card.ui.PuzzleSprite;
        return target != null && sprite != null;
    }

    private void ActivateCard(RuntimeCard card)
    {
        if (!card.is_active)
        {
            card.is_active = true;
            card.ui.gameObject.SetActive(true);
            card.ui.PlayActivationFeedback();
        }
    }

    internal bool AddProgressFromFlyAtIndex(
        int card_idx,
        int delta,
        MetaCompletionKind completion_kind,
        Action on_animation_complete,
        Action<int, Vector3> on_card_resolved)
    {
        RuntimeCard card = runtime_cards[card_idx];
        MetaProgressModel.ProgressApplied applied = model.ApplyProgress(card_idx, delta);
        if (!applied.valid)
        {
            return false;
        }

        ActivateCard(card);
        bool completes_card = completion_kind != MetaCompletionKind.None;
        card.pending_completion_kind = completion_kind;
        card.on_progress_animation_complete = completes_card ? null : on_animation_complete;
        card.on_card_resolved = completes_card ? on_card_resolved : null;
        card.ui.AnimateProgressTo(
            applied.old_total,
            applied.new_total,
            applied.limit,
            card.HandleProgressAnimationComplete);
        card.ui.ShowGainFeedback();
        return true;
    }

    private void BuildCards()
    {
        int count = model.CardCount;
        runtime_cards = new RuntimeCard[count];

        for (int i = 0; i < count; i++)
        {
            EntryView entry = model.GetEntry(i);

            MetaProgressCardUI instance = Instantiate(card_prefab, rows_container);
            instance.gameObject.name = $"Card_{i}_{entry.point_reward.rewardId}";

            RuntimeCard card = new RuntimeCard
            {
                panel = this,
                card_index = i,
                ui = instance,
                is_active = false,
            };

            instance.ConfigureTiming(controller.MetaProgressConfig.cardTiming);
            instance.SetStaticData(entry);
            ApplyCardImmediate(card);

            runtime_cards[i] = card;
            DeactivateCard(card);
        }
    }

    private void ApplyCardImmediate(RuntimeCard card)
    {
        int i = card.card_index;
        int limit = model.GetLimit(i);
        int amount = model.GetCurrentTotal(i);
        card.ui.SetProgressImmediate(amount, limit);
    }

    private static void DeactivateCard(RuntimeCard card)
    {
        card.is_active = false;
        card.ui.gameObject.SetActive(false);
    }

    private void HandleRunReset(RunPendingClearedEvent _)
    {
        if (panel_visible)
        {
            ResetRuntimeCards();
            SetPanelVisible(false);
        }
    }

    private void ResetRuntimeCards()
    {
        if (runtime_cards != null)
        {
            for (int i = 0; i < runtime_cards.Length; i++)
            {
                RuntimeCard card = runtime_cards[i];
                TweenLifetime.StopIfAlive(card.hide_tween);
                card.pending_completion_kind = MetaCompletionKind.None;
                card.on_progress_animation_complete = null;
                card.on_card_resolved = null;
                card.ui.ResetFeedback();
                DeactivateCard(card);
            }
        }
    }

    private void HandleProgressAnimationComplete(RuntimeCard card)
    {
        MetaCompletionKind completion_kind = card.pending_completion_kind;
        Action animation_complete = card.on_progress_animation_complete;
        card.pending_completion_kind = MetaCompletionKind.None;
        card.on_progress_animation_complete = null;

        if (completion_kind != MetaCompletionKind.None)
        {
            HandleCardCompleted(card, completion_kind);
            return;
        }

        animation_complete?.Invoke();
    }

    private void HandleCardCompleted(RuntimeCard card, MetaCompletionKind completion_kind)
    {
        card.ui.ShowCompletion(completion_kind, model.GetLimit(card.card_index));
        TweenLifetime.StopIfAlive(card.hide_tween);
        card.hide_tween = Tween.Delay(card, card.ui.CompleteFeedbackDuration, OnCompletionHideComplete);
    }

    private static void OnCompletionHideComplete(RuntimeCard card)
    {
        RectTransform source = card.ui.ConvertedPuzzleTarget;
        Vector3 source_world = source != null ? source.position : card.ui.transform.position;
        DeactivateCard(card);
        card.HandleCardResolved(source_world);
    }
}
}
