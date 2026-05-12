#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RewardClassificationAudit
{
    public static void RunAll()
    {
        WeaponRewardClassificationAudit.Run();
        GoldRewardAudit.Run();
        ZoneQuotaCoverageAudit.Run();
        SuperZonePremiumAudit.Run();
    }

    internal static RewardDefinition[] LoadAllRewards()
    {
        string[] guids = AssetDatabase.FindAssets("t:RewardDefinition");
        var rewards = new RewardDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            rewards[i] = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
        }
        return rewards;
    }

    internal static ZoneConfig LoadZone(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<ZoneConfig>(assetPath);
    }

    internal static readonly (string path, string label)[] Zones =
    {
        ("Assets/ScriptableObjects/Zones/NormalZone.asset", "Normal"),
        ("Assets/ScriptableObjects/Zones/SafeZone.asset",   "Safe"),
        ("Assets/ScriptableObjects/Zones/SuperZone.asset",  "Super"),
    };

    internal static bool IsEligibleForZone(RewardDefinition r, ZoneConfig zone)
    {
        if (r == null || zone == null) return false;
        if (r.slotCategory == SlotCategory.Unassigned && !r.isDeath) return false;
        if ((int)r.minZoneTier > (int)zone.type) return false;
        if (r.isDeath && !zone.poolRules.allowDeath) return false;
        return true;
    }
}
#endif
