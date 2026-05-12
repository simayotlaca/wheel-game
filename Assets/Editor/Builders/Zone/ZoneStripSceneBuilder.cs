#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

internal static class ZoneStripSceneBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/HUD/ui_zone_bar_design1.prefab";

    public static void Build(GameObject staticCanvasGO)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[ZoneStripSceneBuilder] prefab not found at " + PrefabPath
                + " — run Vertigo/Migrate/Bake ui_zone_bar_design1 first.");
            return;
        }

        Transform parent = staticCanvasGO.transform;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c != null && c.name == "ui_group_zone_progress_bar_indicator")
            {
                Object.DestroyImmediate(c.gameObject);
                break;
            }
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, UILayoutBuilder.UndoLabel);
        instance.name = "ui_group_zone_progress_bar_indicator";
    }
}
#endif
