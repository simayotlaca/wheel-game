using System.Globalization;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class SliceView : MonoBehaviour
{
    [SerializeField] private RectTransform pivot;
    [SerializeField] private Image icon_image;
    [SerializeField] private TMP_Text amount_value;
    [SerializeField] private Image win_glow_overlay;
    [SerializeField] private Image dim_overlay;

    private ConfigAnimation anim_config;
    private Sequence glow_sequence;
    private Tween dim_tween;
    private float current_dim;
    private Color amount_base_color = Color.white;
    private bool base_colors_cached;

    public Vector3 IconWorldPosition => icon_image.transform.position;

    public void Initialize(ConfigAnimation anim_cfg)
    {
        anim_config = anim_cfg;

        GlowReset();
        DimReset();
    }

    public void SetData(WheelResultPicker.ComputedSlot slot_data, float angle_deg)
    {
        DimReset();
        GlowReset();

        Vector3 rot = pivot.localEulerAngles;
        rot.z = angle_deg;
        pivot.localEulerAngles = rot;

        ZoneRewardEntry entry = slot_data.entry;
        if (entry == null || entry.reward == null)
        {
            icon_image.enabled = false;
            SetAmountText(string.Empty);
            return;
        }

        icon_image.enabled = true;
        Sprite reward_sprite = entry.reward.wheelIcon;
        if (reward_sprite == null)
        {
            reward_sprite = entry.reward.icon;
        }
        icon_image.sprite = reward_sprite;

        WheelSliceVisualLayout layout = anim_config.ResolveWheelSliceLayout(entry.reward);
        icon_image.rectTransform.sizeDelta = layout.iconSize;
        icon_image.rectTransform.anchoredPosition = layout.iconAnchoredPos;
        amount_value.rectTransform.anchoredPosition = layout.amountAnchoredPos;
        amount_value.fontSize = layout.amountFontSize;

        int display_amt = slot_data.final_amount;
        SetAmountText(FormatRewardAmount(entry.reward, display_amt));

        DimCacheBaseColorsIfNeeded();
        DimApply();
    }

    public void SetDimmed(bool dimmed)
    {
        float to = dimmed ? 1f : 0f;
        if (!Mathf.Approximately(current_dim, to))
        {
            TweenLifetime.StopIfAlive(dim_tween);
            float dur = dimmed ? anim_config.dimInDuration : anim_config.dimOutDuration;
            dim_tween = Tween.Custom(current_dim, to, dur, DimSetValue, Ease.OutCubic);
        }
    }

    public void Highlight()
    {
        TweenLifetime.StopIfAlive(glow_sequence);
        SetGlowAlpha(1f);
        float fade_dur = anim_config.glowSpeed > 0f ? 1f / anim_config.glowSpeed : 0f;
        glow_sequence = Sequence.Create()
            .ChainDelay(anim_config.glowHoldSeconds)
            .Chain(Tween.Custom(1f, 0f, fade_dur, SetGlowAlpha, Ease.Linear));
    }

    private void GlowReset()
    {
        TweenLifetime.StopIfAlive(glow_sequence);
        SetGlowAlpha(0f);
    }

    private void SetGlowAlpha(float glow)
    {
        float alpha = glow * anim_config.glowMaxAlpha;
        SetGraphicVisible(win_glow_overlay, alpha > 0.001f);
        win_glow_overlay.canvasRenderer.SetAlpha(alpha);
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
        SetDimOverlayAlpha(0f);
    }

    private void DimApply()
    {
        SetDimOverlayAlpha(current_dim * anim_config.dimMaxAlpha);
        icon_image.canvasRenderer.SetColor(Color.Lerp(Color.white, anim_config.dimTint, current_dim));
        amount_value.color = Color.Lerp(amount_base_color, anim_config.dimTint * amount_base_color, current_dim);
    }

    private void SetDimOverlayAlpha(float alpha)
    {
        SetGraphicVisible(dim_overlay, alpha > 0.001f);
        dim_overlay.canvasRenderer.SetAlpha(alpha);
    }

    private void DimCacheBaseColorsIfNeeded()
    {
        if (!base_colors_cached)
        {
            amount_base_color = amount_value.color;
            base_colors_cached = true;
        }
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

    private static string FormatRewardAmount(RewardDefinition reward, int amount)
    {
        if (reward != null && !reward.isDeath)
        {
            if (reward.displayAsMultiplier)
            {
                return "x" + NumberFormatter.FormatCompact(amount);
            }

            switch (reward.visualCategory)
            {
                case RewardVisualCategory.Coin:
                case RewardVisualCategory.Cash:
                    return NumberFormatter.FormatCompact(amount);

                case RewardVisualCategory.Weapon:
                    return "+" + NumberFormatter.FormatCompact(amount);

                case RewardVisualCategory.Compact:
                case RewardVisualCategory.Chest:
                case RewardVisualCategory.Consumable:
                case RewardVisualCategory.Cosmetic:
                    if (amount <= 1)
                    {
                        return string.Empty;
                    }
                    return "x" + amount.ToString(CultureInfo.InvariantCulture);

                default:
                    return amount.ToString(CultureInfo.InvariantCulture);
            }
        }
        return string.Empty;
    }
}
}
