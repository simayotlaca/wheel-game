using System.Collections.Generic;
using UnityEngine;

public partial class WheelLogic
{

    private const float RecentIconPenaltyFloor = 0.05f;

    private bool SampleByCategoryQuotas(ZoneConfig zone, int zoneIndex)
    {
        var rules = zone.poolRules;

        int eff_level = zoneIndex == int.MinValue ? int.MaxValue : zoneIndex;

        if (rules.slotCount != WheelSlotCapacity)
        {
            FailToLegacy(zone, "PRE-G1", $"slotCount={rules.slotCount} ≠ {WheelSlotCapacity}");
            return false;
        }

        int death_quota = rules.EffectiveDeathSlots;
        int quotaSum = death_quota + rules.currencySlots + rules.consumableSlots
                     + rules.throwableSlots + rules.weaponSlots + rules.compactSlots
                     + rules.chestSlots + rules.cosmeticSlots + rules.goldSlots + rules.flexSlots;
        if (quotaSum != WheelSlotCapacity)
        {
            FailToLegacy(zone, "PRE-G2", $"quota sum (incl. flex)={quotaSum} ≠ {WheelSlotCapacity}");
            return false;
        }

        int exp_death = zone.type == ZoneType.Normal ? 1 : 0;
        if (death_quota != exp_death)
        {
            FailToLegacy(zone, "PRE-G3", $"effectiveDeathSlots={death_quota} (allowDeath={rules.allowDeath}, deathSlots={rules.deathSlots}), expected {exp_death} for {zone.type}");
            return false;
        }

        for (int i = 0; i < by_category.Length; i++)
            by_category[i].Clear();
        int zoneTier = (int)zone.type;
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var s = zone.slices[i];
            if (s == null || s.reward == null) continue;
            var r = s.reward;
            if (r.slotCategory == SlotCategory.Unassigned && !r.isDeath) continue;
            if ((int)r.minZoneTier > zoneTier) continue;
            if (eff_level < r.minZoneLevel) continue;
            if (r.isDeath && !rules.allowDeath) continue;
            var cat = SlotCategoryHelper.For(r);
            by_category[(int)cat].Add(s);
        }

        if (exp_death > 0 && by_category[(int)SlotCategory.Death].Count == 0)
        {
            FailToLegacy(zone, "PRE-G4", $"{zone.type} requires a death entry but none in slices[]");
            return false;
        }

        picked_list.Clear();
        picked_icons.Clear();
        picked_families.Clear();

        for (int c = 0; c < CategoryOrder.Length; c++)
        {
            SlotCategory cat = CategoryOrder[c];
            int quota = SlotCategoryHelper.QuotaFor(rules, cat);
            if (quota <= 0) continue;

            var pool = by_category[(int)cat];
            for (int i = 0; i < quota; i++)
            {
                var pick = PickFromPool(pool, picked_icons, picked_families, rules);
                if (pick == null && rules.allowFallbackWhenPoolInsufficient)
                {
                    pick = PickFallbackAcrossCategories(cat, exp_death, picked_icons, picked_families, rules);
                    if (pick != null)
                        DebugLogger.LogWarning($"[WheelLogic] {zone.name}: {cat} slot {i+1}/{quota} fell back to '{pick.reward?.rewardId}' (cross-category, death-rule respected).");
                }
                if (pick != null)
                {
                    picked_list.Add(pick);
                    if (pick.reward != null && pick.reward.icon != null)
                        picked_icons.Add(pick.reward.icon);
                    if (pick.reward != null && !string.IsNullOrEmpty(pick.reward.visualFamily))
                        picked_families.Add(pick.reward.visualFamily);
                }
            }
        }

        for (int i = 0; i < rules.flexSlots; i++)
        {
            var flexPick = PickFallbackAcrossCategories(SlotCategory.Death,
                                                       exp_death, picked_icons, picked_families, rules);
            if (flexPick != null)
            {
                picked_list.Add(flexPick);
                if (flexPick.reward != null && flexPick.reward.icon != null)
                    picked_icons.Add(flexPick.reward.icon);
                if (flexPick.reward != null && !string.IsNullOrEmpty(flexPick.reward.visualFamily))
                    picked_families.Add(flexPick.reward.visualFamily);
            }
        }

        if (picked_list.Count != WheelSlotCapacity)
        {
            FailToLegacy(zone, "POST-G5", $"filled {picked_list.Count}/{WheelSlotCapacity} slots (pool exhausted or fallback failed)");
            return false;
        }

        for (int i = 0; i < picked_list.Count; i++)
        {
            if (picked_list[i] == null || picked_list[i].reward == null)
            {
                FailToLegacy(zone, "POST-G6", $"slot {i} is null after fill");
                return false;
            }
        }

