using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class SliceView : MonoBehaviour
{
    private const float VisibleAlphaEpsilon = 0.001f;

    [System.Serializable]
    private struct IconLayoutOverride
    {
        public string reward_id;
        public string visual_family;
        public RewardVisualCategory[] categories;
        public Vector2 size;
        public Vector2 anchored_position;
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

            if (categories != null)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    if (reward.visualCategory == categories[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [SerializeField] private RectTransform pivot;
    [SerializeField] private Image icon_image;
    [SerializeField] private TMP_Text amount_value;
    [SerializeField] private Image win_glow_overlay;
    [SerializeField] private Image dim_overlay;

    [Header("Icon Layout Overrides")]
    [SerializeField] private IconLayoutOverride[] icon_layout_overrides;

    [Header("Dim")]
    [SerializeField, Min(0f)] private float dim_in_duration;
    [SerializeField, Min(0f)] private float dim_out_duration;
    [SerializeField, Range(0f, 1f)] private float dim_max_alpha;
    [SerializeField] private Color dim_tint;

    [Header("Glow")]
    [SerializeField, Min(0f)] private float glow_speed;
    [SerializeField, Range(0f, 1f)] private float glow_max_alpha;
    [SerializeField, Min(0f)] private float glow_hold_seconds;

    private Sequence glow_sequence;
    private Tween dim_tween;
    private float current_dim;
    private Color icon_base_color = Color.white;
    private Color amount_base_color = Color.white;
    private Vector2 icon_base_size;
    private Vector2 icon_base_anchored_position;
    private Vector2 amount_base_anchored_position;
    private float amount_base_font_size;

    internal Vector3 IconWorldPosition
    {
        get
        {
            return icon_image.transform.position;
        }
    }

    private void Awake()
    {
        icon_base_color = icon_image.color;
        amount_base_color = amount_value.color;
        icon_base_size = icon_image.rectTransform.sizeDelta;
        icon_base_anchored_position = icon_image.rectTransform.anchoredPosition;
        amount_base_anchored_position = amount_value.rectTransform.anchoredPosition;
        amount_base_font_size = amount_value.fontSize;
    }

    private void OnDisable()
    {
        GlowReset();
        DimReset();
    }

    internal void SetData(WheelResultPicker.ComputedSlot slot_data, float angle_deg)
    {
        DimReset();
        GlowReset();

        Vector3 rot = pivot.localEulerAngles;
        rot.z = angle_deg;
        pivot.localEulerAngles = rot;

        ZoneRewardEntry entry = slot_data.entry;
        icon_image.enabled = true;
        icon_image.sprite = entry.reward.ResolveWheelIcon();
        ApplyIconLayoutOverride(entry.reward);

        int display_amt = slot_data.final_amount;
        SetAmountText(entry.reward.ResolveAmountText(display_amt));
    }

    private void ApplyIconLayoutOverride(RewardDefinition reward)
    {
        RectTransform icon_rt = icon_image.rectTransform;
        icon_rt.sizeDelta = icon_base_size;
        icon_rt.anchoredPosition = icon_base_anchored_position;
        amount_value.rectTransform.anchoredPosition = amount_base_anchored_position;
        amount_value.fontSize = amount_base_font_size;

        if (icon_layout_overrides == null)
        {
            return;
        }

        for (int i = 0; i < icon_layout_overrides.Length; i++)
        {
            IconLayoutOverride layout = icon_layout_overrides[i];
            if (layout.Matches(reward))
            {
                if (layout.size.x > 0f && layout.size.y > 0f)
                {
                    icon_rt.sizeDelta = layout.size;
                }
                icon_rt.anchoredPosition = layout.anchored_position;
                amount_value.rectTransform.anchoredPosition = layout.amount_anchored_position;
                if (layout.amount_font_size > 0f)
                {
                    amount_value.fontSize = layout.amount_font_size;
                }
                return;
            }
        }
    }

    internal void SetDimmed(bool dimmed)
    {
        float to = dimmed ? 1f : 0f;
        if (!Mathf.Approximately(current_dim, to))
        {
            TweenLifetime.StopIfAlive(dim_tween);
            float dur = dimmed ? dim_in_duration : dim_out_duration;
            dim_tween = Tween.Custom(current_dim, to, dur, DimSetValue, Ease.OutCubic);
        }
    }

    internal void Highlight()
    {
        TweenLifetime.StopIfAlive(glow_sequence);
        SetGlowAlpha(1f);
        float fade_dur = glow_speed > 0f ? 1f / glow_speed : 0f;
        glow_sequence = Sequence.Create()
            .ChainDelay(glow_hold_seconds)
            .Chain(Tween.Custom(1f, 0f, fade_dur, SetGlowAlpha, Ease.Linear));
    }

    private void GlowReset()
    {
        TweenLifetime.StopIfAlive(glow_sequence);
        SetGlowAlpha(0f);
    }

    private void SetGlowAlpha(float glow)
    {
        SetOverlayAlpha(win_glow_overlay, glow * glow_max_alpha);
    }

    private void DimSetValue(float value)
    {
        current_dim = value;
        DimApply();
    }

    private void DimReset()
    {
        TweenLifetime.StopIfAlive(dim_tween);
        current_dim = 0f;
        DimApply();
    }

    private void DimApply()
    {
        SetOverlayAlpha(dim_overlay, current_dim * dim_max_alpha);
        icon_image.canvasRenderer.SetColor(Color.Lerp(icon_base_color, dim_tint, current_dim));
        amount_value.color = Color.Lerp(amount_base_color, dim_tint * amount_base_color, current_dim);
    }

    private static void SetOverlayAlpha(Graphic graphic, float alpha)
    {
        SetGraphicVisible(graphic, alpha > VisibleAlphaEpsilon);
        graphic.canvasRenderer.SetAlpha(alpha);
    }

    private void SetAmountText(string text)
    {
        bool visible = !string.IsNullOrEmpty(text);
        SetGraphicVisible(amount_value, visible);
        amount_value.text = visible ? text : string.Empty;
    }

    private static void SetGraphicVisible(Graphic graphic, bool visible)
    {
        if (graphic.gameObject.activeSelf != visible)
        {
            graphic.gameObject.SetActive(visible);
        }
    }

}
}
