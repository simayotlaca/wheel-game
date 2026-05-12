using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal static class BackgroundBuilder
{
    private const string CanvasName = "ui_canvas_background";

    private const string SrcStar        = "Assets/Sprites/Wheel/star_glow_alpha.png";
    private const string SolidWhite     = "Assets/Sprites/Wheel/ui_image_solid_white.png";

    private const string GenDir            = "Assets/Generated";
    private const string AtmosphereAsset   = "Assets/Generated/bg_atmosphere_v6.png";
    private const string VignetteAsset     = "Assets/Generated/bg_vignette.png";
    private const string AdditiveShaderName = "UI/Additive";
    private const string AdditiveMatAsset   = "Assets/Generated/UI_Additive.mat";

    private static readonly Vector2 WheelOffset = new Vector2(40f, -20f);

    private static readonly HashSet<string> KeepNames = new HashSet<string>
    {
        "ui_image_bg_reference",
        "ui_image_bg_dim_overlay",
        "ui_image_bg_vignette",
    };

    public static void Build()
    {
        GameObject canvasRoot = UILayoutBuilder.FindFirstInScene("WheelCanvas");
        if (canvasRoot == null)
        {
            Debug.LogWarning("[BackgroundBuilder] WheelCanvas not found.");
            return;
        }

        GameObject bgCanvasGO = UILayoutBuilder.EnsureChild(canvasRoot.transform, CanvasName);
        bgCanvasGO.transform.SetSiblingIndex(0);

        RectTransform canvasRT = bgCanvasGO.GetComponent<RectTransform>();
        Undo.RecordObject(canvasRT, UILayoutBuilder.UndoLabel);
        canvasRT.anchorMin = Vector2.zero;
        canvasRT.anchorMax = Vector2.one;
        canvasRT.pivot = new Vector2(0.5f, 0.5f);
        canvasRT.offsetMin = Vector2.zero;
        canvasRT.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(canvasRT);

        Canvas bgCanvas = UILayoutBuilder.EnsureComponent<Canvas>(bgCanvasGO);
        Undo.RecordObject(bgCanvas, UILayoutBuilder.UndoLabel);
        bgCanvas.overrideSorting = true;
        bgCanvas.sortingOrder = -1;
        EditorUtility.SetDirty(bgCanvas);

        GraphicRaycaster stale = bgCanvasGO.GetComponent<GraphicRaycaster>();
        if (stale != null) Undo.DestroyObjectImmediate(stale);

        for (int i = bgCanvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = bgCanvasGO.transform.GetChild(i);
            if (!KeepNames.Contains(child.name))
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        EnsureSpriteImport(BackgroundStyle.BgReferenceSprite);
        EnsureSpriteImport(SrcStar);
        EnsureSpriteImport(SolidWhite);

        EnsureGeneratedDir();
        EnsureVignetteAsset();
        EnsureSpriteImport(VignetteAsset);
        if (AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundStyle.BgReferenceSprite) == null)
        {
            Debug.LogWarning("[BackgroundBuilder] reference sprite missing at "
                + BackgroundStyle.BgReferenceSprite + " — drop the PNG there and re-run Build UI Layout.");
        }

        BuildEnvelope("ui_image_bg_reference", bgCanvasGO.transform,
            BackgroundStyle.BgReferenceSprite,
            aspectRatio: BackgroundStyle.BgAspectRatio,
            color: BackgroundStyle.BgReferenceTint, idx: 0);

        BuildStretchDim("ui_image_bg_dim_overlay", bgCanvasGO.transform, SolidWhite,
            color: BackgroundStyle.DimWashColor, idx: 1);

        BuildStretchDim("ui_image_bg_vignette", bgCanvasGO.transform, VignetteAsset,
            color: BackgroundStyle.VignetteTintColor, idx: 2);
    }

    private static void BuildStretchDim(string name, Transform parent, string spritePath,
                                        Color32 color, int idx)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        go.transform.SetSiblingIndex(idx);

        AspectRatioFitter staleArf = go.GetComponent<AspectRatioFitter>();
        if (staleArf != null) Undo.DestroyObjectImmediate(staleArf);

        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localEulerAngles = Vector3.zero;
        rt.localScale = Vector3.one;
        EditorUtility.SetDirty(rt);

        ApplyImage(go, spritePath, color, material: null, preserveAspect: false);
    }

    private static void BuildEnvelope(string name, Transform parent, string spritePath,
                                      float aspectRatio, Color32 color, int idx)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        go.transform.SetSiblingIndex(idx);

        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localEulerAngles = Vector3.zero;
        rt.localScale = Vector3.one;
        EditorUtility.SetDirty(rt);

        AspectRatioFitter arf = UILayoutBuilder.EnsureComponent<AspectRatioFitter>(go);
        Undo.RecordObject(arf, UILayoutBuilder.UndoLabel);
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        arf.aspectRatio = aspectRatio;
        EditorUtility.SetDirty(arf);

        ApplyImage(go, spritePath, color, material: null, preserveAspect: false);
    }

    private static void BuildAnchored(string name, Transform parent, string spritePath,
                                      Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos,
                                      Color32 color, int idx, float rotationZ = 0f,
                                      bool flipX = false, Material material = null,
                                      bool preserveAspect = false)
    {
        GameObject go = UILayoutBuilder.EnsureChild(parent, name);
        go.transform.SetSiblingIndex(idx);

        AspectRatioFitter staleArf = go.GetComponent<AspectRatioFitter>();
        if (staleArf != null) Undo.DestroyObjectImmediate(staleArf);

        RectTransform rt = go.GetComponent<RectTransform>();
        Undo.RecordObject(rt, UILayoutBuilder.UndoLabel);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        rt.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
        EditorUtility.SetDirty(rt);

        ApplyImage(go, spritePath, color, material, preserveAspect);
    }

    private static void ApplyImage(GameObject go, string spritePath, Color32 color,
                                   Material material, bool preserveAspect)
    {
        UILayoutBuilder.EnsureComponent<CanvasRenderer>(go);
        Image img = UILayoutBuilder.EnsureComponent<Image>(go);
        Undo.RecordObject(img, UILayoutBuilder.UndoLabel);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) Debug.LogWarning("[BackgroundBuilder] sprite not found: " + spritePath);
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = preserveAspect;
        img.color = color;

        img.material = material;
        img.raycastTarget = false;
        img.maskable = false;
        EditorUtility.SetDirty(img);
    }

    private static void EnsureGeneratedDir()
    {
        if (!System.IO.Directory.Exists(GenDir))
        {
            System.IO.Directory.CreateDirectory(GenDir);
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureSpriteImport(string assetPath)
    {
        if (!System.IO.File.Exists(assetPath)) return;
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite)
        {
            ti.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (ti.spriteImportMode != SpriteImportMode.Single)
        {
            ti.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (!ti.alphaIsTransparency)
        {
            ti.alphaIsTransparency = true;
            changed = true;
        }
        if (ti.mipmapEnabled)
        {
            ti.mipmapEnabled = false;
            changed = true;
        }
        if (ti.wrapMode != TextureWrapMode.Clamp)
        {
            ti.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }
        if (ti.filterMode != FilterMode.Bilinear)
        {
            ti.filterMode = FilterMode.Bilinear;
            changed = true;
        }
        if (changed)
        {
            ti.SaveAndReimport();
            Debug.Log("[BackgroundBuilder] re-imported as Sprite: " + assetPath);
        }
    }

    private static Material GetAdditiveMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMatAsset);
        if (mat != null) return mat;

        Shader shader = Shader.Find(AdditiveShaderName);
        if (shader == null)
        {
            Debug.LogWarning("[BackgroundBuilder] " + AdditiveShaderName
                + " shader not found — additive blending disabled. "
                + "Verify Assets/Shaders/UIAdditive.shader exists and compiled.");
            return null;
        }

        mat = new Material(shader);
        mat.name = "UI_Additive";
        AssetDatabase.CreateAsset(mat, AdditiveMatAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("[BackgroundBuilder] created additive material at " + AdditiveMatAsset);
        return mat;
    }

    private static void EnsureAtmosphereAsset()
    {
        if (System.IO.File.Exists(AtmosphereAsset)) return;
        const int W = 1080, H = 1920;
        Color32[] pixels = TacticalBackgroundBaker.BakeRadialGradient(W, H,
            center: new Color32(0x2A, 0x0B, 0x2E, 255),
            corner: new Color32(0x1A, 0x08, 0x22, 255),
            noiseAmplitude: 3, noiseSeed: 1337);
        WriteSpritePng(W, H, pixels, AtmosphereAsset);
    }

    private static void EnsureVignetteAsset()
    {
        if (System.IO.File.Exists(VignetteAsset)) return;

        const int Size = 768;
        const byte MaxAlpha = 245;

        Color32[] pixels = new Color32[Size * Size];
        float c = (Size - 1) * 0.5f;
        float maxD = Mathf.Sqrt(c * c + c * c);
        float innerRadius = maxD * 0.32f;
        float outerRadius = maxD * 0.95f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01((d - innerRadius) / (outerRadius - innerRadius));
                t = t * t * (3f - 2f * t);
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(t * MaxAlpha));
            }
        }
        WriteSpritePng(Size, Size, pixels, VignetteAsset);
    }

    private static void WriteSpritePng(int W, int H, Color32[] pixels, string assetPath)
    {
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
        }
        Debug.Log("[BackgroundBuilder] baked " + assetPath);
    }
}
