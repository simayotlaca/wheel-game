using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliceView : MonoBehaviour
{
    private const float amount_pop_start_scale = 0.7f;
    private const float amount_pop_duration = 0.32f;

    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private RectTransform pivot;
    [SerializeField] private RectTransform uprightGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image winGlowOverlay;
    [SerializeField] private Image dimOverlay;

    public Image IconImage => iconImage;
    public TMP_Text AmountText => amountText;
    public RectTransform UprightGroup => uprightGroup;
    public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;
    public Vector3 IconWorldPosition => iconImage != null ? iconImage.transform.position : transform.position;

    private float current_slice_angle;
    private Tween amount_pop_tween;

    private float current_glow;
    private float target_glow;
    private float auto_fade_at = -1f;

    private float current_dim;
    private Tween dim_tween;
    private Color amount_base_color = Color.white;
    private bool base_colors_cached;

    void Awake()
    {
        if (animConfig == null)
            throw new System.InvalidOperationException("SliceView: animConfig not wired.");
        if (winGlowOverlay == null)
            throw new System.InvalidOperationException("SliceView: winGlowOverlay not wired.");
        if (dimOverlay == null)
            throw new System.InvalidOperationException("SliceView: dimOverlay not wired.");
        winGlowOverlay.canvasRenderer.SetAlpha(0f);
        dimOverlay.canvasRenderer.SetAlpha(0f);
    }

    void Update()
    {
        GlowTick(Time.deltaTime);
    }

    public void SetDimmed(bool dimmed) => DimSetTarget(dimmed);

    public void SetData(SliceDefinition slice, int currentZone, ZoneType zoneType, float angleDegrees)
    {
        DimReset();
        GlowReset();

        current_slice_angle = angleDegrees;

        if (pivot != null)
        {
            Vector3 rot = pivot.localEulerAngles;
            rot.z = angleDegrees;
            pivot.localEulerAngles = rot;
        }

        if (slice == null || slice.reward == null)
        {
            if (iconImage != null) iconImage.enabled = false;
            if (amountText != null) amountText.text = string.Empty;
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = slice.reward.wheelIcon != null ? slice.reward.wheelIcon : slice.reward.icon;
        }

        if (amountText != null)
        {
            int displayAmount = slice.reward.isDeath ? 0 : slice.reward.ComputeAmount(currentZone, slice.amount);
            amountText.text = RewardDisplayFormatter.Format(slice.reward, displayAmount);
            PopAmountText();
        }

        WheelSliceContentLayout.Apply(this, slice.reward);
        DimCacheBaseColorsIfNeeded();
        DimApply();
        if (amountText != null && amountText.rectTransform.localEulerAngles.z != 0f)
            amountText.rectTransform.localEulerAngles = Vector3.zero;
    }

    private void PopAmountText()
    {
        if (amountText == null || string.IsNullOrEmpty(amountText.text)) return;

        if (amount_pop_tween.isAlive) amount_pop_tween.Stop();

        RectTransform rt = amountText.rectTransform;
        rt.localScale = Vector3.one * amount_pop_start_scale;
        amount_pop_tween = Tween.Scale(rt, Vector3.one, amount_pop_duration, Ease.OutBack);
    }

    public void Highlight() => GlowTrigger();

    private void GlowTrigger()
    {
        target_glow = 1f;
        current_glow = 1f;
        GlowApply();
        auto_fade_at = Time.unscaledTime + animConfig.glowHoldSeconds;
    }

    private void GlowReset()
    {
        current_glow = 0f;
        target_glow = 0f;
        auto_fade_at = -1f;
        GlowApply();
    }

    private void GlowApply()
    {
        winGlowOverlay.canvasRenderer.SetAlpha(current_glow * animConfig.glowMaxAlpha);
    }

    private void GlowTick(float deltaTime)
    {
        if (auto_fade_at > 0f && Time.unscaledTime >= auto_fade_at)
        {
            target_glow = 0f;
            auto_fade_at = -1f;
        }
        if (current_glow == target_glow) return;
        current_glow = Mathf.MoveTowards(current_glow, target_glow, animConfig.glowSpeed * deltaTime);
        GlowApply();
    }

    private void DimSetTarget(bool dimmed)
    {
        float to = dimmed ? 1f : 0f;
        if (Mathf.Approximately(current_dim, to)) return;
        if (dim_tween.isAlive) dim_tween.Stop();
        float dur = dimmed ? animConfig.dimInDuration : animConfig.dimOutDuration;
        dim_tween = Tween.Custom(current_dim, to, dur, DimSetValue, Ease.OutCubic);
    }

    private void DimSetValue(float value)
    {
        current_dim = value;
        DimApply();
    }

    private void DimReset()
    {
        if (dim_tween.isAlive) dim_tween.Stop();
        current_dim = 0f;
    }

    private void DimApply()
    {
        dimOverlay.canvasRenderer.SetAlpha(current_dim * animConfig.dimMaxAlpha);
        if (iconImage != null)
            iconImage.canvasRenderer.SetColor(Color.Lerp(Color.white, animConfig.dimTint, current_dim));
        if (amountText != null)
            amountText.color = Color.Lerp(amount_base_color, animConfig.dimTint * amount_base_color, current_dim);
    }

    private void DimCacheBaseColorsIfNeeded()
    {
        if (base_colors_cached) return;
        if (amountText != null) amount_base_color = amountText.color;
        base_colors_cached = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {

        if (pivot == null) pivot = transform as RectTransform;

        if (uprightGroup == null && iconImage != null)
        {
            Transform t = iconImage.transform.parent;
            if (t != null)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("upright") || n.Contains("button")
                    || n.Contains("slot_content") || n.Contains("wheel_slot_content"))
                    uprightGroup = t as RectTransform;
            }
        }

        ApplyComplianceSettings();
    }

    private void ApplyComplianceSettings()
    {
        iconImage     .MarkDecorative();
        winGlowOverlay.MarkDecorative();
        dimOverlay    .MarkDecorative();
    }
#endif
}