        int real_death = 0;
        for (int i = 0; i < picked_list.Count; i++)
            if (picked_list[i].reward.isDeath) real_death++;
        if (real_death != exp_death)
        {
            FailToLegacy(zone, "POST-G7", $"face has {real_death} death slice(s), expected {exp_death}");
            return false;
        }

        if (rules.enforceUniqueIcons)
        {
            check_icons.Clear();
            var seen_icons = check_icons;
            for (int i = 0; i < picked_list.Count; i++)
            {
                var icon = picked_list[i].reward.icon;
                if (icon == null) continue;
                if (!seen_icons.Add(icon))
                {
                    FailToLegacy(zone, "POST-G8", $"duplicate icon '{icon.name}' on face (enforceUniqueIcons=true)");
                    return false;
                }
            }

            check_families.Clear();
            var seen_families = check_families;
            for (int i = 0; i < picked_list.Count; i++)
            {
                var fam = picked_list[i].reward.visualFamily;
                if (string.IsNullOrEmpty(fam)) continue;
                if (!seen_families.Add(fam))
                {
                    FailToLegacy(zone, "POST-G9", $"duplicate visualFamily '{fam}' on face (enforceUniqueIcons=true)");
                    return false;
                }
            }
        }

        for (int i = picked_list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            var tmp = picked_list[i];
            picked_list[i] = picked_list[j];
            picked_list[j] = tmp;
        }
        for (int i = 0; i < WheelSlotCapacity; i++)
            wheelSlots[i] = picked_list[i];

        previousFaceIcons.Clear();
        if (rules.useRecentIconMemory)
        {
            for (int i = 0; i < picked_list.Count; i++)
            {
                var icon = picked_list[i].reward != null ? picked_list[i].reward.icon : null;
                if (icon != null) previousFaceIcons.Add(icon);
            }
        }

        return true;
    }

    private static void FailToLegacy(ZoneConfig zone, string code, string detail)
    {
        DebugLogger.LogWarning($"[WheelLogic] {zone.name}: category-quota sampler failed [{code}] {detail}. Using legacy sampler instead.");
    }

    private SliceDefinition PickFromPool(List<SliceDefinition> pool, HashSet<Sprite> pickedIcons, HashSet<string> pickedFamilies, in ZonePoolRules rules)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += EffectiveWeight(pool[i], pickedIcons, pickedFamilies, rules);
        if (totalWeight <= 0f) return null;

        if (!rules.useWeightedRandom)
        {
            int eligibles = 0;
            for (int i = 0; i < pool.Count; i++)
                if (EffectiveWeight(pool[i], pickedIcons, pickedFamilies, rules) > 0f) eligibles++;
            if (eligibles == 0) return null;
            int target = rng.Next(0, eligibles);
            int seen = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                float w = EffectiveWeight(pool[i], pickedIcons, pickedFamilies, rules);
                if (w <= 0f) continue;
                if (seen == target) return pool[i];
                seen++;
            }
            return null;
        }

        float r = (float)rng.NextDouble() * totalWeight;
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            float w = EffectiveWeight(pool[i], pickedIcons, pickedFamilies, rules);
            if (w <= 0f) continue;
            acc += w;
            if (r <= acc) return pool[i];
        }

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (EffectiveWeight(pool[i], pickedIcons, pickedFamilies, rules) > 0f)
                return pool[i];
        }
        return null;
    }

    private float EffectiveWeight(SliceDefinition slice, HashSet<Sprite> pickedIcons, HashSet<string> pickedFamilies, in ZonePoolRules rules)
    {
        if (slice == null || slice.reward == null) return 0f;
        var icon = slice.reward.icon;
        if (rules.enforceUniqueIcons && icon != null && pickedIcons.Contains(icon)) return 0f;
        if (rules.enforceUniqueIcons)
        {
            var fam = slice.reward.visualFamily;
            if (!string.IsNullOrEmpty(fam) && pickedFamilies.Contains(fam)) return 0f;
        }

        float w = slice.weight;
        if (w <= 0f) return 0f;
        if (rules.useRecentIconMemory && icon != null && previousFaceIcons.Contains(icon))
        {
            float penalty = Mathf.Max(rules.recentIconPenalty, RecentIconPenaltyFloor);
            w *= penalty;
        }
        return w;
    }

    private SliceDefinition PickFallbackAcrossCategories(SlotCategory exclude, int exp_death,
                                                        HashSet<Sprite> pickedIcons, HashSet<string> pickedFamilies, in ZonePoolRules rules)
    {
        fallback_list.Clear();
        for (int c = 0; c < CategoryOrder.Length; c++)
        {
            SlotCategory key = CategoryOrder[c];
            if (key == exclude) continue;
            if (key == SlotCategory.Death && exp_death == 0) continue;
            fallback_list.AddRange(by_category[(int)key]);
        }
        return PickFromPool(fallback_list, pickedIcons, pickedFamilies, rules);
    }
}
