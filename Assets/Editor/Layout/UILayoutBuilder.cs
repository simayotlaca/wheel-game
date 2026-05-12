using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class UILayoutBuilder
{
    internal const string UndoLabel = "Build UI Layout";

    public static void ApplyFinalLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
        {
            Debug.LogError("[UILayoutBuilder] Cannot run while in Play mode. Stop the game first, then re-run Tools → Build UI Layout.");
            return;
        }
        CanvasSetupPass.Apply();
        WheelLayoutPass.Apply();
        RewardPanelLayoutPass.Apply();

        PopupLayoutPass.Apply();

        RaycastTargetAuditor.AuditAndFix();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("=== [UILayoutBuilder] BUILD COMPLETE — running post-build verification ===");
        DiagnoseLayout();
        CurrencyHUDBuilder.Diagnose();
        DiagnoseWheelAndZoneAndSlice();
        UILayoutHelpers.InvalidateSceneIndex();
    }

    public static void DiagnoseLayout() => UILayoutDiagnostics.DiagnoseLayout();

    private static List<GameObject> FindAllInScene(string name) => UILayoutHelpers.FindAllInScene(name);

    internal static string PathOf(GameObject go) => UILayoutHelpers.PathOf(go);

    private static void SetRect(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        => UILayoutHelpers.SetRect(name, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);

    private static void SetAnchorPos(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
        => UILayoutHelpers.SetAnchorPos(name, anchorMin, anchorMax, pivot, anchoredPosition);

    private static void SetGlow(string name, Vector3 localScale, Color32 color)
        => UILayoutHelpers.SetGlow(name, localScale, color);

    public static void Delete(string name) => UILayoutHelpers.Delete(name);

    public static void SetActiveByName(string name, bool active) => UILayoutHelpers.SetActiveByName(name, active);

    public static void SetScale(string name, Vector3 scale) => UILayoutHelpers.SetScale(name, scale);

    public static void SetImageColor(string name, Color32 color) => UILayoutHelpers.SetImageColor(name, color);

    public static void StretchToParent(string name) => UILayoutHelpers.StretchToParent(name);

    public static void SetImageSprite(string name, string spritePath, Image.Type type) => UILayoutHelpers.SetImageSprite(name, spritePath, type);

    public static void SetImageAlpha(string name, byte alpha) => UILayoutHelpers.SetImageAlpha(name, alpha);

    public static void SetTextStyle(string name, float fontSize, Color32 color, TextAlignmentOptions alignment)
        => UILayoutHelpers.SetTextStyle(name, fontSize, color, alignment);

    public static void SetTextStyle(string name, float fontSize, Color32 color, bool bold)
        => UILayoutHelpers.SetTextStyle(name, fontSize, color, bold);

    public static void SetPanelBackground(string name, Color32 color) => UILayoutHelpers.SetPanelBackground(name, color);

    public static void SetOutline(string name, Color32 color, float width) => UILayoutHelpers.SetOutline(name, color, width);

    public static void CreateVerticalLayout(string name, float spacing) => UILayoutHelpers.CreateVerticalLayout(name, spacing);

    internal static GameObject FindFirstInScene(string name) => UILayoutHelpers.FindFirstInScene(name);

    private static void ReparentTo(string childName, string newParentName) => UILayoutHelpers.ReparentTo(childName, newParentName);

    internal static T FirstSceneInstance<T>(T[] all) where T : Component => UILayoutHelpers.FirstSceneInstance<T>(all);

    internal static Transform FindAnywhere(Transform root, string name) => UILayoutHelpers.FindAnywhere(root, name);

    internal static void SetSiblingOrder(Transform parent, string[] names) => UILayoutHelpers.SetSiblingOrder(parent, names);

    private static void StripComponent<T>(string name) where T : Component => UILayoutHelpers.StripComponent<T>(name);

    internal static Transform FindDeep(Transform root, string name) => UILayoutHelpers.FindDeep(root, name);

    internal static T EnsureComponent<T>(GameObject go) where T : Component => UILayoutHelpers.EnsureComponent<T>(go);

    internal static GameObject EnsureChild(Transform parent, string name) => UILayoutHelpers.EnsureChild(parent, name);

    public static void DiagnoseWheelAndZoneAndSlice() => UILayoutDiagnostics.DiagnoseWheelAndZoneAndSlice();

    internal static bool Wire(UnityEngine.Object host, string fieldName, UnityEngine.Object value)
    {
        if (host == null) return false;
        var so = new SerializedObject(host);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[Wire] {host.GetType().Name}.{fieldName} not found"); return false; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
        return true;
    }
}
