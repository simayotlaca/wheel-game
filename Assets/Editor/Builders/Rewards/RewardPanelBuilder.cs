using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

internal static class RewardPanelBuilder
{
    public static void Build()
    {
        GameObject panelRoot = UILayoutBuilder.FindFirstInScene("ui_group_reward_list");
        if (panelRoot == null)
        {
            Debug.LogWarning("[RewardPanelBuilder.Build] ui_group_reward_list not found — aborting");
            return;
        }

        GameObject bgGO = UILayoutBuilder.EnsureChild(panelRoot.transform, "ui_image_reward_list_bg");
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(bgGO);
        Image bgImg = UILayoutBuilder.EnsureComponent<Image>(bgGO);
        bgImg.raycastTarget = false;

        bgGO.transform.SetSiblingIndex(0);
        UILayoutBuilder.SetImageSprite("ui_image_reward_list_bg",
            RewardPanelStyle.SpriteBg, Image.Type.Sliced);
        UILayoutBuilder.SetImageColor("ui_image_reward_list_bg", RewardPanelStyle.BgFill);
        UILayoutBuilder.StretchToParent("ui_image_reward_list_bg");

        GameObject outlineGO = UILayoutBuilder.EnsureChild(panelRoot.transform, "ui_image_reward_list_outline");
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(outlineGO);
        Image outlineImg = UILayoutBuilder.EnsureComponent<Image>(outlineGO);
        outlineImg.raycastTarget = false;

        outlineGO.transform.SetSiblingIndex(1);
        UILayoutBuilder.SetActiveByName("ui_image_reward_list_outline", true);
        UILayoutBuilder.SetImageSprite("ui_image_reward_list_outline",
            RewardPanelStyle.SpriteOutline, Image.Type.Sliced);
        UILayoutBuilder.SetImageColor("ui_image_reward_list_outline", RewardPanelStyle.OutlineTint);
        UILayoutBuilder.StretchToParent("ui_image_reward_list_outline");
        ConfigureOutline();

        UILayoutBuilder.Delete("ui_image_reward_list_highlight");
        UILayoutBuilder.Delete("ui_image_reward_list_divider");
        UILayoutBuilder.Delete("ui_image_reward_list_glow");

        BuildExitButton(panelRoot.transform);

        {
            CanvasGroup panelCg = UILayoutBuilder.EnsureComponent<CanvasGroup>(panelRoot);
            Undo.RecordObject(panelCg, UILayoutBuilder.UndoLabel);
            panelCg.alpha = DeathOverlayStyle.RewardListPromotedAlpha;
            panelCg.blocksRaycasts = true;
            panelCg.interactable = true;
            EditorUtility.SetDirty(panelCg);
        }

        ConfigureScrollContainer();
        ConfigureItemsContainer();
    }

