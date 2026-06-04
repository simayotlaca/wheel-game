using UnityEngine;

namespace VertigoWheel
{
internal struct ZoneChipStyle
{
    internal Sprite sprite;
    internal Color body_tint;
    internal Color label_color;

    internal ZoneChipStyle(Sprite sprite, Color body_tint, Color label_color)
    {
        this.sprite = sprite;
        this.body_tint = body_tint;
        this.label_color = label_color;
    }
}

[CreateAssetMenu(fileName = "ZoneStyleConfig", menuName = "Vertigo Wheel/Config/Zone Style Config")]
public class ZoneStyleConfig : ScriptableObject
{
    [Header("Sprites")]
    public Sprite spriteNeutral;
    public Sprite spriteSafe;
    public Sprite spriteSuper;

    [Header("Body Tints")]
    public Color tintNormal;
    public Color tintSafe;
    public Color tintSuper;

    [Header("Label Colors")]
    public Color labelNormal;
    public Color labelSafe;
    public Color labelSuper;

    [Header("Item Colors")]
    public Color colorPast;
    public Color colorFuture;

    [Header("Slot Past Fade")]
    [Range(0f, 1f)] public float pastAlphaBase;
    [Range(0f, 1f)] public float pastAlphaStep;
    [Range(0f, 1f)] public float pastAlphaMin;

    internal ZoneChipStyle ResolveChipStyle(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Safe:
                return new ZoneChipStyle(spriteSafe, Color.white, labelSafe);

            case RewardTier.Super:
                return new ZoneChipStyle(spriteSuper, Color.white, labelSuper);

            default:
                return new ZoneChipStyle(spriteNeutral, tintNormal, labelNormal);
        }
    }

    internal Color ResolveSlotColor(RewardTier tier, bool is_past, int past_distance)
    {
        if (is_past)
        {
            Color color = ResolveTierColor(tier, colorPast);
            float alpha = pastAlphaBase - (past_distance - 1) * pastAlphaStep;
            alpha = Mathf.Clamp(alpha, pastAlphaMin, pastAlphaBase);
            color.a = colorPast.a * alpha;
            return color;
        }

        return ResolveTierColor(tier, colorFuture);
    }

    private Color ResolveTierColor(RewardTier tier, Color fallback)
    {
        switch (tier)
        {
            case RewardTier.Super:
                return tintSuper;

            case RewardTier.Safe:
                return tintSafe;

            default:
                return fallback;
        }
    }
}
}
