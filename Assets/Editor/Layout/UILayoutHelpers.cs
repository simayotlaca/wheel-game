using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class UILayoutHelpers
{

    private static Dictionary<string, List<GameObject>> sceneIndex;

    public static void InvalidateSceneIndex() => sceneIndex = null;

    private static void EnsureSceneIndex()
    {
        if (sceneIndex != null) return;
        sceneIndex = new Dictionary<string, List<GameObject>>();
        var rects = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < rects.Length; i++) AddToIndex(rects[i].gameObject);
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++) AddToIndex(canvases[i].gameObject);
    }

    private static void AddToIndex(GameObject go)
    {
        if (!go.scene.IsValid()) return;
        if (EditorUtility.IsPersistent(go)) return;
        if (!sceneIndex.TryGetValue(go.name, out var list))
        {
            list = new List<GameObject>();
            sceneIndex[go.name] = list;
        }
        if (!list.Contains(go)) list.Add(go);
    }

    public static List<GameObject> FindAllInScene(string name)
    {
        EnsureSceneIndex();
        var result = new List<GameObject>();
        if (!sceneIndex.TryGetValue(name, out var cached)) return result;
        for (int i = 0; i < cached.Count; i++)
        {
            GameObject go = cached[i];
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            result.Add(go);
        }
        return result;
    }

    public static GameObject FindFirstInScene(string name)
    {
        var list = FindAllInScene(name);
        return list.Count > 0 ? list[0] : null;
    }

    public static string PathOf(GameObject go)
    {
        var sb = new System.Text.StringBuilder(go.name);
        Transform t = go.transform.parent;
        while (t != null) { sb.Insert(0, t.name + "/"); t = t.parent; }
        return sb.ToString();
    }

    public static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    public static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = Undo.AddComponent<T>(go);
        return comp;
    }

    public static GameObject EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UILayoutBuilder.UndoLabel);
        go.transform.SetParent(parent, false);
        InvalidateSceneIndex();
        return go;
    }

    public static void Delete(string name)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.Log("[Delete] '" + name + "' not present — skipping (already cleaned or never existed)"); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            string path = PathOf(go);
            int childCount = go.transform.childCount;
            Undo.DestroyObjectImmediate(go);
            Debug.Log(string.Format("[Delete] {0} removed (had {1} children)\n  path={2}", name, childCount, path));
        }
        InvalidateSceneIndex();
    }

    public static void SetRect(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        if (list.Count > 1) Debug.LogWarning("[UILayoutBuilder] Multiple matches (" + list.Count + ") for: " + name + " — applying to all");
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) { Debug.LogWarning("[UILayoutBuilder] No RectTransform on: " + name); continue; }
            string before = string.Format("size={0} pos={1} pivot={2} aMin={3} aMax={4}",
                rt.sizeDelta, rt.anchoredPosition, rt.pivot, rt.anchorMin, rt.anchorMax);
            Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            EditorUtility.SetDirty(rt);
            Debug.Log(string.Format("[SetRect] {0} [{1}/{2}] active={3}\n  BEFORE: {4}\n  AFTER:  size={5} pos={6} pivot={7} aMin={8} aMax={9}\n  path={10}",
                name, i + 1, list.Count, go.activeInHierarchy, before,
                rt.sizeDelta, rt.anchoredPosition, rt.pivot, rt.anchorMin, rt.anchorMax,
                PathOf(go)));
        }
    }

    public static void SetImageColor(string name, Color32 color)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            Image img = go.GetComponent<Image>();
            if (img == null) { Debug.LogWarning("[UILayoutBuilder] No Image on: " + name); continue; }
            Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
            img.color = color;
            EditorUtility.SetDirty(img);
            Debug.Log("[SetImageColor] " + name + " color=" + (Color)color + " alpha=" + color.a + "  path=" + PathOf(go));
        }
    }

    public static void SetTextStyle(string name, float fontSize, Color32 color, TextAlignmentOptions alignment)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) { Debug.LogWarning("[UILayoutBuilder] No TMP_Text on: " + name); continue; }
            Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            EditorUtility.SetDirty(tmp);
            Debug.Log(string.Format("[SetTextStyle] {0} fontSize={1} color={2} align={3}  path={4}",
                name, fontSize, (Color)color, alignment, PathOf(go)));
        }
    }

    public static void SetTextStyle(string name, float fontSize, Color32 color, bool bold)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) { Debug.LogWarning("[UILayoutBuilder] No TMP_Text on: " + name); continue; }
            Undo.RecordObject(tmp, UILayoutBuilder.UndoLabel);
            tmp.fontSize = fontSize;
            tmp.color = color;
            if (bold) tmp.fontStyle |= FontStyles.Bold;
            else tmp.fontStyle &= ~FontStyles.Bold;
            EditorUtility.SetDirty(tmp);
            Debug.Log(string.Format("[SetTextStyle] {0} fontSize={1} color={2} bold={3}  path={4}",
                name, fontSize, (Color)color, bold, PathOf(go)));
        }
    }

    public static void SetAnchorPos(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        if (list.Count > 1) Debug.LogWarning("[UILayoutBuilder] Multiple matches (" + list.Count + ") for: " + name + " — applying to all");
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) { Debug.LogWarning("[UILayoutBuilder] No RectTransform on: " + name); continue; }
            Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            EditorUtility.SetDirty(rt);
            Debug.Log(string.Format("[SetAnchorPos] {0} [{1}/{2}] active={3} pos={4} pivot={5}\n  path={6}",
                name, i + 1, list.Count, go.activeInHierarchy,
                rt.anchoredPosition, rt.pivot, PathOf(go)));
        }
    }

    public static void SetGlow(string name, Vector3 localScale, Color32 color)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            Transform tr = go.transform;
            Undo.RecordObject(tr, UILayoutBuilder.UndoLabel);
            tr.localScale = localScale;
            EditorUtility.SetDirty(tr);
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
                img.color = color;
                EditorUtility.SetDirty(img);
            }
            Debug.Log(string.Format("[SetGlow] {0} [{1}/{2}] scale={3} hasImage={4}\n  path={5}",
                name, i + 1, list.Count, tr.localScale, img != null, PathOf(go)));
        }
    }

    public static void SetActiveByName(string name, bool active)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            bool wasActive = go.activeSelf;
            Undo.RecordObject(go, UILayoutBuilder.UndoLabel);
            go.SetActive(active);
            EditorUtility.SetDirty(go);
            Debug.Log(string.Format("[SetActive] {0} [{1}/{2}] wasActive={3} nowActive={4}\n  path={5}",
                name, i + 1, list.Count, wasActive, go.activeSelf, PathOf(go)));
        }
    }

    public static void SetScale(string name, Vector3 scale)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            Undo.RecordObject(go.transform, UILayoutBuilder.UndoLabel);
            go.transform.localScale = scale;
            EditorUtility.SetDirty(go.transform);
            Debug.Log("[SetScale] " + name + " scale=" + scale + "  path=" + PathOf(go));
        }
    }

    public static void StretchToParent(string name)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[StretchToParent] not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            RectTransform rt = list[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rt);
        }
    }

    public static void SetImageSprite(string name, string spritePath, Image.Type type)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) { Debug.LogWarning("[SetImageSprite] sprite not found: " + spritePath); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            Image img = go.GetComponent<Image>();
            if (img == null) { Debug.LogWarning("[SetImageSprite] No Image on: " + name); continue; }
            Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
            img.sprite = sprite;
            img.type = type;
            EditorUtility.SetDirty(img);
            Debug.Log("[SetImageSprite] " + name + " sprite=" + spritePath + " type=" + type);
        }
    }

    public static void SetImageAlpha(string name, byte alpha)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            Image img = go.GetComponent<Image>();
            if (img == null) { Debug.LogWarning("[UILayoutBuilder] No Image on: " + name); continue; }
            Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
            Color c = img.color;
            c.a = alpha / 255f;
            img.color = c;
            EditorUtility.SetDirty(img);
            Debug.Log("[SetImageAlpha] " + name + " alpha=" + alpha + "/255  path=" + PathOf(go));
        }
    }

    public static void SetPanelBackground(string name, Color32 color) => SetImageColor(name, color);

    public static void SetOutline(string name, Color32 color, float width)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go.GetComponent<Graphic>() == null)
            {
                Debug.LogWarning("[UILayoutBuilder] '" + name + "' has no Graphic — Outline must attach to a Graphic-bearing GameObject");
                continue;
            }
            Outline outline = go.GetComponent<Outline>();
            if (outline == null) outline = Undo.AddComponent<Outline>(go);
            Undo.RecordObject(outline, UILayoutBuilder.UndoLabel);
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
            EditorUtility.SetDirty(outline);
            Debug.Log(string.Format("[SetOutline] {0} color=({1},{2},{3},{4}) width={5}",
                name, color.r, color.g, color.b, color.a, width));
        }
    }

    public static void CreateVerticalLayout(string name, float spacing)
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            bool created = vlg == null;
            if (created) vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
            Undo.RecordObject(vlg, UILayoutBuilder.UndoLabel);
            vlg.spacing = spacing;
            if (created)
            {
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperCenter;
            }
            EditorUtility.SetDirty(vlg);
            Debug.Log(string.Format("[CreateVerticalLayout] {0} spacing={1} created={2}", name, spacing, created));
        }
    }

    public static void ReparentTo(string childName, string newParentName)
    {
        GameObject child = FindFirstInScene(childName);
        GameObject newParent = FindFirstInScene(newParentName);
        if (child == null) { Debug.LogWarning("[ReparentTo] Not found: " + childName); return; }
        if (newParent == null) { Debug.LogWarning("[ReparentTo] Not found: " + newParentName); return; }
        if (child.transform.parent == newParent.transform) { Debug.Log("[ReparentTo] " + childName + " already under " + newParentName); return; }
        Undo.SetTransformParent(child.transform, newParent.transform, UILayoutBuilder.UndoLabel);
        InvalidateSceneIndex();
        Debug.Log("[ReparentTo] " + childName + " → " + newParentName);
    }

    public static T FirstSceneInstance<T>(T[] all) where T : Component
    {
        if (all == null) return null;
        for (int i = 0; i < all.Length; i++)
        {
            T c = all[i];
            if (c == null) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(c)) continue;
            return c;
        }
        return null;
    }

    public static Transform FindAnywhere(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform r = FindAnywhere(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    public static void SetSiblingOrder(Transform parent, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform c = parent.Find(names[i]);
            if (c != null) c.SetSiblingIndex(i);
        }
    }

    public static void StripComponent<T>(string name) where T : Component
    {
        var list = FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            T[] comps = go.GetComponents<T>();
            int count = comps.Length;
            for (int c = 0; c < comps.Length; c++)
            {
                Undo.DestroyObjectImmediate(comps[c]);
            }
            EditorUtility.SetDirty(go);
            Debug.Log(string.Format("[Strip<{0}>] {1} [{2}/{3}] removed={4}\n  path={5}",
                typeof(T).Name, name, i + 1, list.Count, count, PathOf(go)));
        }
    }
}
