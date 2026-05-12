#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

internal static class PrefabsSceneBuilder
{
    const string PREFABS = "Assets/Prefabs";

    static void ScrubLooseTempRoot(string name)
    {
        UnityEngine.SceneManagement.Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!active.IsValid()) return;
        GameObject[] roots = active.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == name)
                Object.DestroyImmediate(roots[i]);
        }
    }

    public static GameObject MakeSliceViewPrefab()
    {
        string path = $"{PREFABS}/SliceView.prefab";
        ScrubLooseTempRoot("ui_slice_view");

        var animCfg = LoadAnimConfig();
        const float DesignSlotSize = 140f;

        var root = new GameObject("ui_slice_view");
        root.AddComponent<RectTransform>();
        var sv = root.AddComponent<SliceView>();

        var pivotGO = WheelSceneSetup.Child(root, "ui_pivot_slice");
        var pivotRT = pivotGO.AddComponent<RectTransform>();
        pivotRT.sizeDelta = Vector2.zero;
        pivotRT.anchoredPosition = Vector2.zero;

        var glowGO = WheelSceneSetup.Child(pivotGO, "ui_image_slice_winglow");
        glowGO.AddComponent<CanvasRenderer>();
        var glowRT = glowGO.AddComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.5f, 0.5f);
        glowRT.anchorMax = new Vector2(0.5f, 0.5f);
        glowRT.pivot = new Vector2(0.5f, 0.5f);
        glowRT.anchoredPosition = Vector2.zero;
        float glowSize = DesignSlotSize * (animCfg != null ? animCfg.glowSizeScale : 1.15f);
        glowRT.sizeDelta = new Vector2(glowSize, glowSize);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.sprite = WheelSceneSetup.Spr("star_glow_alpha");
        Color glowRGB = animCfg != null ? animCfg.glowTint : new Color32(255, 200, 80, 255);
        glowImg.color = new Color(glowRGB.r, glowRGB.g, glowRGB.b, 1f);
        glowImg.raycastTarget = false;
        glowImg.preserveAspect = true;

        var scaleGO = WheelSceneSetup.Child(pivotGO, "ui_group_slice_scale_root");
        var scaleRT = scaleGO.AddComponent<RectTransform>();
        scaleRT.sizeDelta = Vector2.zero;
        scaleRT.anchoredPosition = Vector2.zero;

        var upGO = WheelSceneSetup.Child(scaleGO, "ui_group_slice_upright");
        var upRT = upGO.AddComponent<RectTransform>();
        upRT.sizeDelta = new Vector2(140, 140);
        upRT.anchoredPosition = new Vector2(0, 264);

        var iconGO = WheelSceneSetup.Child(upGO, "ui_image_slice_icon");
        iconGO.AddComponent<CanvasRenderer>();
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(108, 108);
        iconRT.anchoredPosition = new Vector2(0, 6);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        WheelSceneSetup.DisableRaycast(iconImg);

        var amtGO = WheelSceneSetup.Child(upGO, "ui_label_slice_amount_value");
        var amtRT = amtGO.AddComponent<RectTransform>();
        amtRT.sizeDelta = new Vector2(120, 26);
        amtRT.anchoredPosition = new Vector2(0, -54);
        var amtTxt = amtGO.AddComponent<TextMeshProUGUI>();
        amtTxt.text = "x100";
        amtTxt.fontSize = 22;
        amtTxt.fontStyle = FontStyles.Bold;
        amtTxt.alignment = TextAlignmentOptions.Center;
        amtTxt.color = new Color(1f, 0.85f, 0.3f);
        amtTxt.raycastTarget = false;
        amtTxt.enableAutoSizing = true;
        amtTxt.fontSizeMin = 14;
        amtTxt.fontSizeMax = 26;

        var dimGO = WheelSceneSetup.Child(pivotGO, "ui_image_slice_dim");
        dimGO.AddComponent<CanvasRenderer>();
        var dimRT = dimGO.AddComponent<RectTransform>();
        dimRT.anchorMin = new Vector2(0.5f, 0.5f);
        dimRT.anchorMax = new Vector2(0.5f, 0.5f);
        dimRT.pivot = new Vector2(0.5f, 0.5f);
        dimRT.anchoredPosition = Vector2.zero;
        float dimSize = DesignSlotSize * (animCfg != null ? animCfg.dimSizeScale : 1.0f);
        dimRT.sizeDelta = new Vector2(dimSize, dimSize);
        var dimImg = dimGO.AddComponent<Image>();
        dimImg.sprite = WheelSceneSetup.Spr("star_glow_alpha");
        dimImg.color = new Color(0f, 0f, 0f, 1f);
        dimImg.raycastTarget = false;
        dimImg.preserveAspect = true;

        UILayoutBuilder.Wire(sv, "animConfig", animCfg);
        UILayoutBuilder.Wire(sv, "pivot", pivotRT);
        UILayoutBuilder.Wire(sv, "uprightGroup", upRT);
        UILayoutBuilder.Wire(sv, "iconImage", iconImg);
        UILayoutBuilder.Wire(sv, "amountText", amtTxt);
        UILayoutBuilder.Wire(sv, "winGlowOverlay", glowImg);
        UILayoutBuilder.Wire(sv, "dimOverlay", dimImg);

        try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { if (root != null) Object.DestroyImmediate(root); }
    }

    static WheelAnimationConfig LoadAnimConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:WheelAnimationConfig");
        if (guids == null || guids.Length == 0) return null;
        string p = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<WheelAnimationConfig>(p);
    }

    public static GameObject MakeRewardItemPrefab()
    {
        string path = $"{PREFABS}/RewardListItem.prefab";
        ScrubLooseTempRoot("ui_item_reward_list");

        var animCfg = LoadAnimConfig();

        var root = new GameObject("ui_item_reward_list");
        root.AddComponent<RectTransform>();
        root.AddComponent<LayoutElement>();

        var rl = root.AddComponent<RewardListItemUI>();
        root.AddComponent<HorizontalLayoutGroup>();

        var frameGO = WheelSceneSetup.Child(root, "ui_image_reward_icon_frame");
        frameGO.AddComponent<CanvasRenderer>();
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = WheelSceneSetup.Spr("ui_card_frame_4px_zone");
        frameImg.type = Image.Type.Sliced;
        WheelSceneSetup.DisableRaycast(frameImg);
        frameGO.AddComponent<LayoutElement>();

        var iconGO = WheelSceneSetup.Child(frameGO, "ui_image_reward_icon");
        iconGO.AddComponent<CanvasRenderer>();
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        WheelSceneSetup.DisableRaycast(iconImg);

        var amtGO = WheelSceneSetup.Child(root, "ui_label_reward_amount_value");
        var amtTxt = amtGO.AddComponent<TextMeshProUGUI>();
        amtTxt.text = "0";
        amtTxt.fontStyle = FontStyles.Bold;
        amtTxt.raycastTarget = false;
        amtGO.AddComponent<LayoutElement>();

        UILayoutBuilder.Wire(rl, "animConfig", animCfg);
        UILayoutBuilder.Wire(rl, "iconImage", iconImg);
        UILayoutBuilder.Wire(rl, "iconFrame", frameImg);
        UILayoutBuilder.Wire(rl, "rewardAmount_value", amtTxt);

        try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { if (root != null) Object.DestroyImmediate(root); }
    }
}
#endif