    private static void ConfigureScrollContainer()
    {
        GameObject panelGO = UILayoutBuilder.FindFirstInScene("ui_group_reward_list");
        if (panelGO == null) { Debug.LogWarning("[RewardPanelBuilder.ConfigureScrollContainer] panel not found"); return; }

        GameObject contentGO = UILayoutBuilder.FindFirstInScene("ui_container_reward_list");
        if (contentGO == null) { Debug.LogWarning("[RewardPanelBuilder.ConfigureScrollContainer] container not found"); return; }

        GameObject scrollGO = UILayoutBuilder.EnsureChild(panelGO.transform, "ui_scroll_reward_list");
        GameObject viewportGO = UILayoutBuilder.EnsureChild(scrollGO.transform, "ui_viewport_reward_list");

        if (contentGO.transform.parent != viewportGO.transform)
        {
            Undo.SetTransformParent(contentGO.transform, viewportGO.transform, UILayoutBuilder.UndoLabel);
        }

        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        Undo.RecordObject(scrollRT, UILayoutBuilder.UndoLabel);
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.pivot     = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = RewardPanelStyle.ScrollOffsetMin;
        scrollRT.offsetMax = RewardPanelStyle.ScrollOffsetMax;
        EditorUtility.SetDirty(scrollRT);

        ScrollRect sr = UILayoutBuilder.EnsureComponent<ScrollRect>(scrollGO);
        Undo.RecordObject(sr, UILayoutBuilder.UndoLabel);
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = RewardPanelStyle.ScrollElasticity;
        sr.inertia = true;
        sr.decelerationRate = RewardPanelStyle.ScrollDecelerationRate;
        sr.scrollSensitivity = RewardPanelStyle.ScrollSensitivity;
        sr.viewport = viewportGO.GetComponent<RectTransform>();
        sr.content  = contentGO.GetComponent<RectTransform>();
        sr.horizontalScrollbar = null;
        sr.verticalScrollbar = BuildVerticalScrollbar(scrollGO.transform);
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        EditorUtility.SetDirty(sr);

        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        Undo.RecordObject(viewportRT, UILayoutBuilder.UndoLabel);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.pivot     = new Vector2(0.5f, 1f);
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(viewportRT);

        Mask staleMask = viewportGO.GetComponent<Mask>();
        if (staleMask != null) Undo.DestroyObjectImmediate(staleMask);

        Image vpImg = UILayoutBuilder.EnsureComponent<Image>(viewportGO);
        Undo.RecordObject(vpImg, UILayoutBuilder.UndoLabel);
        vpImg.color = RewardPanelStyle.ViewportRaycastColor;
        vpImg.raycastTarget = true;
        EditorUtility.SetDirty(vpImg);

        UILayoutBuilder.EnsureComponent<RectMask2D>(viewportGO);

        Transform exitT = panelGO.transform.Find("ui_panel_reward_list_exit_header");
        if (exitT != null)
        {
            scrollGO.transform.SetSiblingIndex(exitT.GetSiblingIndex());
            exitT.SetAsLastSibling();
        }
        else
        {
            scrollGO.transform.SetAsLastSibling();
        }

        RewardListUI listUI = panelGO.GetComponentInChildren<RewardListUI>(true);
        if (listUI != null)
        {
            UILayoutBuilder.Wire(listUI, "scrollRect", sr);
        }

        Debug.Log("[RewardPanelBuilder] scroll wired, vertical, clamped, RectMask2D clip");
    }

    private static Scrollbar BuildVerticalScrollbar(Transform scrollParent)
    {
        GameObject sbGO = UILayoutBuilder.EnsureChild(scrollParent, "ui_scrollbar_reward_list");
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        Undo.RecordObject(sbRT, UILayoutBuilder.UndoLabel);
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot     = new Vector2(1f, 0.5f);
        sbRT.sizeDelta = new Vector2(RewardPanelStyle.ScrollbarWidth, -RewardPanelStyle.ScrollbarVerticalPadding * 2f);
        sbRT.anchoredPosition = new Vector2(-RewardPanelStyle.ScrollbarInset, 0f);
        EditorUtility.SetDirty(sbRT);

        Image sbBg = UILayoutBuilder.EnsureComponent<Image>(sbGO);
        Undo.RecordObject(sbBg, UILayoutBuilder.UndoLabel);
        sbBg.color = RewardPanelStyle.ScrollbarTrackColor;
        sbBg.raycastTarget = true;
        EditorUtility.SetDirty(sbBg);

        GameObject slidingGO = UILayoutBuilder.EnsureChild(sbGO.transform, "Sliding Area");
        RectTransform slidingRT = slidingGO.GetComponent<RectTransform>();
        Undo.RecordObject(slidingRT, UILayoutBuilder.UndoLabel);
        slidingRT.anchorMin = Vector2.zero;
        slidingRT.anchorMax = Vector2.one;
        slidingRT.offsetMin = Vector2.zero;
        slidingRT.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(slidingRT);

        GameObject handleGO = UILayoutBuilder.EnsureChild(slidingGO.transform, "Handle");
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        Undo.RecordObject(handleRT, UILayoutBuilder.UndoLabel);
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(handleRT);

        Image handleImg = UILayoutBuilder.EnsureComponent<Image>(handleGO);
        Undo.RecordObject(handleImg, UILayoutBuilder.UndoLabel);
        handleImg.color = RewardPanelStyle.ScrollbarHandleColor;
        handleImg.raycastTarget = true;
        EditorUtility.SetDirty(handleImg);

        Scrollbar sb = UILayoutBuilder.EnsureComponent<Scrollbar>(sbGO);
        Undo.RecordObject(sb, UILayoutBuilder.UndoLabel);
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;
        EditorUtility.SetDirty(sb);

        return sb;
    }

