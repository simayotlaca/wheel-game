using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(WheelSpinAnimator))]
public class WheelView : MonoBehaviour
{
    [Header("Wheel Parts")]
    [SerializeField] private RectTransform rotatingRewardLayer;
    [SerializeField] private Image wheelBaseImage;
    [SerializeField] private Image wheelFrameImage;
    [SerializeField] private Image wheelIndicatorImage;
    [SerializeField] private SliceView slicePrefab;

    [SerializeField] private WheelSpinAnimator spinAnimator;

    private SliceView[] slice_views;
    private int slice_count;
    private readonly SliceHighlightAnimator highlight_animator = new SliceHighlightAnimator();

    public bool IsSpinning => spinAnimator != null && spinAnimator.IsSpinning;

    void Awake()
    {
        if (spinAnimator == null)
        {
            Debug.LogError("WheelView: spinAnimator not wired — run Vertigo/Build/Full Rebuild.", this);
            enabled = false;
            return;
        }
        spinAnimator.AdoptRefs(rotatingRewardLayer);
    }

    public void TrySkipToEnd()
    {
        if (spinAnimator != null) spinAnimator.TrySkipToEnd();
    }

    public void BuildForZone(ZoneConfig zone, int currentZone, SliceDefinition[] slices)
    {
        if (zone == null)
            throw new System.ArgumentNullException(nameof(zone));
        if (slices == null)
            throw new System.ArgumentNullException(nameof(slices));
        if (rotatingRewardLayer == null)
            throw new System.InvalidOperationException("WheelView: rotatingRewardLayer not wired.");
        if (slicePrefab == null)
            throw new System.InvalidOperationException("WheelView: slicePrefab not wired.");

        Sprite baseSprite      = zone.wheelBase;
        Sprite frameSprite     = zone.wheelFrame;
        Sprite indicatorSprite = zone.wheelIndicator;
        Color frameTint = zone.frameTint;

        if (baseSprite == null)
            throw new System.InvalidOperationException($"WheelView: wheelBase sprite missing for zone {currentZone} ({zone.type}).");
        if (indicatorSprite == null)
            throw new System.InvalidOperationException($"WheelView: wheelIndicator sprite missing for zone {currentZone} ({zone.type}).");

        if (wheelBaseImage != null)
        {
            wheelBaseImage.sprite = baseSprite;
        }
        if (wheelFrameImage != null)
        {
            bool hasFrame = frameSprite != null;
            wheelFrameImage.enabled = hasFrame;
            if (hasFrame)
            {
                wheelFrameImage.sprite = frameSprite;
                wheelFrameImage.color = frameTint;
            }
        }
        if (wheelIndicatorImage != null)
        {
            wheelIndicatorImage.sprite = indicatorSprite;
        }

        int needed = slices.Length;
        EnsureSliceCapacity(needed);

        for (int i = 0; i < slice_views.Length; i++)
        {
            if (slice_views[i] != null)
                slice_views[i].gameObject.SetActive(i < needed);
        }

        slice_count = needed;
        if (needed == 0) return;

        float anglePerSlice = 360f / needed;
        for (int i = 0; i < needed; i++)
        {
            SliceDefinition slice = slices[i];
            SliceView view = slice_views[i];
            view.SetData(slice, currentZone, zone.type, i * anglePerSlice);
        }
    }

    void EnsureSliceCapacity(int needed)
    {
        if (slice_views == null) slice_views = Array.Empty<SliceView>();
        if (slice_views.Length >= needed && needed > 0) return;

        int newSize = Mathf.Max(needed, slice_views.Length);
        SliceView[] resized = new SliceView[newSize];
        Array.Copy(slice_views, resized, slice_views.Length);

        for (int i = slice_views.Length; i < newSize; i++)
        {
            SliceView v = Instantiate(slicePrefab, rotatingRewardLayer);
            resized[i] = v;
        }
        slice_views = resized;
        highlight_animator.Init(slice_views);
    }

    public void SpinTo(int slice_index, float duration, float min_rotations, float max_rotations, Action on_complete)
    {
        if (slice_count == 0 || spinAnimator == null)
        {
            on_complete?.Invoke();
            return;
        }
        spinAnimator.SpinTo(slice_index, slice_count, duration, min_rotations, max_rotations, on_complete);
    }

    public void HighlightSlice(int sliceIndex)
    {
        if (sliceIndex < 0 || sliceIndex >= slice_count) return;
        highlight_animator.HighlightWinner(sliceIndex);
    }

    public bool TryGetSliceWorldPosition(int sliceIndex, out Vector3 world)
    {
        world = Vector3.zero;
        if (slice_views == null) return false;
        if (sliceIndex < 0 || sliceIndex >= slice_count) return false;
        SliceView v = slice_views[sliceIndex];
        if (v == null) return false;
        world = v.IconWorldPosition;
        return true;
    }

    public void DimNonWinners(int winnerIndex) => highlight_animator.DimNonWinners(winnerIndex);

    public void UndimAll() => highlight_animator.UndimAll();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (rotatingRewardLayer == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                string n = transform.GetChild(i).name.ToLowerInvariant();
                if (n.Contains("rotating_reward_layer") || n.Contains("rotatingrewardlayer"))
                {
                    rotatingRewardLayer = transform.GetChild(i) as RectTransform;
                    break;
                }
            }
        }
        if (spinAnimator == null) spinAnimator = GetComponent<WheelSpinAnimator>();

        ApplyComplianceSettings();
    }

    private void ApplyComplianceSettings()
    {
        wheelBaseImage     .MarkDecorative();
        wheelFrameImage    .MarkDecorative();
        wheelIndicatorImage.MarkDecorative();
    }
#endif
}
