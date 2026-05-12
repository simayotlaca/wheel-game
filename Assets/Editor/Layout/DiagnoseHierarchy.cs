#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal static class DiagnoseHierarchy
{
    private const string DefaultRootName = "WheelCanvas";

    private static void Run()
    {
        GameObject root = GameObject.Find(DefaultRootName);
        if (root == null)
        {
            Debug.LogError("[DiagnoseHierarchy] Root '" + DefaultRootName
                + "' not found in active scene. Open the wheel scene first.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("=== Hierarchy Report: ").Append(root.name)
          .Append(" (").Append(System.DateTime.Now.ToString("HH:mm:ss")).AppendLine(") ===");

        int totalNodes = 0;
        int totalWarnings = 0;
        WalkNode(root.transform, depth: 0, sb, ref totalNodes, ref totalWarnings);

        sb.Append("=== ").Append(totalNodes).Append(" nodes, ")
          .Append(totalWarnings).AppendLine(" warnings ===");

        string report = sb.ToString();
        Debug.Log(report);
        EditorGUIUtility.systemCopyBuffer = report;
        Debug.Log("[DiagnoseHierarchy] Report copied to clipboard.");
    }

    private static void WalkNode(Transform t, int depth, StringBuilder sb,
                                 ref int totalNodes, ref int totalWarnings)
    {
        totalNodes++;

        sb.Append(GetIndent(depth, t));
        sb.Append(t.gameObject.name);

        int warnings = 0;

        if (!t.gameObject.activeSelf)
        {
            sb.Append("  ⚠INACTIVE");
            warnings++;
        }

        RectTransform rt = t as RectTransform;
        if (rt == null && t.GetComponent<Canvas>() == null && depth > 0)
        {
            sb.Append("  ⚠NO-RECT");
            warnings++;
        }

        Canvas canvas = t.GetComponent<Canvas>();
        if (canvas != null)
        {
            sb.Append("  [Canvas] sortingOrder=").Append(canvas.sortingOrder);
            if (canvas.overrideSorting) sb.Append(" override");
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            sb.Append("  [Image]");
            sb.Append(" sprite=").Append(img.sprite != null ? img.sprite.name : "<NULL>");
            sb.Append(" a=").Append(img.color.a.ToString("0.00"));
            if (img.material != null && img.material.name != "Default UI Material")
            {
                sb.Append(" mat=").Append(img.material.name);
            }
            if (img.sprite == null)
            {
                sb.Append("  ⚠NULL-SPRITE");
                warnings++;
            }
            if (img.color.a < 0.05f)
            {
                sb.Append("  ⚠ALPHA-ZERO");
                warnings++;
            }
        }

        if (rt != null)
        {
            sb.Append("  anchor=").Append(VecPair(rt.anchorMin, rt.anchorMax));
            sb.Append(" pivot=").Append(Vec(rt.pivot));
            sb.Append(" size=").Append(Vec(rt.sizeDelta));
            sb.Append(" pos=").Append(Vec(rt.anchoredPosition));
            if (Mathf.Abs(rt.anchoredPosition.x) > 5000f
                || Mathf.Abs(rt.anchoredPosition.y) > 5000f)
            {
                sb.Append("  ⚠OFFSCREEN");
                warnings++;
            }
            float zRot = rt.localEulerAngles.z;
            if (zRot != 0f) sb.Append(" rot=").Append(zRot.ToString("0"));
            if (rt.localScale.x < 0f) sb.Append(" flipX");
        }

        sb.AppendLine();
        totalWarnings += warnings;

        for (int i = 0; i < t.childCount; i++)
            WalkNode(t.GetChild(i), depth + 1, sb, ref totalNodes, ref totalWarnings);
    }

    private static string GetIndent(int depth, Transform t)
    {
        if (depth == 0) return "[0] ";
        StringBuilder ind = new StringBuilder(depth * 2 + 6);
        bool isLast = t.parent != null
            && t.GetSiblingIndex() == t.parent.childCount - 1;
        ind.Append(isLast ? "└─" : "├─");
        ind.Append("[").Append(t.GetSiblingIndex()).Append("] ");
        return ind.ToString();
    }

    private static string Vec(Vector2 v)
    {
        return "(" + v.x.ToString("0.#") + "," + v.y.ToString("0.#") + ")";
    }

    private static string VecPair(Vector2 a, Vector2 b)
    {
        if (a == b) return Vec(a);
        return Vec(a) + ".." + Vec(b);
    }
}
#endif
