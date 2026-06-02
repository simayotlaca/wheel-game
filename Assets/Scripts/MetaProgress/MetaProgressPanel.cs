using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace VertigoWheel
{
public class MetaProgressPanel : MonoBehaviour
{
    #region serialized fields
    [SerializeField] private RunSession controller;
    [SerializeField] private RunExitController run_exit_controller;
    [SerializeField] private UnityEngine.UI.ScrollRect scroll_rect;
    [SerializeField] private RectTransform rows_container;

    [Header("Card Prefab")]
    [SerializeField] private MetaProgressCardUI card_prefab;

    [Header("Panel Visibility")]
    [SerializeField] private CanvasGroup panel_canvas_group;
    #endregion

    private MetaProgressModel model;
    private RuntimeCard[] runtime_cards;
    private bool panel_visible;

    public event System.Action<int> OnCardResolved;
    public event System.Action<RewardDefinition, int, Vector3> OnOverflowReady;
    public event System.Action OnAnimationComplete;

    #region properties
    public bool IsAnimating
    {
        get
        {
            if (runtime_cards == null)
            {
                return false;
            }
            for (int i = 0; i < runtime_cards.Length; i++)
            {
                if (runtime_cards[i].HasActiveTweens)
                {
                    return true;
                }
            }
            return false;
        }
    }
    #endregion

    #region lifecycle
    void Start()
    {
        InitializePanel();
    }

    private void InitializePanel()
    {
        model = controller.MetaProgress;
        BuildCards();
        SetPanelVisible(false);
    }

    void OnEnable()
    {
        controller.OnRunEnded += HandleRunReset;
        controller.OnRewardsBanked += HandleRunReset;
        run_exit_controller.OnStateChanged += HandleExitStateChanged;
    }

    void OnDisable()
    {
        controller.OnRunEnded -= HandleRunReset;
        controller.OnRewardsBanked -= HandleRunReset;
        run_exit_controller.OnStateChanged -= HandleExitStateChanged;

        if (runtime_cards != null)
        {
            for (int i = 0; i < runtime_cards.Length; i++)
            {
                TweenLifetime.StopIfAlive(runtime_cards[i].hide_tween);
            }
        }
    }

    private void HandleExitStateChanged(ExitFlowState state)
    {
        scroll_rect.enabled = state == ExitFlowState.None;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panel_visible == visible) return;
        panel_visible = visible;
        if (panel_canvas_group != null)
        {
            panel_canvas_group.alpha = visible ? 1f : 0f;
            panel_canvas_group.blocksRaycasts = visible;
            panel_canvas_group.interactable = visible;
        }
    }
    #endregion

    #region runtime card
    private class RuntimeCard
    {
        public MetaProgressPanel panel;
        public int card_index;
        public MetaProgressCardUI ui;
        public bool is_active;
        public int pending_overflow;
        public Tween hide_tween;

        public void FireFillCompleted()
        {
            panel.HandleCardCompleted(this);
        }

        public void FireProgressAnimationCompleted()
        {
            panel.NotifyAnimationCompleteIfIdle();
        }

        public bool HasActiveTweens
        {
            get
            {
                if (hide_tween.isAlive)
                {
                    return true;
                }
                return ui.IsProgressAnimating;
            }
        }
    }
    #endregion

    #region public api
    public bool PrepareFlyTarget(int card_idx)
    {
        if (TryGetCard(card_idx, out RuntimeCard card))
        {
            SetPanelVisible(true);
            if (!card.is_active)
            {
                ActivateCard(card);
                ApplyCardImmediate(card);
            }
            return true;
        }
        return false;
    }

    public bool TryGetFlyTarget(int card_idx, out RectTransform target, out Sprite sprite)
    {
        target = null;
        sprite = null;
        if (TryGetCard(card_idx, out RuntimeCard card) && card.is_active)
        {
            target = card.ui.PuzzleTarget;
            sprite = card.ui.PuzzleSprite;
            return target != null && sprite != null && target.position != Vector3.zero;
        }
        return false;
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

    public Sprite GetPuzzleSpriteForReward(RewardDefinition reward)
    {
        if (TryGetCard(model.FindCardIndex(reward), out RuntimeCard card))
        {
            return card.ui.PuzzleSprite;
        }
        return null;
    }

    public void NotifyGainArrivedAtIndex(int card_idx, int amount)
    {
        if (amount > 0 && TryGetCard(card_idx, out RuntimeCard card))
        {
            if (card.is_active)
            {
                card.ui.ShowGainFeedback();
            }
        }
    }

    public void AddProgressFromFlyAtIndex(int card_idx, int delta)
    {
        MetaProgressModel.ProgressApplied applied = model.ApplyProgress(card_idx, delta);
        if (!applied.valid)
        {
            return;
        }
        if (TryGetCard(card_idx, out RuntimeCard card))
        {
            ActivateCard(card);
            card.ui.AnimateProgressTo(
                applied.old_total,
                applied.new_total,
                applied.limit,
                applied.just_completed,
                card.FireFillCompleted,
                card.FireProgressAnimationCompleted);
        }
    }
    #endregion

    #region build and bind
    private void BuildCards()
    {
        if (runtime_cards == null)
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

                BindCardStaticData(card);
                ApplyCardImmediate(card);

                runtime_cards[i] = card;
                DeactivateCard(card);
            }
        }
    }

    private void BindCardStaticData(RuntimeCard card)
    {
        card.ui.SetStaticData(model.GetEntry(card.card_index));
    }

    private void ApplyCardImmediate(RuntimeCard card)
    {
        int i = card.card_index;
        int limit = model.GetLimit(i);
        int amount = Mathf.Min(model.GetCurrentTotal(i), limit);
        card.ui.SetProgressImmediate(amount, limit);
    }

    private static void DeactivateCard(RuntimeCard card)
    {
        card.is_active = false;
        card.ui.gameObject.SetActive(false);
    }
    #endregion

    #region card lookup
    private bool TryGetCard(int card_idx, out RuntimeCard card)
    {
        if (runtime_cards != null && card_idx >= 0 && card_idx < runtime_cards.Length)
        {
            card = runtime_cards[card_idx];
            return true;
        }
        card = null;
        return false;
    }
    #endregion

    #region run reset
    private void HandleRunReset()
    {
        if (!panel_visible)
        {
            return;
        }

        ResetRuntimeCards();
        SetPanelVisible(false);
    }

    private void ResetRuntimeCards()
    {
        if (runtime_cards == null)
        {
            return;
        }

        for (int i = 0; i < runtime_cards.Length; i++)
        {
            RuntimeCard card = runtime_cards[i];
            TweenLifetime.StopIfAlive(card.hide_tween);
            card.ui.ResetFeedback();
            card.pending_overflow = 0;
            DeactivateCard(card);
        }
    }
    #endregion

    #region completion flow
    private void HandleCardCompleted(RuntimeCard card)
    {
        if (card.pending_overflow > 0)
        {
            card.ui.ShowConvertedState();
        }
        else
        {
            card.ui.ShowSkinReady(model.GetLimit(card.card_index));
        }

        TweenLifetime.StopIfAlive(card.hide_tween);
        card.hide_tween = Tween.Delay(card, card.ui.CompleteFeedbackDuration, OnConvertedHideComplete);
    }

    private static void OnConvertedHideComplete(RuntimeCard card)
    {
        card.panel.ResolveDeferredOverflow(card.card_index);
        DeactivateCard(card);
        card.panel.OnCardResolved?.Invoke(card.card_index);
        card.panel.NotifyAnimationCompleteIfIdle();
    }

    public void SetDeferredOverflow(int card_idx, int amount)
    {
        if (amount > 0 && TryGetCard(card_idx, out RuntimeCard card))
        {
            card.pending_overflow += amount;
        }
    }

    private void ResolveDeferredOverflow(int card_idx)
    {
        if (TryGetCard(card_idx, out RuntimeCard card))
        {
            if (card.pending_overflow > 0)
            {
                int amount = card.pending_overflow;
                card.pending_overflow = 0;

                RectTransform source = card.ui.ConvertedPuzzleTarget;
                OnOverflowReady?.Invoke(model.OverflowReward, amount, source.position);
            }
        }
    }

    private void NotifyAnimationCompleteIfIdle()
    {
        if (!IsAnimating)
        {
            OnAnimationComplete?.Invoke();
        }
    }
    #endregion
}

