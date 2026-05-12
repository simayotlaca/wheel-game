using UnityEngine;

public static class DeathOverlayStyle
{
    public static readonly Color MainBackdropDim  = new Color(0f, 0f, 0f, 0.75f);
    public static readonly Color MainPanelFill    = new Color(0.07f, 0.07f, 0.10f, 0.96f);
    public static readonly Color MainPanelOutline = new Color(0.55f, 0.55f, 0.65f, 0.45f);

    public static readonly Color GiveUpBackdropDim = new Color(0f, 0f, 0f, 0.82f);

    public const float RewardListPromotedAlpha = 1f;
    public const float RewardListOverlayAlpha  = 1f;

    public const float ZoneBarPromotedAlpha = 1f;
    public const float ZoneBarOverlayAlpha  = 1f;

    public const float MetaProgressPromotedAlpha = 1f;
    public const float MetaProgressOverlayAlpha  = 1f;
}
