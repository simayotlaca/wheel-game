#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SuperZonePremiumAudit
{
    const string SuperPath = "Assets/ScriptableObjects/Zones/SuperZone.asset";

    public static void Run()
    {
        var zone = AssetDatabase.LoadAssetAtPath<ZoneConfig>(SuperPath);
        if (zone == null) { Debug.LogError($"[SuperZonePremiumAudit] missing zone at {SuperPath}"); return; }

        var byTier = new Dictionary<RewardTier, List<RewardDefinition>>
        {
            { RewardTier.Normal, new List<RewardDefinition>() },
            { RewardTier.Safe,   new List<RewardDefinition>() },
            { RewardTier.Super,  new List<RewardDefinition>() },
        };

        if (zone.slices != null)
        {
            for (int i = 0; i < zone.slices.Length; i++)
            {
                var s = zone.slices[i];
                if (s == null || s.reward == null) continue;
                if (byTier.TryGetValue(s.reward.minZoneTier, out var list)) list.Add(s.reward);
            }
        }

        int total = byTier[RewardTier.Normal].Count + byTier[RewardTier.Safe].Count + byTier[RewardTier.Super].Count;
        Debug.Log($"[SuperZonePremiumAudit] Super pool composition (total={total}):");
        Debug.Log($"  · minZoneTier=Super : {byTier[RewardTier.Super].Count}");
        Debug.Log($"  · minZoneTier=Safe  : {byTier[RewardTier.Safe].Count}");
        Debug.Log($"  · minZoneTier=Normal: {byTier[RewardTier.Normal].Count}  ← basic-tier rewards visible in premium pool");

        if (byTier[RewardTier.Normal].Count > 0)
        {
            Debug.LogWarning("[SuperZonePremiumAudit] Normal-tier rewards eligible in Super zone (tier slip):");
            for (int i = 0; i < byTier[RewardTier.Normal].Count; i++)
            {
                var r = byTier[RewardTier.Normal][i];
                Debug.LogWarning($"  · {r.rewardId,-30} slotCategory={r.slotCategory}");
            }
            Debug.LogWarning("[SuperZonePremiumAudit] If premium feel matters, raise minZoneTier on these or remove them from Super slices[].");
        }
        else
        {
            Debug.Log("[SuperZonePremiumAudit] no tier slip — Super pool is purely Safe/Super tier.");
        }
    }
}
#endif