public sealed class MetaProgressModel
{
    public struct MetaChunk
    {
        public int card_index;
        public int amount;
    }

    public struct ProgressAllocation
    {
        public List<MetaChunk> meta_chunks;
        public int overflow_amount;
    }

    public struct ProgressApplied
    {
        public bool valid;
        public int old_total;
        public int new_total;
        public int limit;
        public bool just_completed;
    }

    private sealed class CardState
    {
        public RewardDefinition point_reward;
        public string family_key;
        public int chain_order;
        public string second_dimension_key;
        public int limit;
        public int current_total;
        public int saved_total;
    }

    private readonly EntryView[] entries;
    private readonly CardState[] cards;
    private readonly Dictionary<RewardDefinition, int> reward_to_card_index;
    private readonly Dictionary<string, int[]> family_to_chain;
    private readonly Dictionary<string, int> progression_store;
    private readonly RewardDefinition overflow_reward;

    public int CardCount => cards.Length;
    public RewardDefinition OverflowReward => overflow_reward;

    public MetaProgressModel(IReadOnlyList<EntryView> src_entries, RewardDefinition overflow_reward_src, Dictionary<string, int> saved_progress)
    {
        overflow_reward = overflow_reward_src;
        progression_store = saved_progress ?? new Dictionary<string, int>();

        int n = src_entries.Count;
        entries = new EntryView[n];
        cards = new CardState[n];
        for (int i = 0; i < n; i++)
        {
            EntryView e = src_entries[i];
            entries[i] = e;
            int saved = GetSavedTotal(e);
            cards[i] = new CardState
            {
                point_reward = e.point_reward,
                family_key = e.family_key,
                chain_order = e.chain_order,
                second_dimension_key = e.second_dimension_key,
                limit = e.limit,
                current_total = saved,
                saved_total = saved,
            };
        }

        reward_to_card_index = new Dictionary<RewardDefinition, int>(n);
        var family_temp = new Dictionary<string, List<int>>();
        for (int i = 0; i < n; i++)
        {
            CardState c = cards[i];
            if (c.point_reward != null && !reward_to_card_index.ContainsKey(c.point_reward))
            {
                reward_to_card_index[c.point_reward] = i;
            }
            if (!string.IsNullOrEmpty(c.family_key))
            {
                if (!family_temp.TryGetValue(c.family_key, out List<int> list))
                {
                    list = new List<int>();
                    family_temp[c.family_key] = list;
                }
                list.Add(i);
            }
        }

        family_to_chain = new Dictionary<string, int[]>(family_temp.Count);
        foreach (var kv in family_temp)
        {
            int[] arr = kv.Value.ToArray();
            Array.Sort(arr, CompareByChainOrder);
            family_to_chain[kv.Key] = arr;
        }
    }

