using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class PopupLayoutPass
{
    public static void Apply()
    {
        UILayoutHelpers.SetRect("ui_group_zone_progress_bar_indicator",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(1050f, 80f), new Vector2(0f, -90f));

        ZoneBarBuilder.Build();

        MetaProgressBuilder.Build();
        CanvasPromotionPass.Apply();

        HudOverlayDemoterBuilder.Build();
        ExitConfirmBuilder.Build();
        DeathPopupBuilder.Build();
        BlurOverlayBuilder.Build();

        RunExitControllerBuilder.Build();

        GameRuntimeConfigBuilder.Build();
    }

    internal static void StyleBackdrop(Transform t)
    {
        if (t == null) { Debug.LogWarning("[StyleBackdrop] backdrop missing"); return; }
        RectTransform rt = t as RectTransform;
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, UILayoutBuilder.UndoLabel);

            img.color = new Color32(0, 0, 0, 200);
            img.raycastTarget = true;
            EditorUtility.SetDirty(img);
        }
    }
}