    private static void ConfigureOutline()
    {
        GameObject go = UILayoutBuilder.FindFirstInScene("ui_image_reward_list_outline");
        if (go == null) { Debug.LogWarning("[RewardPanelBuilder.ConfigureOutline] outline not found"); return; }
        Image img = go.GetComponent<Image>();
        if (img == null) return;
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.raycastTarget = false;

        img.pixelsPerUnitMultiplier = RewardPanelStyle.OutlinePPUMul;
        go.transform.SetSiblingIndex(1);
        EditorUtility.SetDirty(img);
    }

    private static void BuildExitButton(Transform parent)
    {
        GameObject panelGO = UILayoutBuilder.EnsureChild(parent, "ui_panel_reward_list_exit_header");
        panelGO.transform.SetAsLastSibling();

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(panelGO);
        Image bg = UILayoutBuilder.EnsureComponent<Image>(panelGO);
        Button btn = UILayoutBuilder.EnsureComponent<Button>(panelGO);

        RectTransform rt = panelGO.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(RewardPanelStyle.ExitW, RewardPanelStyle.ExitH);
        rt.anchoredPosition = new Vector2(0f, RewardPanelStyle.ExitYOffset);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(rt);

        LayoutElement le = UILayoutBuilder.EnsureComponent<LayoutElement>(panelGO);
        Undo.RecordObject(le, UILayoutBuilder.UndoLabel);
        le.ignoreLayout = true;
        EditorUtility.SetDirty(le);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RewardPanelStyle.SpriteExitBg);
        Undo.RecordObject(bg, UILayoutBuilder.UndoLabel);
        if (sprite != null)
        {
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
        }
        bg.color = RewardPanelStyle.ExitBgTint;
        bg.raycastTarget = true;
        EditorUtility.SetDirty(bg);

        Outline outline = UILayoutBuilder.EnsureComponent<Outline>(panelGO);
        Undo.RecordObject(outline, UILayoutBuilder.UndoLabel);

        outline.effectColor = RewardPanelStyle.ExitInnerShadow;
        outline.effectDistance = RewardPanelStyle.ExitInnerDist;
        EditorUtility.SetDirty(outline);

        UILayoutBuilder.Delete("ui_image_reward_list_exit_sheen");

        Undo.RecordObject(btn, UILayoutBuilder.UndoLabel);
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;

        cb.normalColor = RewardPanelStyle.ExitBgTint;
        cb.highlightedColor = RewardPanelStyle.ExitHighlighted;
        cb.pressedColor = RewardPanelStyle.ExitPressed;
        cb.disabledColor = RewardPanelStyle.ExitDisabled;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = RewardPanelStyle.ExitFadeDuration;
        btn.colors = cb;
        EditorUtility.SetDirty(btn);

        GameObject labelGO = UILayoutBuilder.EnsureChild(panelGO.transform, "ui_label_reward_list_exit_text");
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(labelGO);
        TextMeshProUGUI tmp = UILayoutBuilder.EnsureComponent<TextMeshProUGUI>(labelGO);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        Undo.RecordObject(labelRT, UILayoutBuilder.UndoLabel);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelRT.localScale = Vector3.one;
        labelRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(labelRT);

        Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
        tmp.text = "EXIT";
        tmp.fontSize = RewardPanelStyle.ExitFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = RewardPanelStyle.ExitCharSpacing;
        tmp.color = RewardPanelStyle.ExitTextColor;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(tmp);

