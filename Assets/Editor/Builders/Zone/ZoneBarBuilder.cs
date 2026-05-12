using UnityEngine;
using UnityEditor;
using Wheel.UI.Zone;

internal static class ZoneBarBuilder
{
    public static void Build()
    {
        GameObject indicator = UILayoutBuilder.FindFirstInScene("ui_group_zone_progress_bar_indicator");
        if (indicator == null)
        {
            Debug.LogWarning("[ZoneBarBuilder] ui_group_zone_progress_bar_indicator not found — prefab missing? Run Vertigo/Migrate/Bake ui_zone_bar_design1 to regenerate.");
            return;
        }

        ZoneIndicatorUI zone = indicator.GetComponent<ZoneIndicatorUI>();
        WheelController ctrl = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>());
        if (zone != null) UILayoutBuilder.Wire(zone, "controller", ctrl);
    }
}
