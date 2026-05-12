using UnityEngine;
using UnityEditor;

public static class WheelLayoutPass
{
    public static void Apply()
    {

        UILayoutHelpers.SetRect("ui_card_wheel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(WheelPointerStyle.WheelDiameter, WheelPointerStyle.WheelDiameter),
            new Vector2(40f, -60f));

        UILayoutHelpers.SetRect("wheel_root",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(0f, 0f));

        ReparentWheelBackgroundIntoRotator();

        UILayoutHelpers.Delete("ui_image_wheel_card_frame");
        UILayoutHelpers.Delete("ui_image_wheel_card_bg");

        UILayoutHelpers.SetRect("ui_button_spin_center",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(180f, 180f), new Vector2(0f, 0f));

        UILayoutHelpers.SetGlow("ui_image_center_glow",
            new Vector3(0.35f, 0.35f, 1f),
            new Color32(180, 120, 45, 50));

        UILayoutHelpers.Delete("ui_image_wheel_ambient_glow");
        UILayoutHelpers.Delete("ui_image_wheel_rim_highlight");
        UILayoutHelpers.Delete("ui_image_pointer_shine");

        UILayoutHelpers.SetRect("wheel_pointer",
            WheelPointerStyle.AnchorMin,
            WheelPointerStyle.AnchorMax,
            WheelPointerStyle.Pivot,
            WheelPointerStyle.Size,
            WheelPointerStyle.Position);

        RestyleSliceIconPrefab();
    }

    private static void ReparentWheelBackgroundIntoRotator()
    {
        var bgList = UILayoutHelpers.FindAllInScene("wheel_background");
        var rotList = UILayoutHelpers.FindAllInScene("rotating_reward_layer");
        if (bgList.Count == 0 || rotList.Count == 0)
        {
            Debug.LogWarning("[ReparentWheelBg] wheel_background or rotating_reward_layer missing — run Tools → Build Wheel Scene first.");
            return;
        }
        GameObject bg = bgList[0];
        GameObject rot = rotList[0];
        bool alreadyChild = bg.transform.parent == rot.transform;
        if (alreadyChild && bg.transform.GetSiblingIndex() == 0)
        {
            Debug.Log("[ReparentWheelBg] wheel_background already inside rotating_reward_layer at index 0 — no-op.");
            return;
        }
        Undo.SetTransformParent(bg.transform, rot.transform, UILayoutBuilder.UndoLabel);
        bg.transform.SetSiblingIndex(0);
        RectTransform rt = bg.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rt);
        }
        Debug.Log(string.Format("[ReparentWheelBg] wheel_background moved into rotating_reward_layer (was alreadyChild={0}). Whole wheel now rotates as one piece.", alreadyChild));
    }

    private static void RestyleSliceIconPrefab()
    {
        const string path = "Assets/Prefabs/SliceView.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning("[RestyleSliceIconPrefab] prefab not found at " + path); return; }

        try
        {
            Transform uprightT = UILayoutHelpers.FindDeep(root.transform, "ui_group_slice_upright");
            if (uprightT == null)
            {
                Debug.LogWarning("[RestyleSliceIconPrefab] ui_group_slice_upright not found in SliceView prefab");
                return;
            }

            RectTransform upRT = uprightT.GetComponent<RectTransform>();
            if (upRT != null)
            {
                upRT.anchorMin = new Vector2(0.5f, 0.5f);
                upRT.anchorMax = new Vector2(0.5f, 0.5f);
                upRT.pivot = new Vector2(0.5f, 0.5f);
                upRT.sizeDelta = new Vector2(WheelGeometry.SlotSize, WheelGeometry.SlotSize);
                upRT.anchoredPosition = new Vector2(0f, WheelGeometry.SlotRadialOffset);
                EditorUtility.SetDirty(upRT);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[RestyleSliceIconPrefab] DONE — upright slot={WheelGeometry.SlotSize:F0}x{WheelGeometry.SlotSize:F0} @(0,{WheelGeometry.SlotRadialOffset:F0}); runtime overrides via WheelGeometry");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

}