        Outline textHalo = UILayoutBuilder.EnsureComponent<Outline>(labelGO);
        Undo.RecordObject(textHalo, UILayoutBuilder.UndoLabel);
        textHalo.effectColor = RewardPanelStyle.ExitTextGlow;
        textHalo.effectDistance = RewardPanelStyle.ExitTextGlowDist;
        EditorUtility.SetDirty(textHalo);

        Debug.Log("[RewardPanelBuilder.BuildExitButton] EXIT button (gray) at top of reward panel size=170x52 pos=(0,-18)");
    }

    private static void ConfigureItemsContainer()
    {
        GameObject go = UILayoutBuilder.FindFirstInScene("ui_container_reward_list");
        if (go == null) { Debug.LogWarning("[RewardPanelBuilder.ConfigureItemsContainer] ui_container_reward_list not found"); return; }

        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        EditorUtility.SetDirty(rt);

        VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
        Undo.RecordObject(vlg, UILayoutBuilder.UndoLabel);
        vlg.spacing = RewardPanelStyle.ItemsSpacing;
        vlg.padding = new RectOffset(0, 0, RewardPanelStyle.ItemsPadTop, 0);
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        EditorUtility.SetDirty(vlg);

        ContentSizeFitter csf = UILayoutBuilder.EnsureComponent<ContentSizeFitter>(go);
        Undo.RecordObject(csf, UILayoutBuilder.UndoLabel);
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EditorUtility.SetDirty(csf);

        Debug.Log("[RewardPanelBuilder.ConfigureItemsContainer] content top-stretch · ContentSizeFitter vertical=PreferredSize · VLG spacing=" + RewardPanelStyle.ItemsSpacing);
    }

    public static void WireCollectAnimator()
    {
        GameObject panelGO = UILayoutBuilder.FindFirstInScene("ui_group_reward_list");
        if (panelGO == null) { Debug.LogWarning("[WireCollectAnimator] ui_group_reward_list not found"); return; }

        RewardCollectAnimator anim = panelGO.GetComponent<RewardCollectAnimator>();
        if (anim == null) anim = Undo.AddComponent<RewardCollectAnimator>(panelGO);

        WheelController controller = Resources.FindObjectsOfTypeAll<WheelController>().Length > 0
            ? UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>())
            : null;
        RewardListUI list = panelGO.GetComponentInChildren<RewardListUI>(true);
        if (list == null) list = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<RewardListUI>());
        GameObject exitGO = UILayoutBuilder.FindFirstInScene("ui_panel_reward_list_exit_header");
        Button exitBtn = exitGO != null ? exitGO.GetComponent<Button>() : null;

        GameObject flyHostGO = UILayoutBuilder.FindFirstInScene("ui_canvas_static");
        RectTransform flyHostRT = flyHostGO != null ? flyHostGO.GetComponent<RectTransform>() : null;

        SpinRewardFlyAnimator spinFly = panelGO.GetComponent<SpinRewardFlyAnimator>();
        if (spinFly == null) spinFly = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<SpinRewardFlyAnimator>());

        string[] cfgGuids = AssetDatabase.FindAssets("t:WheelAnimationConfig");
        WheelAnimationConfig animCfg = (cfgGuids != null && cfgGuids.Length > 0)
            ? AssetDatabase.LoadAssetAtPath<WheelAnimationConfig>(AssetDatabase.GUIDToAssetPath(cfgGuids[0]))
            : null;

        UILayoutBuilder.Wire(anim, "controller", controller);
        UILayoutBuilder.Wire(anim, "rewardList", list);
        UILayoutBuilder.Wire(anim, "exitButton", exitBtn);
        UILayoutBuilder.Wire(anim, "spinFlyAnimator", spinFly);
        UILayoutBuilder.Wire(anim, "flyContainer", flyHostRT);
        UILayoutBuilder.Wire(anim, "animConfig", animCfg);

        Debug.Log(string.Format(
            "[RewardPanelBuilder.WireCollectAnimator] ctrl={0} list={1} exit={2} spinFly={3} flyHost={4} cfg={5}",
            controller != null, list != null, exitBtn != null, spinFly != null, flyHostRT != null, animCfg != null));
    }

    public static void WireSpinFlyAnimator()
    {
        GameObject panelGO = UILayoutBuilder.FindFirstInScene("ui_group_reward_list");
        if (panelGO == null) { Debug.LogWarning("[WireSpinFlyAnimator] ui_group_reward_list not found"); return; }

        SpinRewardFlyAnimator anim = panelGO.GetComponent<SpinRewardFlyAnimator>();
        if (anim == null) anim = Undo.AddComponent<SpinRewardFlyAnimator>(panelGO);

        WheelController controller = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>());
        WheelView wheelView = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelView>());
        RewardListUI list = panelGO.GetComponentInChildren<RewardListUI>(true);
        if (list == null) list = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<RewardListUI>());

        GameObject flyHostGO = UILayoutBuilder.FindFirstInScene("ui_canvas_static");
        RectTransform flyHostRT = flyHostGO != null ? flyHostGO.GetComponent<RectTransform>() : null;

        string[] cfgGuids = AssetDatabase.FindAssets("t:WheelAnimationConfig");
        WheelAnimationConfig cfg = (cfgGuids != null && cfgGuids.Length > 0)
            ? AssetDatabase.LoadAssetAtPath<WheelAnimationConfig>(AssetDatabase.GUIDToAssetPath(cfgGuids[0]))
            : null;

        UILayoutBuilder.Wire(anim, "controller", controller);
        UILayoutBuilder.Wire(anim, "wheelView", wheelView);
        UILayoutBuilder.Wire(anim, "rewardList", list);
        UILayoutBuilder.Wire(anim, "flyContainer", flyHostRT);
        UILayoutBuilder.Wire(anim, "animConfig", cfg);

        ZoneHUD zoneHUD = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<ZoneHUD>());
        if (zoneHUD != null)
        {
            UILayoutBuilder.Wire(zoneHUD, "controller", controller);
            UILayoutBuilder.Wire(zoneHUD, "spinFlyAnimator", anim);
        }
        SpinHintUI hint = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<SpinHintUI>());
        if (hint != null)
        {
            UILayoutBuilder.Wire(hint, "controller", controller);
            UILayoutBuilder.Wire(hint, "animConfig", cfg);
            UILayoutBuilder.Wire(hint, "spinFlyAnimator", anim);
        }

        Debug.Log(string.Format(
            "[RewardPanelBuilder.WireSpinFlyAnimator] ctrl={0} view={1} list={2} flyHost={3} animCfg={4} zoneHUD={5} hint={6}",
            controller != null, wheelView != null, list != null, flyHostRT != null, cfg != null, zoneHUD != null, hint != null));
    }

    public static void RestyleItemPrefab()
    {
        const string path = "Assets/Prefabs/RewardListItem.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning("[RewardPanelBuilder.RestyleItemPrefab] prefab not found at " + path); return; }

        try
        {
            RectTransform rootRT = root.GetComponent<RectTransform>();
            if (rootRT != null)
            {
                rootRT.sizeDelta = RewardPanelStyle.RowSize;
                rootRT.anchorMin = new Vector2(0.5f, 1f);
                rootRT.anchorMax = new Vector2(0.5f, 1f);
                rootRT.pivot     = new Vector2(0.5f, 0.5f);
            }

            LayoutElement rootLE = root.GetComponent<LayoutElement>();
            if (rootLE != null)
            {
                rootLE.minHeight = RewardPanelStyle.RowMinHeight;
                rootLE.preferredHeight = RewardPanelStyle.RowMinHeight;
            }

            HorizontalLayoutGroup hlg = root.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(RewardPanelStyle.RowPadLeft, 0, 0, 0);
                hlg.spacing = RewardPanelStyle.RowSpacing;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            Transform staleWrapper = root.transform.Find("ui_container_reward_row_content");
            if (staleWrapper != null)
            {
                while (staleWrapper.childCount > 0)
                    staleWrapper.GetChild(0).SetParent(root.transform, false);
                Object.DestroyImmediate(staleWrapper.gameObject);
            }

            Transform frameT = root.transform.Find("ui_image_reward_icon_frame");

            if (frameT != null)
            {
                frameT.gameObject.SetActive(true);
                RectTransform frameRT = frameT.GetComponent<RectTransform>();
                if (frameRT != null)
                {
                    frameRT.sizeDelta = new Vector2(RewardPanelStyle.IconFrameSize, RewardPanelStyle.IconFrameSize);
                    frameRT.localScale = Vector3.one;
                    frameRT.localRotation = Quaternion.identity;
                }
                Image frameImg = frameT.GetComponent<Image>();
                if (frameImg != null)
                {
                    Sprite frameSpr = AssetDatabase.LoadAssetAtPath<Sprite>(RewardPanelStyle.SpriteIconFrame);
                    if (frameSpr != null)
                    {
                        frameImg.sprite = frameSpr;
                        frameImg.type = Image.Type.Sliced;
                    }
                    frameImg.color = RewardPanelStyle.IconFrameTint;
                    frameImg.raycastTarget = false;
                    frameImg.maskable = true;
                }
                LayoutElement frameLE = frameT.GetComponent<LayoutElement>();
                if (frameLE == null) frameLE = frameT.gameObject.AddComponent<LayoutElement>();
                frameLE.minWidth = RewardPanelStyle.IconFrameSize;
                frameLE.minHeight = RewardPanelStyle.IconFrameSize;
                frameLE.preferredWidth = RewardPanelStyle.IconFrameSize;
                frameLE.preferredHeight = RewardPanelStyle.IconFrameSize;
            }

            Transform iconT = UILayoutBuilder.FindDeep(root.transform, "ui_image_reward_icon");
            if (iconT != null)
            {
                RectTransform iconRT = iconT.GetComponent<RectTransform>();
                if (iconRT != null)
                {
                    iconRT.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRT.pivot     = new Vector2(0.5f, 0.5f);
                    iconRT.sizeDelta = new Vector2(RewardPanelStyle.IconSize, RewardPanelStyle.IconSize);
                    iconRT.anchoredPosition = Vector2.zero;
                    iconRT.localScale = Vector3.one;
                    iconRT.localRotation = Quaternion.identity;
                }
                Image iconImg = iconT.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.preserveAspect = true;
                    iconImg.color = Color.white;
                    iconImg.enabled = true;
                    iconImg.raycastTarget = false;
                    iconImg.maskable = true;
                }
                iconT.gameObject.SetActive(true);
            }

            Transform amountT = root.transform.Find("ui_text_reward_amount_value")
                             ?? root.transform.Find("ui_label_reward_amount_value");
            if (amountT != null)
            {
                RectTransform amountRT = amountT.GetComponent<RectTransform>();
                if (amountRT != null)
                {
                    amountRT.sizeDelta = RewardPanelStyle.AmountSize;
                    amountRT.localScale = Vector3.one;
                    amountRT.localRotation = Quaternion.identity;
                }

                LayoutElement amountLE = amountT.GetComponent<LayoutElement>();
                if (amountLE == null) amountLE = amountT.gameObject.AddComponent<LayoutElement>();
                amountLE.preferredWidth  = RewardPanelStyle.AmountSize.x;
                amountLE.preferredHeight = RewardPanelStyle.AmountSize.y;
                amountLE.flexibleWidth   = 0f;

                TMP_Text tmp = amountT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = RewardPanelStyle.AmountFontSize;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontStyle |= FontStyles.Bold;
                    tmp.maskable = true;

                    tmp.enableWordWrapping = true;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[RewardPanelBuilder.RestyleItemPrefab] DONE — row={RewardPanelStyle.RowSize.x}x{RewardPanelStyle.RowSize.y} icon={RewardPanelStyle.IconSize} frame={RewardPanelStyle.IconFrameSize} amountFS={RewardPanelStyle.AmountFontSize}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
