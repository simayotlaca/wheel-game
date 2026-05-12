using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

internal static class CurrencyHUDBuilder
{
    public static void Build()
    {
        GameObject canvas = UILayoutBuilder.FindFirstInScene("ui_canvas_static");
        if (canvas == null)
        {
            Debug.LogWarning("[CurrencyHUDBuilder] ui_canvas_static not found — aborting");
            return;
        }

        GameObject containerGO = UILayoutBuilder.EnsureChild(canvas.transform, "ui_group_top_currency");
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        Undo.RecordObject(containerRT, UILayoutBuilder.UndoLabel);
        containerRT.anchorMin = new Vector2(1f, 1f);
        containerRT.anchorMax = new Vector2(1f, 1f);
        containerRT.pivot = new Vector2(1f, 1f);
        containerRT.sizeDelta = CurrencyHUDStyle.ContainerSize;
        containerRT.anchoredPosition = CurrencyHUDStyle.ContainerOffset + new Vector2(0f, 14f);
        containerRT.localScale = CurrencyHUDStyle.ContainerScale;
        containerRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(containerRT);

        HorizontalLayoutGroup containerHLG = UILayoutBuilder.EnsureComponent<HorizontalLayoutGroup>(containerGO);
        Undo.RecordObject(containerHLG, UILayoutBuilder.UndoLabel);
        containerHLG.padding = new RectOffset(0, 0, 0, 0);
        containerHLG.spacing = CurrencyHUDStyle.ContainerSpacing;
        containerHLG.childAlignment = TextAnchor.MiddleRight;
        containerHLG.childControlWidth = false;
        containerHLG.childControlHeight = false;
        containerHLG.childForceExpandWidth = false;
        containerHLG.childForceExpandHeight = false;
        containerHLG.childScaleWidth = false;
        containerHLG.childScaleHeight = false;
        EditorUtility.SetDirty(containerHLG);

        ContentSizeFitter containerCSF = UILayoutBuilder.EnsureComponent<ContentSizeFitter>(containerGO);
        Undo.RecordObject(containerCSF, UILayoutBuilder.UndoLabel);
        containerCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        containerCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EditorUtility.SetDirty(containerCSF);

        BuildCurrencyGroup(
            containerGO.transform,
            "ui_group_cash",
            "ui_image_cash_icon",
            "ui_text_cash_value",
            CurrencyHUDStyle.CashColor,
            CurrencyHUDStyle.SpriteCashIcon);

        BuildCurrencyGroup(
            containerGO.transform,
            "ui_group_coin",
            "ui_image_coin_icon",
            "ui_text_coin_value",
            CurrencyHUDStyle.CoinColor,
            CurrencyHUDStyle.SpriteCoinIcon);

        UILayoutHelpers.Delete("ui_button_currency_plus");

        Transform cashT = containerGO.transform.Find("ui_group_cash");
        Transform coinT = containerGO.transform.Find("ui_group_coin");
        if (cashT != null) cashT.SetSiblingIndex(0);
        if (coinT != null) coinT.SetSiblingIndex(1);

        AttachHUD(containerGO);

        containerGO.SetActive(true);
    }

