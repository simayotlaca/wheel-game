using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal static class BlurOverlayBuilder
{
    private const string GameObjectName = "ui_image_blur_backdrop";

    public static void Build()
    {
        GameObject overlay = UILayoutBuilder.FindFirstInScene("ui_canvas_overlay");
        if (overlay == null)
        {
            Debug.LogWarning("[BlurOverlayBuilder] ui_canvas_overlay not found — run Build UI Layout first.");
            return;
        }

        GameObject go = UILayoutBuilder.EnsureChild(overlay.transform, GameObjectName);

        go.transform.SetSiblingIndex(0);

        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);

        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);

        Image img = UILayoutBuilder.EnsureComponent<Image>(go);
        Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
        img.sprite = null;
        img.color = new Color32(0x05, 0x07, 0x0B, 230);

        img.raycastTarget = true;
        EditorUtility.SetDirty(img);

        CanvasGroup cg = UILayoutBuilder.EnsureComponent<CanvasGroup>(go);
        Undo.RecordObject(cg, UILayoutBuilder.UndoLabel);
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        EditorUtility.SetDirty(cg);

        BlurBackgroundOverlay overlayScript = UILayoutBuilder.EnsureComponent<BlurBackgroundOverlay>(go);

        RunExitController controller = UILayoutBuilder.FirstSceneInstance(
            Resources.FindObjectsOfTypeAll<RunExitController>());

        UILayoutBuilder.Wire(overlayScript, "exitController", controller);
        UILayoutBuilder.Wire(overlayScript, "blurGroup", cg);
    }
}
