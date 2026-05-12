using UnityEngine;
using UnityEditor;

internal static class DeathPopupBuilder
{

    private const int OverlaySortingOrder = 10;

    public static void Build()
    {
        GameObject death_panel = UILayoutBuilder.FindFirstInScene("ui_panel_death_gameover");
        if (death_panel == null)
        {
            Debug.LogWarning("[DeathPopupBuilder] ui_panel_death_gameover not found, prefab missing? Run Vertigo/Migrate/Bake ui_death_scene_design1.");
            return;
        }

        Canvas overlay_canvas = death_panel.GetComponentInParent<Canvas>(true);
        if (overlay_canvas != null)
        {
            Undo.RecordObject(overlay_canvas, UILayoutBuilder.UndoLabel);
            overlay_canvas.overrideSorting = true;
            overlay_canvas.sortingOrder = OverlaySortingOrder;
            EditorUtility.SetDirty(overlay_canvas);
        }

        WireSceneRefs(death_panel);
    }

    private static void WireSceneRefs(GameObject death_panel)
    {
        DeathGameOverPanel script = death_panel.GetComponent<DeathGameOverPanel>();
        if (script == null) { Debug.LogWarning("[DeathPopupBuilder] DeathGameOverPanel missing, cannot wire scene refs"); return; }

        GameObject reward_panel_go = UILayoutBuilder.FindFirstInScene("ui_group_reward_list");
        UnityEngine.CanvasGroup reward_group = reward_panel_go != null
            ? UILayoutBuilder.EnsureComponent<UnityEngine.CanvasGroup>(reward_panel_go)
            : null;

        HudOverlayDemoter demoter = HudOverlayDemoterBuilder.FindInScene();
        WheelController controller = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>());

        UILayoutBuilder.Wire(script, "controller", controller);
        UILayoutBuilder.Wire(script, "rewardPanelGroup", reward_group);
        UILayoutBuilder.Wire(script, "hudDemoter", demoter);
    }
}
