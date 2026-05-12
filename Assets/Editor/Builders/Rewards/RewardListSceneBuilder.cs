#if UNITY_EDITOR
using UnityEngine;

internal static class RewardListSceneBuilder
{
    public static void Build(GameObject staticCanvasGO, GameObject rewardItemPrefab, WheelController controller)
    {
        var rlGO = new GameObject("ui_group_reward_list");
        rlGO.transform.SetParent(staticCanvasGO.transform, false);
        var rlRT = rlGO.AddComponent<RectTransform>();
        rlRT.anchorMin = new Vector2(0f, 0.5f);
        rlRT.anchorMax = new Vector2(0f, 0.5f);
        rlRT.pivot = new Vector2(0f, 0.5f);
        var rewardListUI = rlGO.AddComponent<RewardListUI>();

        var rlContGO = new GameObject("ui_container_reward_list");
        rlContGO.transform.SetParent(rlGO.transform, false);
        var rlContRT = rlContGO.AddComponent<RectTransform>();

        var itemPrefabUI = rewardItemPrefab != null ? rewardItemPrefab.GetComponent<RewardListItemUI>() : null;

        UILayoutBuilder.Wire(rewardListUI, "controller", controller);
        UILayoutBuilder.Wire(rewardListUI, "container", rlContRT);
        UILayoutBuilder.Wire(rewardListUI, "itemPrefab", itemPrefabUI);

    }
}
#endif
