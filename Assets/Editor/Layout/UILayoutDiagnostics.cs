using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class UILayoutDiagnostics
{
    public static void DiagnoseLayout()
    {
        Debug.Log("=== [UILayoutBuilder] DIAGNOSTIC (read-only) ===");
        string[] names = {
            "WheelCanvas",
            "ui_canvas_dynamic", "ui_canvas_static", "ui_canvas_overlay",
            "wheel_root", "ui_button_spin_center", "ui_image_center_glow",
            "wheel_pointer", "ui_group_reward_list",
            "ui_group_zone_progress_bar_indicator",
            "ui_image_reward_list_bg", "ui_image_reward_list_outline",
            "ui_panel_reward_list_exit_header", "ui_label_reward_list_exit_text",
            "ui_container_reward_list"
        };
        for (int i = 0; i < names.Length; i++)
        {
            ReportAll(names[i]);
        }
    }

    public static void DiagnoseWheelAndZoneAndSlice()
    {
        Debug.Log("=== [UILayoutBuilder] WHEEL + ZONE + SLICE DIAGNOSTIC ===");

        DiagRect("wheel_root", checkARF: true);
        DiagRect("wheel_pointer");
        DiagRect("ui_group_zone_progress_bar_indicator");
        DiagRect("ui_group_reward_list");
        DiagRect("ui_panel_reward_list_exit_header");
        DiagRect("ui_container_reward_list");

        DiagSlicePrefab();
        DiagSliceSceneInstances();
    }

    private static void ReportAll(string name)
    {
        var list = UILayoutHelpers.FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[Diag] NOT FOUND: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            RectTransform rt = go.GetComponent<RectTransform>();
            bool hasARF = go.GetComponent<AspectRatioFitter>() != null;
            Image img = go.GetComponent<Image>();
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            Vector3 scale = go.transform.localScale;

            string rectStr = rt != null
                ? string.Format("aMin={0} aMax={1} pivot={2} size={3} pos={4}",
                    rt.anchorMin, rt.anchorMax, rt.pivot, rt.sizeDelta, rt.anchoredPosition)
                : "<no RectTransform>";
            string imgStr = img != null
                ? string.Format(" img.color=({0:0.00},{1:0.00},{2:0.00}) alpha={3}",
                    img.color.r, img.color.g, img.color.b, Mathf.RoundToInt(img.color.a * 255f))
                : "";
            string tmpStr = tmp != null
                ? string.Format(" tmp.fontSize={0} tmp.color=({1:0.00},{2:0.00},{3:0.00}) tmp.text=\"{4}\"",
                    tmp.fontSize, tmp.color.r, tmp.color.g, tmp.color.b, tmp.text)
                : "";

            Debug.Log(string.Format("[Diag] {0} [{1}/{2}] active={3} ARF={4} scale=({5:0.00},{6:0.00},{7:0.00}) {8}{9}{10}\n  path={11}",
                name, i + 1, list.Count, go.activeInHierarchy, hasARF,
                scale.x, scale.y, scale.z, rectStr, imgStr, tmpStr, UILayoutHelpers.PathOf(go)));
        }
    }

    private static void DiagRect(string name, bool checkARF = false)
    {
        GameObject go = UILayoutHelpers.FindFirstInScene(name);
        if (go == null) { Debug.LogWarning("[DiagRect] " + name + " NOT FOUND"); return; }
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning("[DiagRect] " + name + " has no RectTransform"); return; }

        string arfStr = checkARF
            ? " AspectRatioFitter=" + (go.GetComponent<AspectRatioFitter>() != null)
            : "";
        Debug.Log(string.Format(
            "[DiagRect] {0} active={1} aMin={2} aMax={3} pivot={4} size={5} pos={6}{7}\n  path={8}",
            name, go.activeInHierarchy, rt.anchorMin, rt.anchorMax, rt.pivot,
            rt.sizeDelta, rt.anchoredPosition, arfStr, UILayoutHelpers.PathOf(go)));
    }

    private static void DiagSlicePrefab()
    {
        const string slicePath = "Assets/Prefabs/SliceView.prefab";
        GameObject sliceRoot = AssetDatabase.LoadAssetAtPath<GameObject>(slicePath);
        if (sliceRoot == null)
        {
            Debug.LogWarning("[DiagSlice] SliceView prefab not found at " + slicePath);
            return;
        }

        Transform uprightT = UILayoutHelpers.FindDeep(sliceRoot.transform, "ui_group_slice_upright");
        if (uprightT != null)
        {
            RectTransform rt = uprightT.GetComponent<RectTransform>();
            Debug.Log(string.Format("[DiagSlice prefab] ui_group_slice_upright size={0} pos={1}",
                rt != null ? rt.sizeDelta.ToString() : "<no rect>",
                rt != null ? rt.anchoredPosition.ToString() : "<no rect>"));
        }

        Transform iconT = UILayoutHelpers.FindDeep(sliceRoot.transform, "ui_image_slice_icon");
        if (iconT == null) Debug.LogWarning("[DiagSlice prefab] ui_image_slice_icon NOT FOUND");
        else
        {
            RectTransform rt = iconT.GetComponent<RectTransform>();
            Image img = iconT.GetComponent<Image>();
            Debug.Log(string.Format("[DiagSlice prefab] ui_image_slice_icon size={0} pos={1} preserveAspect={2}",
                rt != null ? rt.sizeDelta.ToString() : "<no rect>",
                rt != null ? rt.anchoredPosition.ToString() : "<no rect>",
                img != null ? img.preserveAspect.ToString() : "<no image>"));
        }

        Transform amtT = UILayoutHelpers.FindDeep(sliceRoot.transform, "ui_label_slice_amount_value");
        if (amtT == null) Debug.LogWarning("[DiagSlice prefab] ui_label_slice_amount_value NOT FOUND");
        else
        {
            RectTransform rt = amtT.GetComponent<RectTransform>();
            TMP_Text tmp = amtT.GetComponent<TMP_Text>();
            Debug.Log(string.Format(
                "[DiagSlice prefab] ui_label_slice_amount_value size={0} pos={1} fontSize={2} color=({3:0.00},{4:0.00},{5:0.00})",
                rt != null ? rt.sizeDelta.ToString() : "<no rect>",
                rt != null ? rt.anchoredPosition.ToString() : "<no rect>",
                tmp != null ? tmp.fontSize : 0f,
                tmp != null ? tmp.color.r : 0f,
                tmp != null ? tmp.color.g : 0f,
                tmp != null ? tmp.color.b : 0f));
        }
    }

    private static void DiagSliceSceneInstances()
    {
        RewardDefinition[] rewardAssets = Resources.FindObjectsOfTypeAll<RewardDefinition>();
        SliceView[] all = Resources.FindObjectsOfTypeAll<SliceView>();
        int reported = 0;
        for (int i = 0; i < all.Length && reported < 8; i++)
        {
            SliceView sv = all[i];
            if (sv == null) continue;
            if (!sv.gameObject.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(sv)) continue;

            Image iconImg = sv.IconImage;
            TMP_Text amtTMP = sv.AmountText;
            RectTransform iconRT = iconImg != null ? iconImg.rectTransform : null;
            RectTransform amtRT = amtTMP != null ? amtTMP.rectTransform : null;

            string rewardLabel = "<unknown>";
            string categoryLabel = "<unknown>";
            if (iconImg != null && iconImg.sprite != null)
            {
                RewardDefinition matched = MatchRewardBySprite(rewardAssets, iconImg.sprite);
                if (matched != null)
                {
                    rewardLabel = matched.rewardId + " / " + matched.displayName;
                    categoryLabel = WheelSliceContentLayout.ResolveCategory(matched).ToString();
                }
                else
                {
                    rewardLabel = "(sprite=" + iconImg.sprite.name + ")";
                }
            }

            Debug.Log(string.Format(
                "[DiagSlice scene #{0}] reward={1} category={2} | icon size={3} pos={4} preserveAspect={5} | amount size={6} pos={7} fs={8}\n  path={9}",
                reported,
                rewardLabel, categoryLabel,
                iconRT != null ? iconRT.sizeDelta.ToString() : "<missing>",
                iconRT != null ? iconRT.anchoredPosition.ToString() : "<missing>",
                iconImg != null ? iconImg.preserveAspect.ToString() : "<missing>",
                amtRT != null ? amtRT.sizeDelta.ToString() : "<missing>",
                amtRT != null ? amtRT.anchoredPosition.ToString() : "<missing>",
                amtTMP != null ? amtTMP.fontSize.ToString() : "<missing>",
                UILayoutHelpers.PathOf(sv.gameObject)));
            reported++;
        }
        if (reported == 0)
        {
            Debug.Log("[DiagSlice scene] no SliceView instances in scene yet (slices spawn at runtime/Play). WheelSliceContentLayout will apply per-category sizes on first build.");
        }
    }

    private static RewardDefinition MatchRewardBySprite(RewardDefinition[] rewards, Sprite sprite)
    {
        if (rewards == null || sprite == null) return null;
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] != null && rewards[i].icon == sprite) return rewards[i];
        }
        return null;
    }
}
