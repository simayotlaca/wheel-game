using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class RewardPanelLayoutPass
{
    public static void Apply()
    {
        UILayoutHelpers.SetRect("ui_group_reward_list",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(300f, 800f), new Vector2(70f, -40f));

        UILayoutHelpers.Delete("ui_label_reward_list_header");
        UILayoutHelpers.Delete("ui_label_spin_header_value");
        UILayoutHelpers.Delete("ui_group_top_hud");
        UILayoutHelpers.Delete("ui_button_leave");
        UILayoutHelpers.Delete("ui_label_wheel_subtitle_value");

        RewardPanelBuilder.Build();

        ApplyRewardPanelBgGradient();
        RestyleExitPill();
        RewardPanelBuilder.RestyleItemPrefab();
        CurrencyHUDBuilder.Build();

        RewardPanelBuilder.WireSpinFlyAnimator();
        RewardPanelBuilder.WireCollectAnimator();
        StripRewardBurstAnimator();
    }

    private static void StripRewardBurstAnimator()
    {
        GameObject panelGO = UILayoutHelpers.FindFirstInScene("ui_group_reward_list");
        if (panelGO == null) return;
        int removed = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(panelGO);
        if (removed > 0)
            Debug.Log("[StripRewardBurstAnimator] removed " + removed + " missing-script component(s) from ui_group_reward_list");
    }

    private static void ApplyRewardPanelBgGradient()
    {
        GameObject bgGO = UILayoutHelpers.FindFirstInScene("ui_image_reward_list_bg");
        if (bgGO == null) { Debug.LogWarning("[ApplyRewardPanelBgGradient] bg not found"); return; }

        UIVerticalGradient stale = bgGO.GetComponent<UIVerticalGradient>();
        if (stale != null) Undo.DestroyObjectImmediate(stale);

        Image img = bgGO.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, UILayoutBuilder.UndoLabel);

            img.color = new Color32(0xC0, 0xC4, 0xCE, 255);
            EditorUtility.SetDirty(img);
        }
    }

    private static void RestyleExitPill()
    {
        GameObject pillGO = UILayoutHelpers.FindFirstInScene("ui_panel_reward_list_exit_header");
        if (pillGO == null) { Debug.LogWarning("[RestyleExitPill] EXIT pill not found"); return; }

        RectTransform rt = pillGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(272f, 62f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            EditorUtility.SetDirty(rt);
        }

        Image bg = pillGO.GetComponent<Image>();
        Sprite pillSprite = bg != null ? bg.sprite : null;
        if (bg != null)
        {
            Undo.RecordObject(bg, UILayoutBuilder.UndoLabel);
            bg.color = Color.white;
            EditorUtility.SetDirty(bg);
        }
        Button btn = pillGO.GetComponent<Button>();
        if (btn != null)
        {
            Undo.RecordObject(btn, UILayoutBuilder.UndoLabel);

            btn.transition = Selectable.Transition.None;
            SpriteState ss = btn.spriteState;
            ss.disabledSprite = null;
            ss.pressedSprite = null;
            ss.highlightedSprite = null;
            ss.selectedSprite = null;
            btn.spriteState = ss;
            EditorUtility.SetDirty(btn);
        }

        Transform staleVeil = pillGO.transform.Find("ui_image_reward_list_exit_disabled_overlay");
        if (staleVeil != null) Undo.DestroyObjectImmediate(staleVeil.gameObject);

        CanvasGroup cg = UILayoutHelpers.EnsureComponent<CanvasGroup>(pillGO);
        Undo.RecordObject(cg, UILayoutBuilder.UndoLabel);
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        EditorUtility.SetDirty(cg);

        GameObject topHL = UILayoutHelpers.EnsureChild(pillGO.transform, "ui_image_reward_list_exit_top_hl");
        UILayoutHelpers.EnsureComponent<CanvasRenderer>(topHL);
        RectTransform topRT = UILayoutHelpers.EnsureComponent<RectTransform>(topHL);
        Undo.RecordObject(topRT, UILayoutBuilder.UndoLabel);
        topRT.anchorMin = new Vector2(0f, 0.6f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.offsetMin = Vector2.zero;
        topRT.offsetMax = Vector2.zero;
        topRT.localScale = Vector3.one;
        topRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(topRT);
        Image topImg = UILayoutHelpers.EnsureComponent<Image>(topHL);
        Undo.RecordObject(topImg, UILayoutBuilder.UndoLabel);
        if (pillSprite != null) { topImg.sprite = pillSprite; topImg.type = Image.Type.Sliced; }
        topImg.color = new Color32(255, 255, 255, 70);
        topImg.raycastTarget = false;
        EditorUtility.SetDirty(topImg);

        GameObject botSh = UILayoutHelpers.EnsureChild(pillGO.transform, "ui_image_reward_list_exit_bot_shadow");
        UILayoutHelpers.EnsureComponent<CanvasRenderer>(botSh);
        RectTransform botRT = UILayoutHelpers.EnsureComponent<RectTransform>(botSh);
        Undo.RecordObject(botRT, UILayoutBuilder.UndoLabel);
        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0.5f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.offsetMin = Vector2.zero;
        botRT.offsetMax = Vector2.zero;
        botRT.localScale = Vector3.one;
        botRT.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(botRT);
        Image botImg = UILayoutHelpers.EnsureComponent<Image>(botSh);
        Undo.RecordObject(botImg, UILayoutBuilder.UndoLabel);
        if (pillSprite != null) { botImg.sprite = pillSprite; botImg.type = Image.Type.Sliced; }
        botImg.color = new Color32(0, 0, 0, 90);
        botImg.raycastTarget = false;
        EditorUtility.SetDirty(botImg);

        Transform label = pillGO.transform.Find("ui_label_reward_list_exit_text");
        if (label != null)
        {
            label.SetAsLastSibling();
            TMP_Text tmp = label.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
                tmp.color = Color.white;

                Material m = tmp.fontSharedMaterial;
                if (m != null)
                {
                    if (m.HasProperty("_FaceDilate")) m.SetFloat("_FaceDilate", 0f);
                    if (m.HasProperty("_GlowPower")) m.SetFloat("_GlowPower", 0f);
                    if (m.HasProperty("_UnderlaySoftness")) m.SetFloat("_UnderlaySoftness", 0.6f);
                    if (m.HasProperty("_UnderlayDilate")) m.SetFloat("_UnderlayDilate", 0f);
                }
                EditorUtility.SetDirty(tmp);
            }
        }
    }
}