    private static void BuildCurrencyGroup(
        Transform parent,
        string groupName,
        string iconName,
        string textName,
        Color32 textColor,
        string spritePath)
    {
        GameObject groupGO = UILayoutBuilder.EnsureChild(parent, groupName);

        RectTransform groupRT = groupGO.GetComponent<RectTransform>();
        Undo.RecordObject(groupRT, UILayoutBuilder.UndoLabel);
        groupRT.localScale = Vector3.one;
        groupRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(groupRT);

        HorizontalLayoutGroup hlg = UILayoutBuilder.EnsureComponent<HorizontalLayoutGroup>(groupGO);
        Undo.RecordObject(hlg, UILayoutBuilder.UndoLabel);
        hlg.padding = new RectOffset(CurrencyHUDStyle.PillPadL, CurrencyHUDStyle.PillPadR, 0, 0);
        hlg.spacing = CurrencyHUDStyle.PillSpacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childScaleWidth = false;
        hlg.childScaleHeight = false;
        EditorUtility.SetDirty(hlg);

        ContentSizeFitter csf = UILayoutBuilder.EnsureComponent<ContentSizeFitter>(groupGO);
        Undo.RecordObject(csf, UILayoutBuilder.UndoLabel);
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EditorUtility.SetDirty(csf);

        LayoutElement groupLE = UILayoutBuilder.EnsureComponent<LayoutElement>(groupGO);
        Undo.RecordObject(groupLE, UILayoutBuilder.UndoLabel);
        groupLE.minHeight = CurrencyHUDStyle.PillMinHeight;
        groupLE.preferredHeight = CurrencyHUDStyle.PillMinHeight;
        groupLE.flexibleWidth = -1f;
        groupLE.flexibleHeight = -1f;
        EditorUtility.SetDirty(groupLE);

        GameObject iconGO = UILayoutBuilder.EnsureChild(groupGO.transform, iconName);
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(iconGO);
        Image iconImg = UILayoutBuilder.EnsureComponent<Image>(iconGO);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        Undo.RecordObject(iconRT, UILayoutBuilder.UndoLabel);
        iconRT.sizeDelta = new Vector2(CurrencyHUDStyle.IconSize, CurrencyHUDStyle.IconSize);
        iconRT.localScale = Vector3.one;
        iconRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(iconRT);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        Undo.RecordObject(iconImg, UILayoutBuilder.UndoLabel);
        if (sprite != null) iconImg.sprite = sprite;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        EditorUtility.SetDirty(iconImg);

        LayoutElement iconLE = UILayoutBuilder.EnsureComponent<LayoutElement>(iconGO);
        Undo.RecordObject(iconLE, UILayoutBuilder.UndoLabel);
        iconLE.minWidth = CurrencyHUDStyle.IconSize;
        iconLE.minHeight = CurrencyHUDStyle.IconSize;
        iconLE.preferredWidth = CurrencyHUDStyle.IconSize;
        iconLE.preferredHeight = CurrencyHUDStyle.IconSize;
        iconLE.flexibleWidth = -1f;
        iconLE.flexibleHeight = -1f;
        EditorUtility.SetDirty(iconLE);

        GameObject textGO = UILayoutBuilder.EnsureChild(groupGO.transform, textName);
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(textGO);
        TextMeshProUGUI tmp = UILayoutBuilder.EnsureComponent<TextMeshProUGUI>(textGO);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        Undo.RecordObject(textRT, UILayoutBuilder.UndoLabel);
        textRT.localScale = Vector3.one;
        textRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(textRT);

        Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
        if (string.IsNullOrEmpty(tmp.text)) tmp.text = "0";
        tmp.fontSize = CurrencyHUDStyle.CurrencyFontSize;
        tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(tmp);

        LayoutElement textLE = UILayoutBuilder.EnsureComponent<LayoutElement>(textGO);
        Undo.RecordObject(textLE, UILayoutBuilder.UndoLabel);

        textLE.minWidth = CurrencyHUDStyle.TextMinWidth;
        textLE.preferredWidth = -1f;
        textLE.flexibleWidth = -1f;
        textLE.flexibleHeight = -1f;
        EditorUtility.SetDirty(textLE);

        Transform iconT = groupGO.transform.Find(iconName);
        Transform textT = groupGO.transform.Find(textName);
        if (iconT != null) iconT.SetSiblingIndex(0);
        if (textT != null) textT.SetSiblingIndex(1);

        Debug.Log(string.Format("[CurrencyHUDBuilder.Group] {0} icon={1} text={2} sprite={3}",
            groupName, iconName, textName, sprite != null ? "OK" : "MISSING"));
    }

    private static void AttachHUD(GameObject containerGO)
    {
        CurrencyHUD hud = containerGO.GetComponent<CurrencyHUD>();
        if (hud == null)
        {
            hud = Undo.AddComponent<CurrencyHUD>(containerGO);
            Debug.Log("[CurrencyHUDBuilder.Attach] CurrencyHUD added");
        }
        else
        {
            Debug.Log("[CurrencyHUDBuilder.Attach] CurrencyHUD already present — re-wiring refs");
        }

        WheelController controller = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>());
        Transform cashT = containerGO.transform.Find("ui_group_cash");
        Transform coinT = containerGO.transform.Find("ui_group_coin");
        TMP_Text cashLabel = cashT != null ? cashT.Find("ui_text_cash_value")?.GetComponent<TMP_Text>() : null;
        TMP_Text coinLabel = coinT != null ? coinT.Find("ui_text_coin_value")?.GetComponent<TMP_Text>() : null;

