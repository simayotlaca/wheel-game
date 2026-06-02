using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoWheel
{
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
    [SerializeField] private ConfigAnimation anim_config;

    private Sequence activate_sequence;
    private Sequence progress_sequence;
    private Tween puzzle_punch_tween;
    private int last_displayed_count = -1;
    private int count_tween_limit;
    private Action<float> on_fill_tween_value;
    private Action<float> on_count_tween_value;
    private Action on_completion_feedback;
    private Action on_animation_complete;

    public RectTransform PuzzleTarget => puzzle_target;

    public RectTransform ConvertedPuzzleTarget => converted_puzzle_target;

    public Sprite PuzzleSprite => puzzle_image.sprite;

    public bool IsProgressAnimating => progress_sequence.isAlive;

    private void Awake()
    {
        on_fill_tween_value = OnFillTweenValue;
        on_count_tween_value = OnCountTweenValue;
    }

    private static string FormatTitleText(string title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim().ToUpperInvariant();
        }
        return "";
    }

    public void SetStaticData(EntryView entry)
    {
        icon_image.sprite = entry.target_icon;
        title_label.text = FormatTitleText(entry.title_text);
        tier_label.text = entry.tier_text;
        tier_label.gameObject.SetActive(!string.IsNullOrEmpty(entry.tier_text));
        subtitle_label.text = entry.subtitle_text;
    }

    public void SetProgressImmediate(int amount, int limit)
    {
        TweenLifetime.StopIfAlive(progress_sequence);
        int clamped = Mathf.Min(amount, limit);
        float fill = Mathf.Clamp01((float)clamped / limit);
        progress_fill.fillAmount = fill;
        count_value.SetText("{0} / {1}", clamped, limit);
        last_displayed_count = clamped;
        count_tween_limit = limit;
    }

    public void AnimateProgressTo(int from_amt, int to_amt, int limit, bool will_complete, Action complete_cb, Action anim_done_cb)
    {
        int from_clamped = Mathf.Min(from_amt, limit);
        int to_clamped = Mathf.Min(to_amt, limit);
        float fill_target = Mathf.Clamp01((float)to_clamped / limit);
        count_tween_limit = limit;
        on_completion_feedback = will_complete ? complete_cb : null;
        on_animation_complete = anim_done_cb;

        TweenLifetime.StopIfAlive(progress_sequence);

        Tween fill_tween = Tween.Custom(progress_fill.fillAmount, fill_target, anim_config.metaFillDuration, on_fill_tween_value, Ease.OutCubic);
        fill_tween.OnComplete(OnFillTweenComplete);

        Tween count_tween = Tween.Custom((float)from_clamped, (float)to_clamped, anim_config.metaCountDuration, on_count_tween_value, Ease.OutCubic);

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
            count_value.SetText("{0} / {1}", count, count_tween_limit);
        }
    }

    private void OnFillTweenComplete()
    {
        if (on_completion_feedback != null)
        {
            Action cb = on_completion_feedback;
            on_completion_feedback = null;
            cb.Invoke();
        }
    }

    private void OnProgressSequenceComplete()
    {
        if (on_animation_complete != null)
        {
            Action cb = on_animation_complete;
            on_animation_complete = null;
            cb.Invoke();
        }
    }

    public void PlayActivationFeedback()
    {
        RectTransform rt = card_root;
        TweenLifetime.StopIfAlive(activate_sequence);

        float start_scale = anim_config.metaCardActivateStartScale;
        float fade_time   = anim_config.metaCardActivateFadeTime;

        rt.localScale = new Vector3(start_scale, start_scale, 1f);
        canvas_group.alpha = 0f;

        activate_sequence = Sequence.Create()
            .Group(Tween.Scale(rt, Vector3.one, fade_time, Ease.OutBack));

        activate_sequence.Group(Tween.Alpha(canvas_group, 1f, fade_time, Ease.OutCubic));
    }

    public void ShowGainFeedback()
    {
        TweenLifetime.StopIfAlive(puzzle_punch_tween);

        puzzle_target.localScale = Vector3.one;
        puzzle_punch_tween = Tween.PunchScale(puzzle_target, Vector3.one * anim_config.metaCardPuzzlePunchScale, anim_config.metaCardPuzzlePunchDuration);
    }

    public float CompleteFeedbackDuration => anim_config.metaCardPopupFadeIn + anim_config.metaCardPopupHold + anim_config.metaCardPopupFadeOut;

    public void ShowConvertedState()
    {
        if (converted_view != null)
        {
            converted_view.SetActive(true);
        }
    }

    public void ShowSkinReady(int count)
    {
        if (skin_ready_view == null)
        {
            return;
        }
        if (skin_ready_weapon != null)
        {
            skin_ready_weapon.sprite = icon_image.sprite;
        }
        if (skin_ready_count != null)
        {
            skin_ready_count.text = count.ToString();
        }
        skin_ready_view.SetActive(true);
    }

    private void HideSkinReady()
    {
        if (skin_ready_view != null)
        {
            skin_ready_view.SetActive(false);
        }
    }

    public void ResetFeedback()
    {
        TweenLifetime.StopIfAlive(activate_sequence);
        TweenLifetime.StopIfAlive(puzzle_punch_tween);
        TweenLifetime.StopIfAlive(progress_sequence);
        on_completion_feedback = null;
        on_animation_complete = null;
        canvas_group.alpha = 1f;
        card_root.localScale = Vector3.one;
        progress_fill.color = anim_config.metaCardProgressIdleColor;
        puzzle_target.localScale = Vector3.one;
        if (converted_view != null)
        {
            converted_view.SetActive(false);
        }
        HideSkinReady();
    }

    private void OnDisable()
    {
        TweenLifetime.StopIfAlive(activate_sequence);
        TweenLifetime.StopIfAlive(puzzle_punch_tween);
        TweenLifetime.StopIfAlive(progress_sequence);
        on_completion_feedback = null;
        on_animation_complete = null;
    }
}
}
