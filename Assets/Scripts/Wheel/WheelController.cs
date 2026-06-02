using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class WheelController : MonoBehaviour
{
    [Header("Wheel Parts")]
    [SerializeField] private RectTransform rotating_reward_layer;
    [SerializeField] private Image wheel_base_image;
    [SerializeField] private Image wheel_indicator_image;
    [SerializeField] private SliceView slice_prefab;

    [Header("Indicator Kick")]
    [SerializeField] private RectTransform indicator_target;
    [SerializeField] private ConfigAnimation anim_config;

    private ObjectPool<SliceView> slice_pool;
    private Tween spin_tween;
    private Action on_spin_complete;
    private int last_tick_slice = -1;
    private bool kick_in_flight;

    private int ActiveSliceCount => slice_pool != null ? slice_pool.ActiveCount : 0;

    void Awake()
    {
        if (indicator_target != null)
        {
            indicator_target.localRotation = Quaternion.identity;
        }
        slice_pool = new ObjectPool<SliceView>(slice_prefab, rotating_reward_layer, 1);
    }

    public void BuildForZone(WheelVisual zone, WheelResultPicker.ComputedSlot[] slots)
    {
        wheel_base_image.sprite = zone.wheelBase;
        wheel_indicator_image.sprite = zone.wheelIndicator;

        int needed = slots.Length;
        slice_pool.EnsureCapacity(needed);
        slice_pool.ReleaseAll();

        if (needed != 0)
        {
            float angle_per_slice = 360f / needed;
            for (int i = 0; i < needed; i++)
            {
                WheelResultPicker.ComputedSlot slot = slots[i];
                SliceView slice = slice_pool.Acquire();
                if (slice == null)
                {
                    return;
                }
                slice.Initialize(anim_config);
                slice.transform.SetSiblingIndex(i + 1);
                slice.SetData(slot, i * angle_per_slice);
            }
        }
    }

    public void SpinTo(int slice_idx, float dur, float min_dur, float min_rot, float max_rot, Action done_cb)
    {
        int slice_count = ActiveSliceCount;
        if (slice_count <= 0)
        {
            done_cb.Invoke();
            return;
        }

        ResetIndicatorKick();

        float angle_per_slice = 360f / slice_count;
        float target_slice_angle = -(slice_idx * angle_per_slice);

        float current_angle = NormalizeAngle(rotating_reward_layer.localEulerAngles.z);
        float rotations = UnityEngine.Random.Range(min_rot, max_rot);
        float end_angle = current_angle + (-360f * rotations);
        float end_normalized = NormalizeAngle(end_angle);
        float adjust = NormalizeAngle(target_slice_angle - end_normalized);
        if (adjust > 180f)
        {
            adjust -= 360f;
        }
        float final_end_angle = end_angle + adjust;

        on_spin_complete = done_cb;
        last_tick_slice = -1;

        TweenLifetime.StopIfAlive(spin_tween);

        float delta_angle = Mathf.Abs(final_end_angle - current_angle);
        float reference_delta = 360f * (min_rot + max_rot) * 0.5f;
        float spin_dur = dur;
        if (reference_delta > 0.01f)
        {
            spin_dur = Mathf.Max(min_dur, dur * (delta_angle / reference_delta));
        }

        spin_tween = Tween.LocalEulerAngles(
                rotating_reward_layer,
                new Vector3(0f, 0f, current_angle),
                new Vector3(0f, 0f, final_end_angle),
                spin_dur,
                anim_config.wheelSpinCurve)
            .OnUpdate(this, (target, _) => target.HandleSpinTweenUpdate())
            .OnComplete(OnSpinTweenComplete);
    }

    public void HighlightSlice(int slice_idx)
    {
        if (slice_idx >= 0 && slice_idx < ActiveSliceCount)
        {
            SliceView slice = GetSlice(slice_idx);
            if (slice != null)
            {
                slice.Highlight();
            }
        }
    }

    public void ShineSlice(int win_idx)
    {
        int n = slice_pool.ActiveCount;
        for (int i = 0; i < n; i++)
        {
            SliceView slice = slice_pool.GetActive(i);
            if (slice != null)
            {
                slice.SetDimmed(i != win_idx);
            }
        }
    }

    public void ClearShine()
    {
        int n = slice_pool.ActiveCount;
        for (int i = 0; i < n; i++)
        {
            SliceView slice = slice_pool.GetActive(i);
            if (slice != null)
            {
                slice.SetDimmed(false);
            }
        }
    }

    public bool TryGetSliceWorldPosition(int slice_idx, out Vector3 world)
    {
        world = Vector3.zero;
        SliceView slice = GetSlice(slice_idx);
        if (slice != null)
        {
            world = slice.IconWorldPosition;
            return true;
        }
        return false;
    }

    private SliceView GetSlice(int index)
    {
        if (index >= 0 && index < ActiveSliceCount)
        {
            return slice_pool.GetActive(index);
        }
        return null;
    }

    private void OnSpinTweenComplete()
    {
        ResetIndicatorKick();
        Action cb = on_spin_complete;
        on_spin_complete = null;
        if (cb != null)
        {
            cb.Invoke();
        }
    }

    private void HandleSpinTweenUpdate()
    {
        float angle = rotating_reward_layer.localEulerAngles.z;

        int slice_count = ActiveSliceCount;
        if (slice_count > 0)
        {
            float per_slice = 360f / slice_count;
            int current_slice = (int)(angle / per_slice);
            if (last_tick_slice >= 0 && current_slice != last_tick_slice)
            {
                TriggerIndicatorKick();
            }
            last_tick_slice = current_slice;
        }
    }

    private void ResetIndicatorKick()
    {
        kick_in_flight = false;
        if (indicator_target != null)
        {
            Tween.StopAll(onTarget: indicator_target);
            indicator_target.localRotation = Quaternion.identity;
        }
    }

    private void TriggerIndicatorKick()
    {
        if (indicator_target == null)
        {
            return;
        }

        if (!kick_in_flight)
        {
            kick_in_flight = true;
            Tween.LocalRotation(indicator_target, new Vector3(0f, 0f, anim_config.kickAngle), anim_config.kickDuration, Ease.OutQuad)
                .OnComplete(OnKickPeak);
        }
    }

    private void OnKickPeak()
    {
        Tween.LocalRotation(indicator_target, Vector3.zero, anim_config.kickDuration, Ease.OutQuad)
            .OnComplete(OnKickReturn);
    }

    private void OnKickReturn()
    {
        indicator_target.localRotation = Quaternion.identity;
        kick_in_flight = false;
    }

    private static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a < 0f)
        {
            a += 360f;
        }
        return a;
    }

}
}
