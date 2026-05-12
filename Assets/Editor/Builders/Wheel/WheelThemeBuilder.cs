#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WheelThemeBuilder
{
    private const string ConfigsDir = "Assets/ScriptableObjects/Themes";
    private const string SpritesDir = "Assets/Sprites/Wheel";
    private const string WheelConfigPath = "Assets/ScriptableObjects/Core/WheelConfig.asset";

    public static void CreateDefaultThemes()
    {
        EnsureFolder(ConfigsDir);

        WheelThemeData bronze = BuildOrLoad("WheelTheme_Bronze", WheelTier.Bronze,
            "ui_spin_bronze_base", null, "ui_spin_bronze_indicator");
        WheelThemeData silver = BuildOrLoad("WheelTheme_Silver", WheelTier.Silver,
            "ui_spin_silver_base", null, "ui_spin_silver_indicator");
        WheelThemeData gold = BuildOrLoad("WheelTheme_Gold", WheelTier.Gold,
            "ui_spin_golden_base", null, "ui_spin_golden_indicator");

        AssetDatabase.SaveAssets();

        WheelConfig config = AssetDatabase.LoadAssetAtPath<WheelConfig>(WheelConfigPath);
        if (config != null)
        {
            bool changed = false;
            if (config.bronzeTheme == null) { config.bronzeTheme = bronze; changed = true; }
            if (config.silverTheme == null) { config.silverTheme = silver; changed = true; }
            if (config.goldTheme   == null) { config.goldTheme   = gold;   changed = true; }
            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("[WheelThemeBuilder] WheelConfig.asset theme slots populated.");
            }
            else
            {
                Debug.Log("[WheelThemeBuilder] WheelConfig theme slots already filled — left as-is.");
            }
        }
        else
        {
            Debug.LogWarning($"[WheelThemeBuilder] {WheelConfigPath} not found; created themes but did not auto-wire.");
        }

        Debug.Log("[WheelThemeBuilder] Done. Themes live in " + ConfigsDir + "/. Adjust silverStartZone / goldStartZone on WheelConfig to taste.");
    }

    private static WheelThemeData BuildOrLoad(string fileName, WheelTier tier, string baseSprite, string frameSprite, string indicatorSprite)
    {
        string path = $"{ConfigsDir}/{fileName}.asset";
        WheelThemeData asset = AssetDatabase.LoadAssetAtPath<WheelThemeData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WheelThemeData>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[WheelThemeBuilder] Created {path}");
        }

        asset.tier = tier;

        asset.wheelBase      = LoadSprite(baseSprite);
        asset.wheelFrame     = LoadSprite(frameSprite);
        asset.wheelIndicator = LoadSprite(indicatorSprite);
        if (asset.frameTint == default) asset.frameTint = Color.white;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        foreach (string ext in new[] { ".png", ".jpg" })
        {
            string path = $"{SpritesDir}/{spriteName}{ext}";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sp) return sp;
            }
        }
        Debug.LogWarning($"[WheelThemeBuilder] Sprite not found: {spriteName} (looked in {SpritesDir})");
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
