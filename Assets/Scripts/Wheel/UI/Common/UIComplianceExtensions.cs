using UnityEngine.UI;

public static class UIComplianceExtensions
{
    public static void MarkDecorative(this MaskableGraphic g)
    {
        if (g == null) return;
        if (g.raycastTarget) g.raycastTarget = false;
        if (g.maskable)      g.maskable      = false;
    }
}