        string[] cfgGuids = AssetDatabase.FindAssets("t:WheelAnimationConfig");
        WheelAnimationConfig animCfg = null;
        if (cfgGuids != null && cfgGuids.Length > 0)
        {
            string cfgPath = AssetDatabase.GUIDToAssetPath(cfgGuids[0]);
            animCfg = AssetDatabase.LoadAssetAtPath<WheelAnimationConfig>(cfgPath);
        }

        UILayoutBuilder.Wire(hud, "controller", controller);
        UILayoutBuilder.Wire(hud, "animConfig", animCfg);
        UILayoutBuilder.Wire(hud, "cashGroup", cashT != null ? cashT.GetComponent<RectTransform>() : null);
        UILayoutBuilder.Wire(hud, "coinGroup", coinT != null ? coinT.GetComponent<RectTransform>() : null);
        UILayoutBuilder.Wire(hud, "cashLabel", cashLabel);
        UILayoutBuilder.Wire(hud, "coinLabel", coinLabel);

        Debug.Log(string.Format(
            "[CurrencyHUDBuilder.Attach] wired ctrl={0} animCfg={1} cashGroup={2} coinGroup={3} cashLabel={4} coinLabel={5}",
            controller != null, animCfg != null, cashT != null, coinT != null, cashLabel != null, coinLabel != null));
    }

    public static void Diagnose()
    {
        Debug.Log("=== [CurrencyHUDBuilder] CURRENCY HUD DIAGNOSTIC ===");

        GameObject container = UILayoutBuilder.FindFirstInScene("ui_group_top_currency");
        if (container == null)
        {
            Debug.LogWarning("[DiagCurrency] ui_group_top_currency NOT FOUND");
            return;
        }
        RectTransform crt = container.GetComponent<RectTransform>();
        Debug.Log(string.Format(
            "[DiagCurrency] ui_group_top_currency active={0} aMin={1} aMax={2} pivot={3} size={4} pos={5}\n  path={6}",
            container.activeInHierarchy, crt.anchorMin, crt.anchorMax, crt.pivot, crt.sizeDelta, crt.anchoredPosition,
            UILayoutBuilder.PathOf(container)));

        DiagnoseChildGroup("ui_group_cash", "ui_image_cash_icon", "ui_text_cash_value");
        DiagnoseChildGroup("ui_group_coin", "ui_image_coin_icon", "ui_text_coin_value");
    }

    private static void DiagnoseChildGroup(string groupName, string iconName, string textName)
    {
        GameObject g = UILayoutBuilder.FindFirstInScene(groupName);
        if (g == null) { Debug.LogWarning("[DiagCurrency] " + groupName + " NOT FOUND"); return; }
        int childCount = g.transform.childCount;

        GameObject icon = UILayoutBuilder.FindFirstInScene(iconName);
        Vector2 iconSize = Vector2.zero;
        if (icon != null) iconSize = icon.GetComponent<RectTransform>().sizeDelta;

        GameObject text = UILayoutBuilder.FindFirstInScene(textName);
        string txtVal = "<missing>";
        float fontSize = 0f;
        Color textColor = Color.white;
        if (text != null)
        {
            TMP_Text tmp = text.GetComponent<TMP_Text>();
            if (tmp != null) { txtVal = tmp.text; fontSize = tmp.fontSize; textColor = tmp.color; }
        }

        Debug.Log(string.Format(
            "[DiagCurrency] {0} childCount={1} icon.size={2} text.value=\"{3}\" text.fontSize={4} text.color=({5:0.00},{6:0.00},{7:0.00})",
            groupName, childCount, iconSize, txtVal, fontSize, textColor.r, textColor.g, textColor.b));
    }
}
