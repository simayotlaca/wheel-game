#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class DeathPopupSceneBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/Popups/ui_death_scene_design1.prefab";

    internal struct Refs
    {
        public Button giveUp;
        public Button revive;
    }

    public static Refs Build(GameObject overlayCanvasGO, WheelController controller)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[DeathPopupSceneBuilder] prefab not found at " + PrefabPath
                + ", run Vertigo/Migrate/Bake ui_death_scene_design1 first.");
            return default;
        }

        Transform parent = overlayCanvasGO.transform;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c != null && c.name == "ui_panel_death_gameover")
            {
                Object.DestroyImmediate(c.gameObject);
                break;
            }
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, UILayoutBuilder.UndoLabel);
        instance.name = "ui_panel_death_gameover";

        Transform give_up_transform = UILayoutBuilder.FindAnywhere(instance.transform, "ui_button_giveup");
        Transform revive_transform = UILayoutBuilder.FindAnywhere(instance.transform, "ui_button_revive");

        return new Refs
        {
            giveUp = give_up_transform != null ? give_up_transform.GetComponent<Button>() : null,
            revive = revive_transform != null ? revive_transform.GetComponent<Button>() : null,
        };
    }
}
#endif
