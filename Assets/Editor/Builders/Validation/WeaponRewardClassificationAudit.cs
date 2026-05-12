#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WeaponRewardClassificationAudit
{
    static readonly string[] WeaponNameHints =
    {
        "weapon_", "pistol", "rifle", "shotgun", "sniper", "smg", "knife"
    };

    public static void Run()
    {
        var rewards = RewardClassificationAudit.LoadAllRewards();
        var mismatches = new List<RewardDefinition>();
        int weaponCorrect = 0;

        for (int i = 0; i < rewards.Length; i++)
        {
            var r = rewards[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.rewardId)) continue;

            bool nameSuggestsWeapon = NameSuggestsWeapon(r.rewardId);
            if (!nameSuggestsWeapon) continue;

            if (r.slotCategory == SlotCategory.Weapon) weaponCorrect++;
            else mismatches.Add(r);
        }

        if (mismatches.Count == 0)
        {
            Debug.Log($"[WeaponRewardClassificationAudit] OK — {weaponCorrect} weapon-named rewards match slotCategory=Weapon, no drift.");
        }
        else
        {
            Debug.LogWarning($"[WeaponRewardClassificationAudit] {mismatches.Count} naming/category mismatch(es) (correct={weaponCorrect}):");
            for (int i = 0; i < mismatches.Count; i++)
            {
                var r = mismatches[i];
                Debug.LogWarning($"  · rewardId={r.rewardId,-30} slotCategory={r.slotCategory} (suggested: Weapon) minZoneTier={r.minZoneTier} path={AssetDatabase.GetAssetPath(r)}");
            }
            Debug.LogWarning("[WeaponRewardClassificationAudit] These rewards fill weapon-quota slots via cross-category fallback today. Reclassify them in a separate fix pass.");
        }
    }

    static bool NameSuggestsWeapon(string id)
    {
        for (int i = 0; i < WeaponNameHints.Length; i++)
            if (id.IndexOf(WeaponNameHints[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }
}
#endif
