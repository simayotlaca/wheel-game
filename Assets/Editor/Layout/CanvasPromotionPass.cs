using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class CanvasPromotionPass
{

    public static void Apply()
    {
        PromoteCanvas("ui_group_reward_list");
        PromoteCanvas("ui_group_top_currency");

        PromoteCanvas("ui_group_zone_progress_bar_indicator");

        PromoteCanvas("ui_group_meta_progress");
        PromoteFlyHostCanvas();
    }

    private static void PromoteFlyHostCanvas()
    {
        GameObject go = UILayoutHelpers.FindFirstInScene("ui_canvas_static");
        if (go == null) { Debug.LogWarning("[PromoteFlyHostCanvas] ui_canvas_static not found"); return; }
        Canvas canvas = UILayoutHelpers.EnsureComponent<Canvas>(go);
        Undo.RecordObject(canvas, UILayoutBuilder.UndoLabel);
        canvas.overrideSorting = true;
        canvas.sortingOrder = UICanvasOrders.FlyHost;
        EditorUtility.SetDirty(canvas);
        UILayoutHelpers.EnsureComponent<GraphicRaycaster>(go);
        Debug.Log("[PromoteFlyHostCanvas] ui_canvas_static sortingOrder → " + UICanvasOrders.FlyHost);
    }

    private static void PromoteCanvas(string name)
    {
        GameObject go = UILayoutHelpers.FindFirstInScene(name);
        if (go == null)
        {
            Debug.LogWarning("[PromoteHUDCanvases] " + name + " not found");
            return;
        }

        Canvas canvas = UILayoutHelpers.EnsureComponent<Canvas>(go);
        Undo.RecordObject(canvas, UILayoutBuilder.UndoLabel);
        canvas.overrideSorting = true;
        canvas.sortingOrder = UICanvasOrders.HUDPromoted;
        EditorUtility.SetDirty(canvas);

        GraphicRaycaster raycaster = UILayoutHelpers.EnsureComponent<GraphicRaycaster>(go);
        Undo.RecordObject(raycaster, UILayoutBuilder.UndoLabel);
        EditorUtility.SetDirty(raycaster);

        Debug.Log("[PromoteHUDCanvases] " + name + " sortingOrder → " + UICanvasOrders.HUDPromoted);
    }
}
