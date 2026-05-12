#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WheelAutoLinker
{
    const string GeneratedRoot = "Assets/Sprites/Wheel/Generated";

    static WheelAutoLinker()
    {
        EditorApplication.delayCall += Heal;
    }

    static void Heal()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        int repaired = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:RewardDefinition"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null) continue;
            if (reward.wheelIcon != null) continue;
            if (reward.icon == null) continue;

            string baseName = Path.GetFileNameWithoutExtension(
                AssetDatabase.GetAssetPath(reward.icon));
            string wheelPath = $"{GeneratedRoot}/{baseName}_wheel.png";
            var wheelSpr = AssetDatabase.LoadAssetAtPath<Sprite>(wheelPath);
            if (wheelSpr == null) continue;

            reward.wheelIcon = wheelSpr;
            EditorUtility.SetDirty(reward);
            repaired++;
            Debug.Log($"[WheelAutoLinker] linked {reward.rewardId} → {baseName}_wheel.png");
        }

        if (repaired > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[WheelAutoLinker] repaired {repaired} RewardDefinition(s).");
        }
    }
}
#endif
