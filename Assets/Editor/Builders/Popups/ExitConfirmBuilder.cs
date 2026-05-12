using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

internal static class ExitConfirmBuilder
{
    private const float BtnW = 380f;
    private const float BtnH = 104f;
    private const float BtnGap = 48f;

    public static void Build()
    {
        GameObject overlay = UILayoutBuilder.FindFirstInScene("ui_canvas_overlay");
        if (overlay == null)
        {
            Debug.LogWarning("[ExitConfirmBuilder] ui_canvas_overlay not found");
            return;
        }

        GameObject panelGO = UILayoutBuilder.EnsureChild(overlay.transform, "ui_panel_exit_confirm");
        FillParent(panelGO);

        ExitConfirmPanel script = panelGO.GetComponent<ExitConfirmPanel>();
        if (script == null) script = Undo.AddComponent<ExitConfirmPanel>(panelGO);

        DestroyIfExists(panelGO.transform, "ui_group_exit_safe");

        GameObject collectRoot = BuildCollectState(panelGO.transform);
        GameObject freshRoot   = BuildFreshStartState(panelGO.transform);

        collectRoot.SetActive(false);
        freshRoot.SetActive(false);

        WirePanel(script, collectRoot, freshRoot);
    }

    private static GameObject BuildCollectState(Transform parent)
    {
        GameObject root = BuildStateRoot(parent, "ui_group_exit_collect");

        BuildBlurBackdrop(root.transform, "ui_image_exit_collect_blur");

        BuildBodyLabel(
            root.transform,
            "ui_label_exit_collect_body",
            "Sure you want to cash out?\nThe best rewards are still ahead!",
            anchoredY: 45f,
            width: 900f, height: 100f,
            fontSize: 35f, lineSpacing: -8f);

        const float CollectBtnW = 312f;
        const float CollectBtnH = 76f;
        const float CollectFont = 28f;
        const float CollectGap  = 22f;

        GameObject row = UILayoutBuilder.EnsureChild(root.transform, "ui_group_exit_collect_buttons");
        StyleButtonRowAnchored(
            row, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f),
            gap: CollectGap,
            widthOverride: 2 * CollectBtnW + CollectGap,
            heightOverride: CollectBtnH);

        BuildButton(row.transform, "ui_button_exit_collect_confirm",
            "CASH OUT", new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            "Assets/Sprites/Wheel/UI_button_collect_green.png",
            CollectBtnW, CollectBtnH, CollectFont,
            new Color32(0x0E, 0x4A, 0x1A, 230));
        BuildButton(row.transform, "ui_button_exit_collect_cancel",
            "KEEP SPINNING", new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            "Assets/Sprites/Wheel/UI_button_collect_orange.png",
            CollectBtnW, CollectBtnH, CollectFont,
            new Color32(0x6E, 0x2C, 0x00, 230));

        UILayoutBuilder.SetSiblingOrder(root.transform, new[] {
            "ui_image_exit_collect_blur",
            "ui_label_exit_collect_body",
            "ui_group_exit_collect_buttons"
        });

