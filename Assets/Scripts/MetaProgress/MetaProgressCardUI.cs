using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoWheel
{
internal enum MetaCompletionKind
{
    None,
    SkinReady,
    ConvertsToOverflow,
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class MetaProgressCardUI : MonoBehaviour
{
    [SerializeField] private RectTransform card_root;
    [SerializeField] private Image icon_image;
    [SerializeField] private TMP_Text title_label;
    [SerializeField] private TMP_Text tier_label;
    [SerializeField] private TMP_Text subtitle_label;
    [SerializeField] private Image progress_fill;
    [SerializeField] private TMP_Text count_value;
    [SerializeField] private RectTransform puzzle_target;
    [SerializeField] private Image puzzle_image;
    [SerializeField] private GameObject converted_view;
    [SerializeField] private RectTransform converted_puzzle_target;
    [SerializeField] private GameObject skin_ready_view;
    [SerializeField] private Image skin_ready_weapon;
    [SerializeField] private TMP_Text skin_ready_count;
    [SerializeField] private CanvasGroup canvas_group;

    private MetaProgressCardTiming timing;
    private Sequence activate_sequence;
    private Sequence progress_sequence;
    private Tween puzzle_punch_tween;
    private int last_displayed_count = -1;
    private int count_tween_limit;
    private Action on_progress_animation_complete;

    internal RectTransform PuzzleTarget
    {
        get
        {
            return puzzle_target;
        }
    }

    internal RectTransform ConvertedPuzzleTarget
    {
        get
        {
            return converted_puzzle_target;
        }
    }

    internal Sprite PuzzleSprite
    {
        get
        {
            return puzzle_image.sprite;
        }
    }

    private static string FormatTitleText(string title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim().ToUpperInvariant();
        }
        return "";
    }

    internal void ConfigureTiming(MetaProgressCardTiming card_timing)
    {
        timing = card_timing;
    }

    internal void SetStaticData(EntryView entry)
    {
        icon_image.sprite = entry.target_icon;
        title_label.text = FormatTitleText(entry.title_text);
        tier_label.text = entry.tier_text;
        tier_label.gameObject.SetActive(!string.IsNullOrEmpty(entry.tier_text));
        subtitle_label.text = entry.subtitle_text;
    }

    internal void SetProgressImmediate(int amount, int limit)
    {
        TweenLifetime.StopIfAlive(progress_sequence);
        int clamped = Mathf.Min(amount, limit);
        float fill = Mathf.Clamp01((float)clamped / limit);
        progress_fill.fillAmount = fill;
        TextTransformer.SetProgressCount(count_value, clamped, limit);
        last_displayed_count = clamped;
        count_tween_limit = limit;
    }

    internal void AnimateProgressTo(
        int from_amt,
        int to_amt,
        int limit,
        Action progress_animation_complete)
    {
        int from_clamped = Mathf.Min(from_amt, limit);
        int to_clamped = Mathf.Min(to_amt, limit);
        float fill_target = Mathf.Clamp01((float)to_clamped / limit);
        count_tween_limit = limit;
        on_progress_animation_complete = progress_animation_complete;

        TweenLifetime.StopIfAlive(progress_sequence);

        Tween fill_tween = Tween.Custom(
            progress_fill.fillAmount,
            fill_target,
            timing.fill_duration,
            OnFillTweenValue,
            Ease.OutCubic);

        Tween count_tween = Tween.Custom(
            (float)from_clamped,
            (float)to_clamped,
            timing.count_duration,
            OnCountTweenValue,
            Ease.OutCubic);

        progress_sequence = Sequence.Create()
            .Group(fill_tween)
            .Group(count_tween)
            .OnComplete(OnProgressSequenceComplete);
    }

    private void OnFillTweenValue(float v)
    {
        progress_fill.fillAmount = v;
    }

    private void OnCountTweenValue(float v)
    {
        int count = Mathf.RoundToInt(v);
        if (last_displayed_count != count)
        {
            last_displayed_count = count;
            TextTransformer.SetProgressCount(count_value, count, count_tween_limit);
        }
    }

    private void OnProgressSequenceComplete()
    {
        Action animation_complete = on_progress_animation_complete;
        on_progress_animation_complete = null;
        animation_complete?.Invoke();
    }

    internal void PlayActivationFeedback()
    {
        RectTransform rt = card_root;
        TweenLifetime.StopIfAlive(activate_sequence);

        float start_scale = timing.activate_start_scale;
        float fade_time = timing.activate_fade_time;

        rt.localScale = new Vector3(start_scale, start_scale, 1f);
        canvas_group.alpha = 0f;

        activate_sequence = Sequence.Create().Group(Tween.Scale(rt, Vector3.one, fade_time, Ease.OutBack));

        activate_sequence.Group(Tween.Alpha(canvas_group, 1f, fade_time, Ease.OutCubic));
    }

    internal void ShowGainFeedback()
    {
        TweenLifetime.StopIfAlive(puzzle_punch_tween);

        puzzle_target.localScale = Vector3.one;
        puzzle_punch_tween = Tween.PunchScale(puzzle_target, Vector3.one * timing.puzzle_punch_scale, timing.puzzle_punch_duration);
    }

    internal float CompleteFeedbackDuration
    {
        get
        {
            return timing.complete_feedback_duration;
        }
    }

    internal void ShowCompletion(MetaCompletionKind completion_kind, int ready_count)
    {
        if (completion_kind == MetaCompletionKind.ConvertsToOverflow)
        {
            converted_view.SetActive(true);
        }
        else
        {
            skin_ready_weapon.sprite = icon_image.sprite;
            TextTransformer.SetNumber(skin_ready_count, ready_count);
            skin_ready_view.SetActive(true);
        }
    }

    internal void ResetFeedback()
    {
        StopFeedback();
        canvas_group.alpha = 1f;
        card_root.localScale = Vector3.one;
        puzzle_target.localScale = Vector3.one;
        converted_view.SetActive(false);
        skin_ready_view.SetActive(false);
    }

    private void OnDisable()
    {
        StopFeedback();
    }

    private void StopFeedback()
    {
        TweenLifetime.StopIfAlive(activate_sequence);
        TweenLifetime.StopIfAlive(puzzle_punch_tween);
        TweenLifetime.StopIfAlive(progress_sequence);
        on_progress_animation_complete = null;
    }
}
}
