#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ZoneQuotaCoverageAudit
{
    public static void Run()
    {
        int errors = 0;
        int warnings = 0;

        for (int z = 0; z < RewardClassificationAudit.Zones.Length; z++)
        {
            var (path, label) = RewardClassificationAudit.Zones[z];
            var zone = RewardClassificationAudit.LoadZone(path);
            if (zone == null) { Debug.LogError($"[ZoneQuotaCoverageAudit] missing zone asset at {path}"); errors++; continue; }

            Debug.Log($"[ZoneQuotaCoverageAudit] {label} (slices={zone.slices?.Length ?? 0}, slotCount={zone.poolRules.slotCount})");

            int[] eligibleByCategory = CountEligibleByCategory(zone);

            ReportRow(label, zone, SlotCategory.Death,      zone.poolRules.EffectiveDeathSlots, eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Currency,   zone.poolRules.currencySlots,      eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Consumable, zone.poolRules.consumableSlots,    eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Throwable,  zone.poolRules.throwableSlots,     eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Weapon,     zone.poolRules.weaponSlots,        eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Compact,    zone.poolRules.compactSlots,       eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Chest,      zone.poolRules.chestSlots,         eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Cosmetic,   zone.poolRules.cosmeticSlots,      eligibleByCategory, ref errors, ref warnings);
            ReportRow(label, zone, SlotCategory.Gold,       zone.poolRules.goldSlots,          eligibleByCategory, ref errors, ref warnings);

            if (zone.poolRules.flexSlots > 0)
                Debug.Log($"  flex={zone.poolRules.flexSlots} (wildcard, fills cross-category)");
        }

        Debug.Log($"--- [ZoneQuotaCoverageAudit] DONE — errors:{errors} warnings:{warnings} ---");
    }

    static void ReportRow(string zoneLabel, ZoneConfig zone, SlotCategory cat, int quota, int[] eligible, ref int errors, ref int warnings)
    {
        int avail = eligible[(int)cat];
        if (quota == 0)
        {
            if (avail > 0) Debug.Log($"  {cat,-11} quota=0 eligible={avail} (unused content, ok)");
            return;
        }

        if (avail == 0)
        {
            Debug.LogError($"  {cat,-11} quota={quota} eligible=0 ← cross-fallback fires every face");
            errors++;
        }
        else if (avail < quota)
        {
            Debug.LogWarning($"  {cat,-11} quota={quota} eligible={avail} ← under enforceUniqueIcons, fallback may fire");
            warnings++;
        }
        else
        {
            Debug.Log($"  {cat,-11} quota={quota} eligible={avail} ok");
        }
    }

    static int[] CountEligibleByCategory(ZoneConfig zone)
    {

        int[] counts = new int[(int)SlotCategory.Gold + 1];
        if (zone == null || zone.slices == null) return counts;
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var s = zone.slices[i];
            if (s == null || s.reward == null) continue;
            if (!RewardClassificationAudit.IsEligibleForZone(s.reward, zone)) continue;
            var cat = SlotCategoryHelper.For(s.reward);
            counts[(int)cat]++;
        }
        return counts;
    }
}
#endif
