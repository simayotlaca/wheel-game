using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

public static class RaycastTargetAuditor
{

    public static void AuditMenu()
    {
        int choice = EditorUtility.DisplayDialogComplex(
            "Audit RaycastTargets",
            "Sweep every Image / TMP_Text in the scene + RewardListItem prefab " +
            "for raycastTarget / maskable flags that aren't doing any work.\n\n" +
            "• Just check — log what would change, don't touch anything.\n" +
            "• Auto-fix — log AND turn off the unnecessary flags.",
            "Just check",
            "Cancel",
            "Auto-fix");
        if (choice == 0)      Run(autoFix: false);
        else if (choice == 2) Run(autoFix: true);

    }

    public static void Audit() => Run(autoFix: false);
    public static void AuditAndFix() => Run(autoFix: true);

    static void Run(bool autoFix)
    {
        Debug.Log("=== [RaycastTargetAuditor] START " + (autoFix ? "(auto-fix mode)" : "(read-only)") + " ===");

        int flagged = 0, fixedCount = 0;

        flagged += AuditScene(autoFix, ref fixedCount);

        flagged += AuditPrefab("Assets/Prefabs/RewardListItem.prefab", autoFix, ref fixedCount, assumesRuntimeMask: true);

        Debug.Log(string.Format(
            "=== [RaycastTargetAuditor] DONE — flagged={0} fixed={1} ===",
            flagged, fixedCount));
    }

    static int AuditScene(bool autoFix, ref int fixedCount)
    {
        int flagged = 0;
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            flagged += ScanHierarchy(roots[i].transform, autoFix, ref fixedCount, "[scene] ");
        return flagged;
    }

    static int AuditPrefab(string path, bool autoFix, ref int fixedCount, bool assumesRuntimeMask = false)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning("[RaycastTargetAuditor] prefab not found: " + path); return 0; }
        int flagged = 0;
        try
        {
            flagged = ScanHierarchy(root.transform, autoFix, ref fixedCount, "[prefab " + path + "] ", assumesRuntimeMask);
            if (autoFix) PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return flagged;
    }

    static int ScanHierarchy(Transform t, bool autoFix, ref int fixedCount, string prefix, bool assumesRuntimeMask = false)
    {
        int flagged = 0;

        Image img = t.GetComponent<Image>();
        if (img != null && img.raycastTarget && !IsClickable(t.gameObject) && !IsModalBackdrop(t))
        {
            if (!autoFix) Debug.LogWarning(prefix + "Image with raycastTarget=true but no clickable handler: " + Path(t));
            flagged++;
            if (autoFix) { Undo.RecordObject(img, "Audit RaycastTarget"); img.raycastTarget = false; EditorUtility.SetDirty(img); fixedCount++; }
        }

        if (img != null && img.maskable && !assumesRuntimeMask && !IsUnderMask(t))
        {
            if (!autoFix) Debug.LogWarning(prefix + "Image with maskable=true but no Mask/RectMask2D ancestor: " + Path(t));
            flagged++;
            if (autoFix) { Undo.RecordObject(img, "Audit Maskable"); img.maskable = false; EditorUtility.SetDirty(img); fixedCount++; }
        }

        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null && tmp.raycastTarget && !IsClickable(t.gameObject))
        {
            if (!autoFix) Debug.LogWarning(prefix + "TMP_Text with raycastTarget=true but no clickable handler: " + Path(t));
            flagged++;
            if (autoFix) { Undo.RecordObject(tmp, "Audit RaycastTarget"); tmp.raycastTarget = false; EditorUtility.SetDirty(tmp); fixedCount++; }
        }

        if (tmp != null && tmp.maskable && !assumesRuntimeMask && !IsUnderMask(t))
        {
            if (!autoFix) Debug.LogWarning(prefix + "TMP_Text with maskable=true but no Mask/RectMask2D ancestor: " + Path(t));
            flagged++;
            if (autoFix) { Undo.RecordObject(tmp, "Audit Maskable"); tmp.maskable = false; EditorUtility.SetDirty(tmp); fixedCount++; }
        }

        for (int i = 0; i < t.childCount; i++)
            flagged += ScanHierarchy(t.GetChild(i), autoFix, ref fixedCount, prefix, assumesRuntimeMask);

        return flagged;
    }

    static bool IsUnderMask(Transform t)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.GetComponent<Mask>() != null) return true;
            if (p.GetComponent<RectMask2D>() != null) return true;
            p = p.parent;
        }
        return false;
    }

    static bool IsModalBackdrop(Transform t)
    {
        string n = t.name.ToLowerInvariant();
        if (n.Contains("backdrop")) return true;

        if (n.Contains("_blur")) return true;

        if (n.EndsWith("_panel") && t.parent != null && t.parent.name.ToLowerInvariant().Contains("exit")) return true;
        return false;
    }

    static bool IsClickable(GameObject go)
    {
        if (go.GetComponent<Selectable>() != null) return true;
        Component[] all = go.GetComponents<Component>();
        for (int i = 0; i < all.Length; i++)
        {
            Component c = all[i];
            if (c == null) continue;
            if (c is IPointerClickHandler) return true;
            if (c is IPointerDownHandler)  return true;
            if (c is IPointerUpHandler)    return true;
            if (c is IPointerEnterHandler) return true;
            if (c is IPointerExitHandler)  return true;
            if (c is IDragHandler)         return true;
        }
        return false;
    }

    static string Path(Transform t)
    {
        if (t == null) return "<null>";
        var sb = new System.Text.StringBuilder(t.name);
        Transform p = t.parent;
        while (p != null) { sb.Insert(0, "/"); sb.Insert(0, p.name); p = p.parent; }
        return sb.ToString();
    }
}
