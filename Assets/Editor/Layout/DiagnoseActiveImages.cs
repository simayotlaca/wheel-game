#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal static class DiagnoseActiveImages
{

    private static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DiagnoseActiveImages] Enter Play mode first — most diagnostic value comes from runtime state.");
        }

        Image[] all = Object.FindObjectsOfType<Image>(includeInactive: true);
        if (all == null || all.Length == 0)
        {
            Debug.Log("[DiagnoseActiveImages] No active Image components found.");
            return;
        }

        System.Array.Sort(all, (a, b) =>
        {
            int sa = ResolveSortingOrder(a);
            int sb = ResolveSortingOrder(b);
            return sa.CompareTo(sb);
        });

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[DiagnoseActiveImages] {all.Length} active Image(s) — sorted by sortingOrder ascending:");
        for (int i = 0; i < all.Length; i++)
        {
            Image img = all[i];
            if (img == null) continue;
            RectTransform rt = img.rectTransform;
            int order = ResolveSortingOrder(img);
            string path = HierarchyPath(img.transform);
            string spriteName = img.sprite != null ? img.sprite.name : "<none>";
            Vector2 size = rt != null ? rt.sizeDelta : Vector2.zero;
            Vector3 pos = rt != null ? rt.position : Vector3.zero;
            Vector3 scl = rt != null ? rt.lossyScale : Vector3.one;
            string state = img.gameObject.activeInHierarchy ? "ACTIVE" : "inactive";
            sb.AppendFormat(
                "  [order={0,3}] [{1}] {2}\n    sprite='{3}'  size=({4:0},{5:0})  pos=({6:0},{7:0})  lossyScale=({8:0.00},{9:0.00})  color=#{10:X2}{11:X2}{12:X2}{13:X2}\n",
                order, state, path, spriteName, size.x, size.y, pos.x, pos.y, scl.x, scl.y,
                (byte)(img.color.r * 255), (byte)(img.color.g * 255), (byte)(img.color.b * 255), (byte)(img.color.a * 255));
        }
        Debug.Log(sb.ToString());

        string outPath = System.IO.Path.Combine(Application.dataPath, "..", "DiagnoseActiveImages.log");
        try
        {
            System.IO.File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[DiagnoseActiveImages] Full log written to: {System.IO.Path.GetFullPath(outPath)}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DiagnoseActiveImages] Failed to write log file: {e.Message}");
        }
    }

    private static int ResolveSortingOrder(Image img)
    {
        if (img == null) return 0;
        Canvas c = img.canvas;
        return c != null ? c.sortingOrder : 0;
    }

    private static string HierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        StringBuilder sb = new StringBuilder();
        sb.Append(t.name);
        Transform p = t.parent;
        while (p != null)
        {
            sb.Insert(0, p.name + "/");
            p = p.parent;
        }
        return sb.ToString();
    }
}
#endif
