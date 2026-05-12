using UnityEngine;

public class HudOverlayDemoter : MonoBehaviour
{
    [SerializeField] private Canvas rewardListCanvas;
    [SerializeField] private Canvas zoneBarCanvas;
    [SerializeField] private Canvas metaProgressCanvas;
    [SerializeField] private CanvasGroup metaProgressGroup;

    public void SetDemoted(bool demoted)
    {
        int order = demoted
            ? UICanvasOrders.RewardListBelowOverlay
            : UICanvasOrders.HUDPromoted;
        if (rewardListCanvas   != null) rewardListCanvas.sortingOrder   = order;
        if (zoneBarCanvas      != null) zoneBarCanvas.sortingOrder      = order;
        if (metaProgressCanvas != null) metaProgressCanvas.sortingOrder = order;
        if (!demoted && metaProgressGroup != null) metaProgressGroup.alpha = DeathOverlayStyle.MetaProgressPromotedAlpha;
    }

    public void SetForDeath(bool demoted)
    {
        if (rewardListCanvas != null) rewardListCanvas.sortingOrder = UICanvasOrders.HUDPromoted;
        int order = demoted
            ? UICanvasOrders.RewardListBelowOverlay
            : UICanvasOrders.HUDPromoted;
        if (zoneBarCanvas != null) zoneBarCanvas.sortingOrder = order;
        if (metaProgressCanvas != null) metaProgressCanvas.sortingOrder = order;
        if (!demoted && metaProgressGroup != null) metaProgressGroup.alpha = DeathOverlayStyle.MetaProgressPromotedAlpha;
    }

    public void SetMetaProgressDemoted(bool demoted)
    {
        if (metaProgressCanvas != null)
            metaProgressCanvas.sortingOrder = demoted
                ? UICanvasOrders.RewardListBelowOverlay
                : UICanvasOrders.HUDPromoted;
        if (metaProgressGroup != null)
            metaProgressGroup.alpha = demoted
                ? DeathOverlayStyle.MetaProgressOverlayAlpha
                : DeathOverlayStyle.MetaProgressPromotedAlpha;
    }
}
