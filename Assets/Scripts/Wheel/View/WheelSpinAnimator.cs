using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

public class WheelSpinAnimator : MonoBehaviour
{

    private const float MIN_SPIN_DURATION_SECONDS = 0.1f;

    [FormerlySerializedAs("rotatingRewardLayer")]
    [SerializeField] private RectTransform rotating_reward_layer;

    [FormerlySerializedAs("spinCurve")]
    [SerializeField] private AnimationCurve spin_curve = new AnimationCurve(
        new Keyframe(0f, 0f, 3f, 3f),
        new Keyframe(0.5f, 0.85f, 0.6f, 0.6f),
        new Keyframe(1f, 1f, 0f, 0f));
    [FormerlySerializedAs("indicatorPulse")]
    [SerializeField] private IndicatorPulse indicator_pulse;

    private Tween spin_tween;
    private Action on_spin_complete;
    private int current_slice_count;
    private int last_tick_slice = -1;
    private float prev_angle_for_kick = -1f;

    public bool IsSpinning => spin_tween.isAlive;

    void Awake()
    {
        if (rotating_reward_layer == null)
        {
            Debug.LogError("WheelSpinAnimator: rotating_reward_layer not wired.", this);
            enabled = false;
            return;
        }

        if (indicator_pulse == null)
            Debug.LogError("WheelSpinAnimator: indicator_pulse not wired.", this);
    }

    public void AdoptRefs(RectTransform layer_from_view)
    {
        if (rotating_reward_layer == null && layer_from_view != null)
            rotating_reward_layer = layer_from_view;
    }

    public void TrySkipToEnd()
    {
        if (!spin_tween.isAlive) return;
        spin_tween.Complete();
    }

    public void SpinTo(int slice_index, int slice_count, float duration, float min_rotations, float max_rotations, Action on_complete)
    {
        if (slice_count <= 0 || rotating_reward_layer == null)
        {
            on_complete?.Invoke();
            return;
        }

        if (indicator_pulse != null) indicator_pulse.ResetKickFlag();

        float angle_per_slice = 360f / slice_count;
        float target_slice_angle = -(slice_index * angle_per_slice);

        float current_angle = NormalizeAngle(rotating_reward_layer.localEulerAngles.z);
        float rotations = UnityEngine.Random.Range(min_rotations, max_rotations);
        float end_angle = current_angle + (-360f * rotations);
        float end_normalized = NormalizeAngle(end_angle);
        float adjust = NormalizeAngle(target_slice_angle - end_normalized);
        if (adjust > 180f) adjust -= 360f;
        float final_end_angle = end_angle + adjust;

        on_spin_complete = on_complete;
        current_slice_count = slice_count;
        last_tick_slice  = -1;
        prev_angle_for_kick = rotating_reward_layer.localEulerAngles.z;

        if (spin_tween.isAlive) spin_tween.Stop();

        float delta_angle = Mathf.Abs(final_end_angle - current_angle);
        float reference_delta = 360f * (min_rotations + max_rotations) * 0.5f;
        float spin_duration = reference_delta > 0.01f
            ? Mathf.Max(MIN_SPIN_DURATION_SECONDS, duration * (delta_angle / reference_delta))
            : duration;

        spin_tween = Tween.LocalEulerAngles(
                rotating_reward_layer,
                new Vector3(0f, 0f, current_angle),
                new Vector3(0f, 0f, final_end_angle),
                spin_duration,
                spin_curve)
            .OnComplete(OnSpinTweenComplete);
        enabled = true;
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    private void OnSpinTweenComplete()
    {
        enabled = false;
        Action cb = on_spin_complete;
        on_spin_complete = null;
        cb?.Invoke();
    }

    void Update()
    {
        if (!spin_tween.isAlive) { enabled = false; return; }
        if (rotating_reward_layer == null) { enabled = false; return; }

        float angle = rotating_reward_layer.localEulerAngles.z;

        if (current_slice_count > 0)
        {
            float per_slice = 360f / current_slice_count;
            int current_slice = (int)(angle / per_slice);
            if (last_tick_slice >= 0 && current_slice != last_tick_slice)
            {
                if (indicator_pulse != null && prev_angle_for_kick >= 0f && Time.deltaTime > 0f)
                {
                    float d_angle = Mathf.DeltaAngle(prev_angle_for_kick, angle);
                    indicator_pulse.Tick(d_angle / Time.deltaTime);
                }
            }
            last_tick_slice = current_slice;
        }
        prev_angle_for_kick = angle;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (rotating_reward_layer == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                string n = transform.GetChild(i).name.ToLowerInvariant();
                if (n.Contains("rotating_reward_layer") || n.Contains("rotatingrewardlayer"))
                {
                    rotating_reward_layer = transform.GetChild(i) as RectTransform;
                    break;
                }
            }
        }
    }
#endif
}
