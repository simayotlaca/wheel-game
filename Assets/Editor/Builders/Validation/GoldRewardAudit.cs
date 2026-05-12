#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GoldRewardAudit
{
    public static void Run()
    {
        var allRewards = RewardClassificationAudit.LoadAllRewards();
        var allGoldRewards = new List<RewardDefinition>();
        for (int i = 0; i < allRewards.Length; i++)
            if (allRewards[i] != null && allRewards[i].slotCategory == SlotCategory.Gold)
                allGoldRewards.Add(allRewards[i]);

        Debug.Log($"[GoldRewardAudit] {allGoldRewards.Count} reward(s) classified as slotCategory=Gold:");
        for (int i = 0; i < allGoldRewards.Count; i++)
        {
            var r = allGoldRewards[i];
            Debug.Log($"  · {r.rewardId,-30} minZoneTier={r.minZoneTier} icon={(r.icon != null ? r.icon.name : "<null>")}");
        }

        int problems = 0;
        for (int z = 0; z < RewardClassificationAudit.Zones.Length; z++)
        {
            var (path, label) = RewardClassificationAudit.Zones[z];
            var zone = RewardClassificationAudit.LoadZone(path);
            if (zone == null) { Debug.LogError($"[GoldRewardAudit] missing zone asset at {path}"); problems++; continue; }

            int quota = zone.poolRules.goldSlots;
            int eligibleInSlices = CountEligibleGoldInSlices(zone);

            string summary = $"[GoldRewardAudit] {label,-6} goldSlots={quota} eligibleGoldInSlices={eligibleInSlices}";
            if (quota > 0 && eligibleInSlices == 0)
            {
                Debug.LogError(summary + " ← QUOTA UNFULFILLED, cross-fallback will replace gold with non-gold every face");
                problems++;
            }
            else if (quota > 0 && eligibleInSlices < quota)
            {
                Debug.LogWarning(summary + " ← pool smaller than quota; under enforceUniqueIcons, fallback may fire");
                problems++;
            }
            else
            {
                Debug.Log(summary + " ← OK");
            }
        }

        int namingDrift = 0;
        for (int i = 0; i < allRewards.Length; i++)
        {
            var r = allRewards[i];
            if (r == null || string.IsNullOrEmpty(r.rewardId)) continue;
            bool nameSaysGold = r.rewardId.IndexOf("gold", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (nameSaysGold && r.slotCategory != SlotCategory.Gold)
            {
                Debug.LogWarning($"[GoldRewardAudit] naming/category drift: rewardId={r.rewardId} slotCategory={r.slotCategory} (name suggests Gold)");
                namingDrift++;
            }
        }
        if (namingDrift == 0) Debug.Log("[GoldRewardAudit] no gold-named rewards mis-classified.");

        Debug.Log($"--- [GoldRewardAudit] DONE — issues:{problems} namingDrift:{namingDrift} ---");
    }

    static int CountEligibleGoldInSlices(ZoneConfig zone)
    {
        if (zone == null || zone.slices == null) return 0;
        int count = 0;
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var s = zone.slices[i];
            if (s == null || s.reward == null) continue;
            if (s.reward.slotCategory != SlotCategory.Gold) continue;
            if (!RewardClassificationAudit.IsEligibleForZone(s.reward, zone)) continue;
            count++;
        }
        return count;
    }
}
#endif
