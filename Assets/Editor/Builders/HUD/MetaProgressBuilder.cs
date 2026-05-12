#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal static class MetaProgressBuilder
{
    const string PanelName        = "ui_group_meta_progress";
    const string RowsContainer    = "ui_container_meta_progress_rows";
    const string RowPrefabPath    = "Assets/Prefabs/MetaProgressRow.prefab";
    const string DefinitionsDir   = "Assets/ScriptableObjects/MetaProgress";

    const string RowName          = "ui_item_meta_progress_row";
    const string BorderName       = "ui_image_meta_progress_border";
    const string BackplateName    = "ui_image_meta_progress_backplate";
    const string RarityEdgeName   = "ui_image_meta_progress_rarity_edge";
    const string IconGlowName     = "ui_image_meta_progress_icon_glow";
    const string IconFrameName    = "ui_image_meta_progress_icon_frame";
    const string WeaponIconName   = "ui_image_meta_progress_weapon_icon";
    const string InfoGroupName    = "ui_group_meta_progress_info";
    const string NameLabelName    = "ui_label_meta_progress_weapon_name_value";
    const string BarBgName        = "ui_image_meta_progress_bar_bg";
    const string BarFillName      = "ui_image_meta_progress_bar_fill";
    const string BarHighlightName = "ui_image_meta_progress_bar_highlight";
    const string AmountLabelName  = "ui_label_meta_progress_amount_value";
    const string FlashName        = "ui_image_meta_progress_flash";
    const string UnlockedName     = "ui_label_meta_progress_unlocked_value";

    public static void Build()
    {

        var existing = Object.FindObjectsOfType<MetaProgressPanel>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null) Undo.DestroyObjectImmediate(existing[i].gameObject);
        }

        WeaponProgressDefinition[] defs = EnsureDemoDefinitions();

        GameObject canvas = UILayoutBuilder.FindFirstInScene("ui_canvas_static");
        if (canvas == null)
        {
            Debug.LogWarning("[MetaProgressBuilder] ui_canvas_static not found — aborting");
            return;
        }

        GameObject rowPrefab = EnsureRowPrefab();
        if (rowPrefab == null)
        {
            Debug.LogWarning("[MetaProgressBuilder] row prefab build failed — aborting");
            return;
        }

        GameObject panelGO = UILayoutBuilder.EnsureChild(canvas.transform, PanelName);
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        Undo.RecordObject(panelRT, UILayoutBuilder.UndoLabel);
        panelRT.anchorMin = MetaProgressStyle.PanelAnchorMin;
        panelRT.anchorMax = MetaProgressStyle.PanelAnchorMax;
        panelRT.pivot = MetaProgressStyle.PanelPivot;
        panelRT.sizeDelta = MetaProgressStyle.PanelSize;
        panelRT.anchoredPosition = MetaProgressStyle.PanelAnchoredPos;
        panelRT.localScale = Vector3.one;
        panelRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(panelRT);

        StripLegacyChild(panelGO, "ui_image_meta_progress_bg");
        StripLegacyChild(panelGO, "ui_label_meta_progress_title_value");

        GameObject rowsGO = UILayoutBuilder.EnsureChild(panelGO.transform, RowsContainer);
        RectTransform rowsRT = rowsGO.GetComponent<RectTransform>();
        rowsRT.anchorMin = Vector2.zero;
        rowsRT.anchorMax = Vector2.one;
        rowsRT.pivot = new Vector2(0.5f, 1f);
        rowsRT.offsetMin = Vector2.zero;
        rowsRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = UILayoutBuilder.EnsureComponent<VerticalLayoutGroup>(rowsGO);
        vlg.padding = MetaProgressStyle.RowsContainerPadding;
        vlg.spacing = MetaProgressStyle.RowsContainerSpacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        EditorUtility.SetDirty(vlg);

        MetaProgressPanel panel = UILayoutBuilder.EnsureComponent<MetaProgressPanel>(panelGO);
        WheelController wheelCtrl = Object.FindObjectOfType<WheelController>(true);
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("controller").objectReferenceValue = wheelCtrl;
        so.FindProperty("rowsContainer").objectReferenceValue = rowsRT;
        so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab.GetComponent<MetaProgressRowUI>();
        so.FindProperty("panelRoot").objectReferenceValue = panelGO;
        SerializedProperty defsProp = so.FindProperty("definitions");
        defsProp.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
            defsProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];

        RewardDefinition[] allRewards = LoadAllRewardDefinitions();
        SerializedProperty rewardsProp = so.FindProperty("rewards");
        rewardsProp.arraySize = allRewards.Length;
        for (int i = 0; i < allRewards.Length; i++)
            rewardsProp.GetArrayElementAtIndex(i).objectReferenceValue = allRewards[i];

        so.FindProperty("maxVisibleRows").intValue = MetaProgressStyle.MaxVisibleRows;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[MetaProgressBuilder] DONE — panel={PanelName} controller={(wheelCtrl != null)} defs={defs.Length}");
    }

    static GameObject EnsureRowPrefab()
    {
        Sprite slicedCard = AssetDatabase.LoadAssetAtPath<Sprite>(MetaProgressStyle.BackplateSprite);
        Sprite solid      = AssetDatabase.LoadAssetAtPath<Sprite>(MetaProgressStyle.SolidSprite);

        GameObject root = new GameObject(RowName, typeof(RectTransform));
        try
        {
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(0f, MetaProgressStyle.RowHeight);
            LayoutElement le = root.AddComponent<LayoutElement>();
            le.preferredHeight = MetaProgressStyle.RowHeight;
            le.minHeight = MetaProgressStyle.RowHeight;

            Image borderImg = AddStretchedImage(root.transform, BorderName, slicedCard,
                MetaProgressStyle.BorderColorIdle, MetaProgressStyle.BorderInset);

            Image backplateImg = AddStretchedImage(root.transform, BackplateName, slicedCard,
                MetaProgressStyle.BackplateColor, 0f);
            UnityEngine.UI.Shadow backplateShadow = backplateImg.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            backplateShadow.effectColor = MetaProgressStyle.BackplateShadowColor;
            backplateShadow.effectDistance = MetaProgressStyle.BackplateShadowOffset;

            GameObject edgeGO = new GameObject(RarityEdgeName, typeof(RectTransform), typeof(CanvasRenderer));
            edgeGO.transform.SetParent(root.transform, false);
            RectTransform edgeRT = edgeGO.GetComponent<RectTransform>();

            edgeRT.anchorMin = new Vector2(0f, 0f);
            edgeRT.anchorMax = new Vector2(0f, 1f);
            edgeRT.pivot = new Vector2(0f, 0.5f);
            edgeRT.sizeDelta = new Vector2(MetaProgressStyle.RarityEdgeWidth, -MetaProgressStyle.RarityEdgePaddingV * 2f);
            edgeRT.anchoredPosition = Vector2.zero;
            Image edgeImg = edgeGO.AddComponent<Image>();
            edgeImg.sprite = solid;
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = Color.white;
            edgeImg.raycastTarget = false;

            GameObject glowGO = new GameObject(IconGlowName, typeof(RectTransform), typeof(CanvasRenderer));
            glowGO.transform.SetParent(root.transform, false);
            RectTransform glowRT = glowGO.GetComponent<RectTransform>();
            glowRT.anchorMin = MetaProgressStyle.IconAnchor;
            glowRT.anchorMax = MetaProgressStyle.IconAnchor;
            glowRT.pivot = new Vector2(0f, 0.5f);
            glowRT.sizeDelta = MetaProgressStyle.IconGlowSize;

            glowRT.anchoredPosition = MetaProgressStyle.IconFrameAnchoredPos
                                    + new Vector2(-(MetaProgressStyle.IconGlowSize.x - MetaProgressStyle.IconFrameSize.x) * 0.5f, 0f);
            Image glowImg = glowGO.AddComponent<Image>();
            glowImg.sprite = solid;
            glowImg.type = Image.Type.Sliced;
            glowImg.color = Color.white;
            glowImg.raycastTarget = false;

            GameObject frameGO = new GameObject(IconFrameName, typeof(RectTransform), typeof(CanvasRenderer));
            frameGO.transform.SetParent(root.transform, false);
            RectTransform frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = MetaProgressStyle.IconAnchor;
            frameRT.anchorMax = MetaProgressStyle.IconAnchor;
            frameRT.pivot = new Vector2(0f, 0.5f);
            frameRT.sizeDelta = MetaProgressStyle.IconFrameSize;
            frameRT.anchoredPosition = MetaProgressStyle.IconFrameAnchoredPos;
            Image frameImg = frameGO.AddComponent<Image>();
            frameImg.sprite = slicedCard;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = MetaProgressStyle.IconFrameColor;
            frameImg.raycastTarget = false;

            UnityEngine.UI.Shadow frameShadow = frameGO.AddComponent<UnityEngine.UI.Shadow>();
            frameShadow.effectColor = MetaProgressStyle.IconFrameShadowColor;
            frameShadow.effectDistance = MetaProgressStyle.IconFrameShadowOffset;

            GameObject iconGO = new GameObject(WeaponIconName, typeof(RectTransform), typeof(CanvasRenderer));
            iconGO.transform.SetParent(root.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = MetaProgressStyle.IconAnchor;
            iconRT.anchorMax = MetaProgressStyle.IconAnchor;
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = MetaProgressStyle.WeaponIconSize;

            iconRT.anchoredPosition = MetaProgressStyle.IconFrameAnchoredPos
                                    + new Vector2(MetaProgressStyle.IconFrameSize.x * 0.5f, 0f);
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            GameObject infoGO = new GameObject(InfoGroupName, typeof(RectTransform));
            infoGO.transform.SetParent(root.transform, false);
            RectTransform infoRT = infoGO.GetComponent<RectTransform>();
            infoRT.anchorMin = MetaProgressStyle.InfoGroupAnchorMin;
            infoRT.anchorMax = MetaProgressStyle.InfoGroupAnchorMax;
            infoRT.pivot = MetaProgressStyle.InfoGroupPivot;
            infoRT.offsetMin = MetaProgressStyle.InfoGroupOffsetMin;
            infoRT.offsetMax = MetaProgressStyle.InfoGroupOffsetMax;

            GameObject nameGO = new GameObject(NameLabelName, typeof(RectTransform));
            nameGO.transform.SetParent(infoGO.transform, false);
            RectTransform nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = MetaProgressStyle.NameAnchorMin;
            nameRT.anchorMax = MetaProgressStyle.NameAnchorMax;
            nameRT.pivot = MetaProgressStyle.NamePivot;
            nameRT.offsetMin = MetaProgressStyle.NameOffsetMin;
            nameRT.offsetMax = MetaProgressStyle.NameOffsetMax;
            TMP_Text nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text = "WEAPON";
            nameTxt.fontSize = MetaProgressStyle.NameFontSize;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Left;
            nameTxt.color = Color.white;
            nameTxt.raycastTarget = false;

            GameObject barBgGO = new GameObject(BarBgName, typeof(RectTransform), typeof(CanvasRenderer));
            barBgGO.transform.SetParent(infoGO.transform, false);
            RectTransform barBgRT = barBgGO.GetComponent<RectTransform>();
            barBgRT.anchorMin = MetaProgressStyle.BarAnchorMin;
            barBgRT.anchorMax = MetaProgressStyle.BarAnchorMax;
            barBgRT.pivot = MetaProgressStyle.BarPivot;
            barBgRT.offsetMin = MetaProgressStyle.BarOffsetMin;
            barBgRT.offsetMax = MetaProgressStyle.BarOffsetMax;
            Image barBgImg = barBgGO.AddComponent<Image>();
            barBgImg.sprite = slicedCard;
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = MetaProgressStyle.BarBgColor;
            barBgImg.raycastTarget = false;

            GameObject barFillGO = new GameObject(BarFillName, typeof(RectTransform), typeof(CanvasRenderer));
            barFillGO.transform.SetParent(barBgGO.transform, false);
            RectTransform barFillRT = barFillGO.GetComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = Vector2.one;
            barFillRT.pivot = new Vector2(0f, 0.5f);
            barFillRT.sizeDelta = Vector2.zero;
            barFillRT.anchoredPosition = Vector2.zero;
            barFillRT.localScale = new Vector3(0f, 1f, 1f);
            Image barFillImg = barFillGO.AddComponent<Image>();
            barFillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MetaProgressStyle.BarFillSprite);
            barFillImg.type = Image.Type.Sliced;
            barFillImg.color = Color.white;
            barFillImg.raycastTarget = false;

            GameObject barHiGO = new GameObject(BarHighlightName, typeof(RectTransform), typeof(CanvasRenderer));
            barHiGO.transform.SetParent(barFillGO.transform, false);
            RectTransform barHiRT = barHiGO.GetComponent<RectTransform>();
            barHiRT.anchorMin = new Vector2(0f, 1f);
            barHiRT.anchorMax = new Vector2(1f, 1f);
            barHiRT.pivot = new Vector2(0.5f, 1f);
            barHiRT.sizeDelta = new Vector2(0f, MetaProgressStyle.BarHighlightHeight);
            barHiRT.anchoredPosition = Vector2.zero;
            Image barHiImg = barHiGO.AddComponent<Image>();
            barHiImg.sprite = solid;
            barHiImg.type = Image.Type.Sliced;
            barHiImg.color = MetaProgressStyle.BarHighlightColor;
            barHiImg.raycastTarget = false;

            GameObject amtGO = new GameObject(AmountLabelName, typeof(RectTransform));
            amtGO.transform.SetParent(infoGO.transform, false);
            RectTransform amtRT = amtGO.GetComponent<RectTransform>();
            amtRT.anchorMin = MetaProgressStyle.AmountAnchorMin;
            amtRT.anchorMax = MetaProgressStyle.AmountAnchorMax;
            amtRT.pivot = MetaProgressStyle.AmountPivot;
            amtRT.offsetMin = MetaProgressStyle.AmountOffsetMin;
            amtRT.offsetMax = MetaProgressStyle.AmountOffsetMax;
            TMP_Text amtTxt = amtGO.AddComponent<TextMeshProUGUI>();
            amtTxt.text = "0 / 100";
            amtTxt.fontSize = MetaProgressStyle.AmountFontSize;
            amtTxt.fontStyle = FontStyles.Bold;
            amtTxt.alignment = TextAlignmentOptions.Right;
            amtTxt.color = MetaProgressStyle.AmountColor;
            amtTxt.raycastTarget = false;

            Image flashImg = AddStretchedImage(root.transform, FlashName, slicedCard,
                MetaProgressStyle.FlashColorIdle, 0f);

            GameObject badgeGO = new GameObject(UnlockedName, typeof(RectTransform));
            badgeGO.transform.SetParent(root.transform, false);
            RectTransform badgeRT = badgeGO.GetComponent<RectTransform>();
            badgeRT.anchorMin = MetaProgressStyle.UnlockedAnchorMin;
            badgeRT.anchorMax = MetaProgressStyle.UnlockedAnchorMax;
            badgeRT.pivot = MetaProgressStyle.UnlockedPivot;
            badgeRT.sizeDelta = MetaProgressStyle.UnlockedSize;
            badgeRT.anchoredPosition = MetaProgressStyle.UnlockedAnchoredPos;
            TMP_Text badgeTxt = badgeGO.AddComponent<TextMeshProUGUI>();
            badgeTxt.text = "✓ UNLOCKED";
            badgeTxt.fontSize = MetaProgressStyle.UnlockedFontSize;
            badgeTxt.fontStyle = FontStyles.Bold;
            badgeTxt.alignment = TextAlignmentOptions.Right;
            badgeTxt.color = Color.white;
            badgeTxt.raycastTarget = false;
            badgeGO.SetActive(false);

            MetaProgressRowUI row = root.AddComponent<MetaProgressRowUI>();
            SerializedObject rso = new SerializedObject(row);
            rso.FindProperty("weaponIcon").objectReferenceValue = iconImg;
            rso.FindProperty("nameLabel").objectReferenceValue = nameTxt;
            rso.FindProperty("barFill").objectReferenceValue = barFillImg;
            rso.FindProperty("amountLabel").objectReferenceValue = amtTxt;
            rso.FindProperty("unlockedBadge").objectReferenceValue = badgeGO;
            rso.FindProperty("unlockedBadgeLabel").objectReferenceValue = badgeTxt;
            rso.FindProperty("rowRoot").objectReferenceValue = rootRT;
            rso.FindProperty("animDuration").floatValue = MetaProgressStyle.RowAnimDuration;
            rso.FindProperty("rarityEdge").objectReferenceValue = edgeImg;
            rso.FindProperty("iconGlow").objectReferenceValue = glowImg;
            rso.FindProperty("iconFrame").objectReferenceValue = frameImg;
            rso.FindProperty("border").objectReferenceValue = borderImg;
            rso.FindProperty("flashOverlay").objectReferenceValue = flashImg;
            rso.FindProperty("barHighlight").objectReferenceValue = barHiImg;
            rso.ApplyModifiedPropertiesWithoutUndo();

            string dir = Path.GetDirectoryName(RowPrefabPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
        }
    }

    static Image AddStretchedImage(Transform parent, string name, Sprite sprite, Color32 color, float inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void StripLegacyChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null)
        {
            Object.DestroyImmediate(t.gameObject);
            Debug.Log($"[MetaProgressBuilder] stripped legacy child '{name}'");
        }
    }

    static WeaponProgressDefinition[] EnsureDemoDefinitions()
    {
        CurrencyConfig cc = AssetDatabase.LoadAssetAtPath<CurrencyConfig>(
            "Assets/ScriptableObjects/Core/CurrencyConfig.asset");
        int cashTarget = cc != null ? cc.demoCashProgressTarget : 50;

        var arr = new WeaponProgressDefinition[3];
        arr[0] = EnsureDef("WeaponProgress_Cash",    "cash",    "CASH",    cashTarget, "cash");
        arr[1] = EnsureDef("WeaponProgress_Shotgun", "shotgun", "SHOTGUN", 500,        "shotgun");
        arr[2] = EnsureDef("WeaponProgress_Rifle",   "rifle",   "RIFLE",   800,        "rifle");
        AssetDatabase.SaveAssets();
        return arr;
    }

    static RewardDefinition[] LoadAllRewardDefinitions()
    {
        string[] guids = AssetDatabase.FindAssets("t:RewardDefinition");
        var list = new System.Collections.Generic.List<RewardDefinition>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var rd = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (rd != null) list.Add(rd);
        }
        return list.ToArray();
    }

    static WeaponProgressDefinition EnsureDef(string assetName, string itemId,
                                              string displayName, int requiredPoints,
                                              string pointsRewardId)
    {
        string path = $"{DefinitionsDir}/{assetName}.asset";
        WeaponProgressDefinition def = AssetDatabase.LoadAssetAtPath<WeaponProgressDefinition>(path);
        bool created = false;
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<WeaponProgressDefinition>();
            AssetDatabase.CreateAsset(def, path);
            created = true;
        }

        bool changed = created
            || def.itemId != itemId
            || def.displayName != displayName
            || def.requiredPoints != requiredPoints
            || def.pointsRewardId != pointsRewardId
            || !def.initiallyVisible;

        if (changed)
        {
            def.itemId = itemId;
            def.displayName = displayName;
            def.requiredPoints = requiredPoints;
            def.pointsRewardId = pointsRewardId;
            def.initiallyVisible = true;
            EditorUtility.SetDirty(def);
            Debug.Log($"[MetaProgressBuilder] {(created ? "created" : "updated")} {path}");
        }
        return def;
    }
}
#endif