        return root;
    }

    private static GameObject BuildFreshStartState(Transform parent)
    {
        GameObject root = BuildStateRoot(parent, "ui_group_exit_fresh");

        DestroyIfExists(root.transform, "ui_image_exit_fresh_card");
        DestroyIfExists(root.transform, "ui_image_exit_fresh_glow");

        BuildBlurBackdrop(root.transform, "ui_image_exit_fresh_blur");

        BuildBodyLabel(
            root.transform,
            "ui_label_exit_fresh_body",
            "You haven't earned any rewards yet. Do you still want to exit?",
            anchoredY: 95f);

        GameObject row = UILayoutBuilder.EnsureChild(root.transform, "ui_group_exit_fresh_buttons");
        StyleButtonRowAnchored(row, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f));

        const float FreshBtnW = 270f;
        const float FreshBtnH = 76f;
        const float FreshFont = 28f;
        BuildButton(row.transform, "ui_button_exit_fresh_confirm",
            "EXIT", new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            "Assets/Sprites/Wheel/UI_button_exit_silver.png",
            FreshBtnW, FreshBtnH, FreshFont,
            new Color32(0x4F, 0x56, 0x61, 220));
        BuildButton(row.transform, "ui_button_exit_fresh_cancel",
            "GO BACK", new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            "Assets/Sprites/Wheel/UI_button_exit_orange.png",
            FreshBtnW, FreshBtnH, FreshFont,
            new Color32(0x6E, 0x2C, 0x00, 230));

        UILayoutBuilder.SetSiblingOrder(root.transform, new[] {
            "ui_image_exit_fresh_blur",
            "ui_label_exit_fresh_body",
            "ui_group_exit_fresh_buttons"
        });

        return root;
    }

    private static GameObject BuildStateRoot(Transform parent, string name)
    {
        GameObject root = UILayoutBuilder.EnsureChild(parent, name);
        FillParent(root);
        return root;
    }

    private static void BuildBlurBackdrop(Transform parent, string name)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        FillParent(go);

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);
        Image img = UILayoutBuilder.EnsureComponent<Image>(go);
        Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
        img.sprite = null;
        img.color = new Color32(0x05, 0x07, 0x0B, 210);
        img.raycastTarget = true;
        EditorUtility.SetDirty(img);
    }

    private static void BuildBodyLabel(
        Transform parent, string name, string text, float anchoredY,
        float width = 1300f, float height = 120f,
        float fontSize = 40f, float lineSpacing = -4f)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        EditorUtility.SetDirty(rt);

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);
        TextMeshProUGUI tmp = UILayoutBuilder.EnsureComponent<TextMeshProUGUI>(go);
        Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = lineSpacing;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(tmp);

        Outline outline = UILayoutBuilder.EnsureComponent<Outline>(go);
        Undo.RecordObject(outline, UILayoutBuilder.UndoLabel);
        outline.effectColor = new Color32(0, 0, 0, 230);
        outline.effectDistance = new Vector2(1.8f, -1.8f);
        EditorUtility.SetDirty(outline);

        Shadow shadow = UILayoutBuilder.EnsureComponent<Shadow>(go);
        Undo.RecordObject(shadow, UILayoutBuilder.UndoLabel);
        shadow.effectColor = new Color32(0, 0, 0, 240);
        shadow.effectDistance = new Vector2(2f, -2f);
        EditorUtility.SetDirty(shadow);
    }

    private static void StyleButtonRowAnchored(
        GameObject row, Vector2 anchor, Vector2 anchoredPos,
        float gap = BtnGap, float widthOverride = -1f, float heightOverride = -1f)
    {
        float rowH = heightOverride > 0f ? heightOverride : BtnH;
        float rowW = widthOverride  > 0f ? widthOverride  : BtnW * 2 + gap;

        RectTransform rt = row.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(rowW, rowH);
        rt.anchoredPosition = anchoredPos;
        EditorUtility.SetDirty(rt);

        HorizontalLayoutGroup hlg = UILayoutBuilder.EnsureComponent<HorizontalLayoutGroup>(row);
        Undo.RecordObject(hlg, UILayoutBuilder.UndoLabel);
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = gap;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        EditorUtility.SetDirty(hlg);
    }

    private static GameObject BuildButton(Transform parent, string name, string label, Color32 fill, string spritePath = "Assets/Sprites/Wheel/UI_button_grey_standard.png", float widthOverride = -1f, float heightOverride = -1f, float labelFontSize = -1f, Color32? labelOutlineColor = null)
    {
        float w = widthOverride > 0f ? widthOverride : BtnW;
        float h = heightOverride > 0f ? heightOverride : BtnH;

        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.sizeDelta = new Vector2(w, h);
        EditorUtility.SetDirty(rt);

        LayoutElement le = UILayoutBuilder.EnsureComponent<LayoutElement>(go);
        Undo.RecordObject(le, UILayoutBuilder.UndoLabel);
        le.preferredWidth = w;
        le.preferredHeight = h;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
        le.minWidth = w;
        le.minHeight = h;
        EditorUtility.SetDirty(le);

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);
        Image bg = UILayoutBuilder.EnsureComponent<Image>(go);
        Undo.RecordObject(bg, UILayoutBuilder.UndoLabel);
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (spr != null) { bg.sprite = spr; bg.type = Image.Type.Sliced; }
        bg.color = fill;
        bg.raycastTarget = true;
        EditorUtility.SetDirty(bg);

        Outline outline = UILayoutBuilder.EnsureComponent<Outline>(go);
        Undo.RecordObject(outline, UILayoutBuilder.UndoLabel);
        outline.effectColor = new Color32(0, 0, 0, 140);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        EditorUtility.SetDirty(outline);

        Button btn = UILayoutBuilder.EnsureComponent<Button>(go);
        Undo.RecordObject(btn, UILayoutBuilder.UndoLabel);
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        EditorUtility.SetDirty(btn);

        BuildGlossStrip(go.transform, "ui_image_button_top_highlight",
            top: true,  height: h * 0.22f, color: new Color32(0xFF, 0xFF, 0xFF, 80));
        BuildGlossStrip(go.transform, "ui_image_button_bottom_shadow",
            top: false, height: h * 0.18f, color: new Color32(0x00, 0x00, 0x00, 55));

        GameObject lblGO = UILayoutBuilder.EnsureChild(go.transform, "ui_label_button_text");
        FillParent(lblGO);
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(lblGO);
        TextMeshProUGUI tmp = UILayoutBuilder.EnsureComponent<TextMeshProUGUI>(lblGO);
        Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
        tmp.text = label;
        tmp.fontSize = labelFontSize > 0f ? labelFontSize : 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.characterSpacing = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(tmp);

        if (labelOutlineColor.HasValue)
        {
            Outline lblOutline = UILayoutBuilder.EnsureComponent<Outline>(lblGO);
            Undo.RecordObject(lblOutline, UILayoutBuilder.UndoLabel);
            lblOutline.effectColor = labelOutlineColor.Value;
            lblOutline.effectDistance = new Vector2(1.6f, -1.6f);
            EditorUtility.SetDirty(lblOutline);

            Shadow lblShadow = UILayoutBuilder.EnsureComponent<Shadow>(lblGO);
            Undo.RecordObject(lblShadow, UILayoutBuilder.UndoLabel);
            lblShadow.effectColor = new Color32(0, 0, 0, 180);
            lblShadow.effectDistance = new Vector2(0f, -2f);
            EditorUtility.SetDirty(lblShadow);
        }

        UILayoutBuilder.SetSiblingOrder(go.transform, new[] {
            "ui_image_button_top_highlight",
            "ui_image_button_bottom_shadow",
            "ui_label_button_text"
        });

        return go;
    }

    private static void WirePanel(ExitConfirmPanel script, GameObject collectRoot, GameObject freshRoot)
    {
        Button collectConfirm = FindButton(collectRoot.transform, "ui_button_exit_collect_confirm");
        Button collectCancel  = FindButton(collectRoot.transform, "ui_button_exit_collect_cancel");
        Button freshConfirm   = FindButton(freshRoot.transform,   "ui_button_exit_fresh_confirm");
        Button freshCancel    = FindButton(freshRoot.transform,   "ui_button_exit_fresh_cancel");

        HudOverlayDemoter demoter = HudOverlayDemoterBuilder.FindInScene();

        UILayoutBuilder.Wire(script, "safeExitRoot", collectRoot);
        UILayoutBuilder.Wire(script, "safeExitConfirmButton", collectConfirm);
        UILayoutBuilder.Wire(script, "safeExitCancelButton", collectCancel);
        UILayoutBuilder.Wire(script, "freshStartRoot", freshRoot);
        UILayoutBuilder.Wire(script, "freshStartConfirmButton", freshConfirm);
        UILayoutBuilder.Wire(script, "freshStartCancelButton", freshCancel);
        UILayoutBuilder.Wire(script, "hudDemoter", demoter);

        Debug.Log(string.Format(
            "[ExitConfirmBuilder] wired collect(c/x)={0}/{1} fresh(c/x)={2}/{3} demoter={4}",
            collectConfirm != null, collectCancel != null,
            freshConfirm != null, freshCancel != null, demoter != null));
    }

    private static Button FindButton(Transform root, string name)
    {
        Transform t = UILayoutBuilder.FindAnywhere(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static void DestroyIfExists(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
    }

    private static void FillParent(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }

    private static void BuildGlossStrip(Transform parent, string name, bool top, float height, Color32 color)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        float edge = top ? 1f : 0f;
        rt.anchorMin = new Vector2(0f, edge);
        rt.anchorMax = new Vector2(1f, edge);
        rt.pivot = new Vector2(0.5f, edge);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
        EditorUtility.SetDirty(rt);

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);
        Image img = UILayoutBuilder.EnsureComponent<Image>(go);
        Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
        img.sprite = null;
        img.color = color;
        img.raycastTarget = false;
        EditorUtility.SetDirty(img);
    }
}
