using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class WheelController : MonoBehaviour
{
    private const float FullCircleDegrees = 360f;

    [Header("Flow")]
    [SerializeField] private RunSession controller;

    [Header("Wheel Parts")]
    [SerializeField] private RectTransform rotating_reward_layer;
    [SerializeField] private Image wheel_base_image;
    [SerializeField] private Image wheel_indicator_image;
    [SerializeField] private SliceView slice_prefab;

    private ObjectPool<SliceView> slice_pool;
    private Tween spin_tween;
    private RunEventPass event_pass;

    void Awake()
    {
        slice_pool = new ObjectPool<SliceView>(slice_prefab, rotating_reward_layer, 0);
        event_pass = new RunEventPass(controller.Events);
    }

    private void OnEnable()
    {
        event_pass.Subscribe<RunPendingClearedEvent>(HandlePendingCleared);
        event_pass.Subscribe<RunDeathHitEvent>(HandleDeathHit);
    }

    private void OnDisable()
    {
        event_pass.ReleaseAll();
        TweenLifetime.StopIfAlive(spin_tween);
    }

    internal void BuildForZone(WheelVisual zone, WheelResultPicker.ComputedSlot[] slots)
    {
        if (isActiveAndEnabled)
        {
            BuildSlices(zone, slots);
        }
    }

    internal void RevealSlice(int slice_idx)
    {
        if (isActiveAndEnabled)
        {
            HighlightSlice(slice_idx);
            ShineSlice(slice_idx);
        }
    }

    private void HandlePendingCleared(RunPendingClearedEvent _)
    {
        CleanupRunView();
    }

    private void HandleDeathHit(RunDeathHitEvent _)
    {
        CleanupRunView();
    }

    private void CleanupRunView()
    {
        TweenLifetime.StopIfAlive(spin_tween);
        ClearShine();
    }

    private void BuildSlices(WheelVisual zone, WheelResultPicker.ComputedSlot[] slots)
    {
        ClearShine();
        rotating_reward_layer.localEulerAngles = Vector3.zero;
        wheel_base_image.sprite = zone.wheelBase;
        wheel_indicator_image.sprite = zone.wheelIndicator;

        int needed = slots.Length;
        slice_pool.EnsureCapacity(needed);
        slice_pool.ReleaseAll();

        float angle_per_slice = FullCircleDegrees / needed;
        for (int i = 0; i < needed; i++)
        {
            WheelResultPicker.ComputedSlot slot = slots[i];
            SliceView slice = slice_pool.Acquire();
            slice.transform.SetSiblingIndex(i + 1);
            slice.SetData(slot, i * angle_per_slice);
        }
    }

    internal void SpinToSlice(int slice_idx, WheelSpinTiming timing)
    {
        int slice_count = slice_pool.ActiveCount;

        int min_rotations = Mathf.RoundToInt(timing.minFullRotations);
        int max_rotations = Mathf.RoundToInt(timing.maxFullRotations);
        int rotations = UnityEngine.Random.Range(min_rotations, max_rotations + 1);
        float end_angle = -(rotations * FullCircleDegrees + slice_idx * (FullCircleDegrees / slice_count));

        TweenLifetime.StopIfAlive(spin_tween);

        spin_tween = Tween.LocalEulerAngles(
                rotating_reward_layer,
                Vector3.zero,
                new Vector3(0f, 0f, end_angle),
                timing.duration,
                timing.curve)
            .OnComplete(OnSpinTweenComplete);
    }

    private void HighlightSlice(int slice_idx)
    {
        GetSlice(slice_idx).Highlight();
    }

    private void ShineSlice(int win_idx)
    {
        int n = slice_pool.ActiveCount;
        for (int i = 0; i < n; i++)
        {
            SliceView slice = slice_pool.GetActive(i);
            slice.SetDimmed(i != win_idx);
        }
    }

    private void ClearShine()
    {
        int n = slice_pool.ActiveCount;
        for (int i = 0; i < n; i++)
        {
            SliceView slice = slice_pool.GetActive(i);
            slice.SetDimmed(false);
        }
    }

    internal Vector3 GetSliceWorldPosition(int slice_idx)
    {
        return GetSlice(slice_idx).IconWorldPosition;
    }

    private SliceView GetSlice(int index)
    {
        return slice_pool.GetActive(index);
    }

    private void OnSpinTweenComplete()
    {
        controller.NotifyWheelSpinCompleted();
    }

}
}
