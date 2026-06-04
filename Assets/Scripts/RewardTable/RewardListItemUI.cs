using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoWheel
{
[RequireComponent(typeof(CanvasGroup))]
public class RewardListItemUI : MonoBehaviour
{
    [System.Serializable]
    private struct ListLayoutOverride
    {
        public string reward_id;
        public string visual_family;
        public Vector2 icon_size;
        public Vector2 icon_anchored_position;
        public Vector2 amount_anchored_position;
        public float amount_font_size;

        internal bool Matches(RewardDefinition reward)
        {
            if (!string.IsNullOrEmpty(reward_id))
            {
                return reward.rewardId == reward_id;
            }

            if (!string.IsNullOrEmpty(visual_family))
            {
                return reward.visualFamily == visual_family;
            }

            return false;
        }
    }

    private const int GoldPriority = 0;
    private const int CashPriority = 1;
    private const int SpecialPriority = 2;
    private const int AllCardsPriority = 3;
    private const int OtherPriority = 4;

    [SerializeField] private Image icon_image;
    [SerializeField] private TMP_Text reward_amount_value;
    [SerializeField] private ListLayoutOverride[] layout_overrides;

    private CanvasGroup canvas_group;
    private RewardDefinition reward_definition;
    private int displayed_amount;
    private float next_text_pulse_time;
    private CurrencyConfig currency_config;
    private RewardListItemTiming timing;
    private Vector3 amount_rest_scale = Vector3.one;
    private Vector2 icon_base_size;
    private Vector2 icon_base_anchored_position;
    private Vector2 amount_base_anchored_position;
    private float amount_base_font_size;

    private Tween count_tween;
    private Tween pulse_tween;
    private Action on_count_complete;
    private bool count_tween_complete;
    private int count_tween_target;

    internal RewardDefinition Reward
    {
        get
        {
            return reward_definition;
        }
    }

    internal int CategoryPriority { get; private set; }

    internal RectTransform IconRectTransform
    {
        get
        {
            return icon_image.rectTransform;
        }
    }

    void Awake()
    {
        canvas_group = GetComponent<CanvasGroup>();
        amount_rest_scale = reward_amount_value.transform.localScale;
        icon_base_size = icon_image.rectTransform.sizeDelta;
        icon_base_anchored_position = icon_image.rectTransform.anchoredPosition;
        amount_base_anchored_position = reward_amount_value.rectTransform.anchoredPosition;
        amount_base_font_size = reward_amount_value.fontSize;
    }

    internal void SetCurrencyConfig(CurrencyConfig config)
    {
        currency_config = config;
    }

    internal void ConfigureTiming(RewardListItemTiming item_timing)
    {
        timing = item_timing;
    }

    internal void SetData(RewardDefinition reward, int amount, Action count_complete)
    {
        if (reward_definition != reward)
        {
            ApplyRewardVisual(reward);
            ResetAmountDisplayState();
        }

        StartAmountCountUp(amount, count_complete);
    }

    internal void PrepareTarget(RewardDefinition reward)
    {
        ApplyRewardVisual(reward);
        ResetAmountDisplayState();
        ResetAmountTextVisual();
        TextTransformer.Clear(reward_amount_value);
    }

    internal void SetVisible(bool visible)
    {
        canvas_group.alpha = visible ? 1f : 0f;
        canvas_group.blocksRaycasts = visible;
        canvas_group.interactable = visible;
    }

    void OnDisable()
    {
        TweenLifetime.StopIfAlive(count_tween);
        TweenLifetime.StopIfAlive(pulse_tween);
        on_count_complete = null;
        ResetAmountTextVisual();
    }

    private void ApplyRewardVisual(RewardDefinition reward)
    {
        reward_definition = reward;
        CategoryPriority = ResolveCategoryPriority(reward);

        Sprite spr = reward.ResolveListIcon();
        icon_image.sprite = spr;
        icon_image.enabled = spr != null;
        ApplyLayoutOverride(reward);
    }

    private void ApplyLayoutOverride(RewardDefinition reward)
    {
        RectTransform icon_rt = icon_image.rectTransform;
        icon_rt.sizeDelta = icon_base_size;
        icon_rt.anchoredPosition = icon_base_anchored_position;
        reward_amount_value.rectTransform.anchoredPosition = amount_base_anchored_position;
        reward_amount_value.fontSize = amount_base_font_size;

        if (layout_overrides == null)
        {
            return;
        }

        for (int i = 0; i < layout_overrides.Length; i++)
        {
            ListLayoutOverride layout = layout_overrides[i];
            if (layout.Matches(reward))
            {
                if (layout.icon_size.x > 0f && layout.icon_size.y > 0f)
                {
                    icon_rt.sizeDelta = layout.icon_size;
                }
                icon_rt.anchoredPosition = layout.icon_anchored_position;
                reward_amount_value.rectTransform.anchoredPosition = layout.amount_anchored_position;
                if (layout.amount_font_size > 0f)
                {
                    reward_amount_value.fontSize = layout.amount_font_size;
                }
                return;
            }
        }
    }

    private int ResolveCategoryPriority(RewardDefinition reward)
    {
        if (reward == currency_config.goldReward)
        {
            return GoldPriority;
        }

        switch (reward.slotCategory)
        {
            case SlotCategory.Currency: return CashPriority;
            case SlotCategory.Special:  return SpecialPriority;
            case SlotCategory.AllCards: return AllCardsPriority;
            case SlotCategory.Other:    return OtherPriority;
        }

        return OtherPriority;
    }

    private void ResetAmountDisplayState()
    {
        TweenLifetime.StopIfAlive(count_tween);
        TweenLifetime.StopIfAlive(pulse_tween);
        on_count_complete = null;
        count_tween_complete = false;
        count_tween_target = 0;
        displayed_amount = 0;
        next_text_pulse_time = 0f;
    }

    private void StartAmountCountUp(int amount, Action count_complete)
    {
        on_count_complete = count_complete;
        count_tween_complete = false;
        count_tween_target = amount;

        if (amount == displayed_amount)
        {
            count_tween_complete = true;
            CompleteCountUpWhenReady();
            return;
        }

        TweenLifetime.StopIfAlive(count_tween);

        count_tween = Tween.Custom(
                (float)displayed_amount,
                (float)amount,
                timing.reward_amount_count_up_duration,
                OnCountTweenValue)
            .OnComplete(HandleCountTweenComplete);
    }

    private void HandleCountTweenComplete()
    {
        WriteAmountValue(count_tween_target);
        count_tween_complete = true;
        CompleteCountUpWhenReady();
    }

    private void OnCountTweenValue(float val)
    {
        WriteAmountValue(Mathf.RoundToInt(val));
    }

    private void WriteAmountValue(int value)
    {
        if (value == displayed_amount)
        {
            return;
        }

        bool is_incremented = value > displayed_amount;
        displayed_amount = value;

        TextTransformer.SetThousandsNumber(reward_amount_value, value);

        if (is_incremented)
        {
            TryPlayAmountPulse(value);
        }
    }

    private void TryPlayAmountPulse(int value)
    {
        if (value <= 0)
        {
            return;
        }

        float now = Time.time;
        if (now < next_text_pulse_time)
        {
            return;
        }

        next_text_pulse_time = now + timing.reward_text_tick_pulse_min_interval;
        float duration = timing.reward_text_tick_pulse_duration;
        float strength = timing.reward_text_tick_pulse_strength;
        if (strength <= 0f || duration <= 0f)
        {
            return;
        }

        Transform amount_transform = reward_amount_value.transform;
        TweenLifetime.StopIfAlive(pulse_tween);
        pulse_tween = Tween.PunchScale(amount_transform, new Vector3(strength, strength, 0f), duration)
            .OnComplete(HandlePulseComplete);
    }

    private void HandlePulseComplete()
    {
        CompleteCountUpWhenReady();
    }

    private void CompleteCountUpWhenReady()
    {
        if (count_tween_complete && !pulse_tween.isAlive)
        {
            Action complete = on_count_complete;
            on_count_complete = null;
            complete?.Invoke();
        }
    }

    private void ResetAmountTextVisual()
    {
        TweenLifetime.StopIfAlive(pulse_tween);
        reward_amount_value.transform.localScale = amount_rest_scale;
    }

    internal void Clear()
    {
        SetVisible(true);
        reward_definition = null;
        ResetAmountDisplayState();
        icon_image.enabled = false;

        ResetAmountTextVisual();
        TextTransformer.Clear(reward_amount_value);
    }
}
}
