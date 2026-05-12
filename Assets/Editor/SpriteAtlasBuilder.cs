#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasBuilder
{
    const string SOURCE_FOLDER = "Assets/Sprites/Wheel";
    const string ATLAS_FOLDER = "Assets/Atlases";

    static readonly string[] Categories =
    {
        "Icon", "Spin", "Button", "Panel", "Frame", "VFX"
    };

    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(SOURCE_FOLDER))
        {
            Debug.LogError($"[SpriteAtlasBuilder] Source folder missing: {SOURCE_FOLDER}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ATLAS_FOLDER))
            AssetDatabase.CreateFolder("Assets", "Atlases");

        EnforceUncompressedSources();

        var groups = new Dictionary<string, List<Object>>();
        foreach (string cat in Categories) groups[cat] = new List<Object>();

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SOURCE_FOLDER });
        foreach (string guid in spriteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;
            groups[Categorize(sprite.name)].Add(sprite);
        }

        var built = new List<SpriteAtlas>();
        foreach (string cat in Categories)
        {
            string atlasPath = $"{ATLAS_FOLDER}/{cat}SpriteAtlas.spriteatlas";
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            atlas.SetIncludeInBuild(true);

            SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
            packing.enableTightPacking = false;
            packing.enableRotation = false;
            packing.padding = 4;
            atlas.SetPackingSettings(packing);

            SpriteAtlasTextureSettings tex = atlas.GetTextureSettings();
            tex.generateMipMaps = false;
            tex.filterMode = FilterMode.Bilinear;
            tex.readable = false;
            atlas.SetTextureSettings(tex);

            TextureImporterPlatformSettings platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.format = TextureImporterFormat.Automatic;
            atlas.SetPlatformSettings(platform);

            atlas.Remove(atlas.GetPackables());
            if (groups[cat].Count > 0)
                atlas.Add(groups[cat].ToArray());

            EditorUtility.SetDirty(atlas);
            built.Add(atlas);
            Debug.Log($"[SpriteAtlasBuilder] {cat}SpriteAtlas ← {groups[cat].Count} sprites");
        }

        AssetDatabase.SaveAssets();
        SpriteAtlasUtility.PackAtlases(built.ToArray(), EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
    }

    static string Categorize(string name)
    {
        string s = name.ToLowerInvariant();
        if (s.Contains("_fx_") || s.Contains("vfx") || s.Contains("glow") || s.Contains("flash") || s.Contains("shine"))
            return "VFX";
        if (s.Contains("button")) return "Button";
        if (s.Contains("_spin_") || s.StartsWith("ui_spin_")) return "Spin";
        if (s.Contains("_frame")) return "Frame";
        if (s.Contains("panel") || s.Contains("zone_bar") || s.Contains("slice_shadow")
            || s.Contains("image_solid") || s.Contains("bg_reference"))
            return "Panel";
        return "Icon";
    }

    static void EnforceUncompressedSources()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SOURCE_FOLDER });
        bool any = false;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            any = true;
        }
        if (any) AssetDatabase.Refresh();
    }
}
#endif
