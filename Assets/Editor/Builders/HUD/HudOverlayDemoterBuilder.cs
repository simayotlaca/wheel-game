using UnityEditor;
using UnityEngine;

internal static class HudOverlayDemoterBuilder
{
    const string GOName = "ui_runtime_hud_demoter";

    public static void Build()
    {

        var existing = Object.FindObjectsOfType<HudOverlayDemoter>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null) Undo.DestroyObjectImmediate(existing[i].gameObject);
        }

        GameObject go = new GameObject(GOName);
        Undo.RegisterCreatedObjectUndo(go, UILayoutBuilder.UndoLabel);

        HudOverlayDemoter demoter = UILayoutHelpers.EnsureComponent<HudOverlayDemoter>(go);

        Canvas reward = ResolveCanvas("ui_group_reward_list");
        Canvas zone   = ResolveCanvas("ui_group_zone_progress_bar_indicator");
        Canvas meta   = ResolveCanvas("ui_group_meta_progress");

        CanvasGroup metaGroup = meta != null
            ? UILayoutHelpers.EnsureComponent<CanvasGroup>(meta.gameObject)
            : null;

        UILayoutBuilder.Wire(demoter, "rewardListCanvas", reward);
        UILayoutBuilder.Wire(demoter, "zoneBarCanvas", zone);
        UILayoutBuilder.Wire(demoter, "metaProgressCanvas", meta);
        UILayoutBuilder.Wire(demoter, "metaProgressGroup", metaGroup);
    }

    public static HudOverlayDemoter FindInScene()
    {
        return Object.FindObjectOfType<HudOverlayDemoter>(true);
    }

    private static Canvas ResolveCanvas(string name)
    {
        GameObject go = UILayoutHelpers.FindFirstInScene(name);
        return go != null ? go.GetComponent<Canvas>() : null;
    }
}