    private int CompareByChainOrder(int a, int b)
    {
        int cmp = cards[a].chain_order.CompareTo(cards[b].chain_order);
        if (cmp == 0)
        {
            return a.CompareTo(b);
        }
        return cmp;
    }

    private int GetSavedTotal(EntryView entry)
    {
        string key = entry.second_dimension_key;
        if (!string.IsNullOrEmpty(key) && progression_store.TryGetValue(key, out int saved))
        {
            return saved;
        }
        return entry.initial_amount;
    }

    public EntryView GetEntry(int index)
    {
        return entries[index];
    }

    public int GetCurrentTotal(int index)
    {
        return cards[index].current_total;
    }

    public int GetLimit(int index)
    {
        return cards[index].limit;
    }

    public int FindCardIndex(RewardDefinition reward)
    {
        if (reward != null && reward_to_card_index.TryGetValue(reward, out int idx))
        {
            return idx;
        }
        return -1;
    }

    public bool IsRewardTracked(RewardDefinition reward)
    {
        return FindCardIndex(reward) >= 0;
    }

    public bool HasUnbankedProgress
    {
        get
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].current_total > cards[i].saved_total)
                {
                    return true;
                }
            }
            return false;
        }
    }

    private int GetRemainingCapacity(int index)
    {
        return Math.Max(0, cards[index].limit - cards[index].current_total);
    }

    public ProgressAllocation AllocateProgress(RewardDefinition reward, int amount)
    {
        var alloc = new ProgressAllocation { meta_chunks = new List<MetaChunk>(), overflow_amount = 0 };
        if (amount <= 0)
        {
            return alloc;
        }

        int root_idx = FindCardIndex(reward);
        if (root_idx < 0)
        {
            alloc.overflow_amount = amount;
            return alloc;
        }

        string family = cards[root_idx].family_key;
        int remaining = amount;

        if (!string.IsNullOrEmpty(family) && family_to_chain.TryGetValue(family, out int[] chain))
        {
            for (int i = 0; i < chain.Length && remaining > 0; i++)
            {
                remaining = AllocateToSingle(chain[i], remaining, alloc);
            }
        }
        else
        {
            remaining = AllocateToSingle(root_idx, remaining, alloc);
        }

        alloc.overflow_amount = remaining;
        return alloc;
    }

    private int AllocateToSingle(int idx, int remaining, ProgressAllocation alloc)
    {
        int cap = GetRemainingCapacity(idx);
        if (cap <= 0)
        {
            return remaining;
        }
        int take = Math.Min(remaining, cap);
        alloc.meta_chunks.Add(new MetaChunk { card_index = idx, amount = take });
        return remaining - take;
    }

    public ProgressApplied ApplyProgress(int index, int delta)
    {
        ProgressApplied result = default;
        if (delta <= 0)
        {
            return result;
        }
        CardState c = cards[index];
        if (c.point_reward == null || c.current_total >= c.limit)
        {
            return result;
        }
        int old_total = c.current_total;
        int new_total = Math.Min(old_total + delta, c.limit);
        c.current_total = new_total;

        result.valid = true;
        result.old_total = old_total;
        result.new_total = new_total;
        result.limit = c.limit;
        result.just_completed = new_total >= c.limit;
        return result;
    }

    public void Commit()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].saved_total = cards[i].current_total;
        }
        SaveProgression();
    }

    public void Revert()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].current_total = cards[i].saved_total;
        }
    }

    private void SaveProgression()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            CardState c = cards[i];
            if (!string.IsNullOrEmpty(c.second_dimension_key) && c.current_total > 0)
            {
                progression_store[c.second_dimension_key] = c.current_total;
            }
        }
        PlayerProgress.SaveProgression(progression_store);
    }
}
}
