#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WheelDistributionApplier
{
    const string CoreRoot       = "Assets/ScriptableObjects/Core";
    const string ZonesRoot      = "Assets/ScriptableObjects/Zones";
    const string SlicePlansRoot = "Assets/ScriptableObjects/Generated/SlicePlans";
    const string RewardsRoot    = "Assets/ScriptableObjects/Rewards";
    const string SpritesRoot    = "Assets/Sprites/Wheel";

    static readonly HashSet<string> missingPlanIds = new HashSet<string>();

    public static void Apply()
    {
        Dictionary<string, RewardDefinition> rewards = null;
        missingPlanIds.Clear();
        try
        {
            AssetDatabase.StartAssetEditing();

            rewards = EnsureRewards();
            ApplyTierClassification(rewards);
            FixGrenadeAsset(rewards);
            BuildAndAssignSlices(rewards);
            EnsureExitRulesWired();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        ZoneRewardTableBuilder.Bootstrap();
        ZoneRewardTableBuilder.Validate();

        if (rewards != null) LogPostBuildAudit(rewards);
    }

    static void EnsureExitRulesWired()
    {
        const string wheelConfigPath = CoreRoot + "/WheelConfig.asset";
        const string rulesAssetPath  = CoreRoot + "/RunExitRules.asset";

        var wheelConfig = AssetDatabase.LoadAssetAtPath<WheelConfig>(wheelConfigPath);
        if (wheelConfig == null)
        {
            Debug.LogError($"[WheelDistributionApplier] EnsureExitRulesWired: WheelConfig missing at {wheelConfigPath} — skipped.");
            return;
        }

        var rules = AssetDatabase.LoadAssetAtPath<RunExitRules>(rulesAssetPath);
        if (rules == null)
        {
            if (File.Exists(rulesAssetPath))
            {
                Debug.LogWarning($"[WheelDistributionApplier] {rulesAssetPath} exists but won't load as RunExitRules — recreating.");
                AssetDatabase.DeleteAsset(rulesAssetPath);
            }
            rules = ScriptableObject.CreateInstance<RunExitRules>();
            AssetDatabase.CreateAsset(rules, rulesAssetPath);
        }

        var so = new SerializedObject(wheelConfig);
        var prop = so.FindProperty("exitRules");
        if (prop == null)
        {
            Debug.LogError("[WheelDistributionApplier] WheelConfig has no 'exitRules' field — script out of sync.");
            return;
        }

        if (prop.objectReferenceValue != (Object)rules)
        {
            prop.objectReferenceValue = rules;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wheelConfig);
            Debug.Log($"[WheelDistributionApplier] WheelConfig.exitRules → {rulesAssetPath}");
        }
    }

    static void LogPostBuildAudit(Dictionary<string, RewardDefinition> byId)
    {
        if (missingPlanIds.Count > 0)
        {
            var ordered = new List<string>(missingPlanIds);
            ordered.Sort();
            Debug.LogError($"[WheelDistributionApplier] missing-from-byId rewardIds referenced by plan: {string.Join(", ", ordered)}");
        }

        var groups = new Dictionary<string, List<string>>();
        foreach (var kv in byId)
        {
            var fam = kv.Value != null ? kv.Value.visualFamily : null;
            if (string.IsNullOrEmpty(fam)) continue;
            if (!groups.TryGetValue(fam, out var list)) { list = new List<string>(); groups[fam] = list; }
            list.Add(kv.Key);
        }
        if (groups.Count == 0) return;

        var sb = new System.Text.StringBuilder("[WheelDistributionApplier] visualFamily groups:\n");
        var famKeys = new List<string>(groups.Keys);
        famKeys.Sort();
        foreach (var fam in famKeys)
        {
            var members = groups[fam];
            members.Sort();
            sb.Append("  ").Append(fam).Append(": ").Append(string.Join(", ", members)).Append('\n');
        }
        Debug.Log(sb.ToString());
    }

    static Dictionary<string, RewardDefinition> EnsureRewards()
    {
        var byId = new Dictionary<string, RewardDefinition>();
        var idToPath = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:RewardDefinition"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var r = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.rewardId))
            {
                Debug.LogError($"[WheelDistributionApplier] empty rewardId on asset: {path}");
                continue;
            }
            if (idToPath.TryGetValue(r.rewardId, out var existingPath))
            {
                Debug.LogError($"[WheelDistributionApplier] rewardId collision '{r.rewardId}' — both '{existingPath}' and '{path}' carry the same id. Fix one before re-running.");
                continue;
            }
            idToPath[r.rewardId] = path;
            byId[r.rewardId] = r;
        }

        var spec = new (string id, string name, string icon, RewardVisualCategory vc, SlotCategory sc, RewardTier tier, bool scales)[]
        {
            ("weapon_tier1_shotgun", "Tier 1 Shotgun", "UI_Icon_Renders_tier1_shotgun.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Normal, false),
            ("weapon_tier2_rifle",   "Tier 2 Rifle",   "UI_Icon_Renders_tier2_rifle.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Safe,   false),
            ("weapon_tier2_mle",     "Tier 2 Bayonet", "UI_Icon_Renders_tier2_mle.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Safe,   false),
            ("weapon_tier3_shotgun", "Tier 3 Shotgun", "UI_Icon_Renders_tier3_shotgun.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Super,  false),
            ("weapon_tier3_smg",     "Tier 3 SMG",     "UI_Icon_Renders_tier3_smg.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Super,  false),
            ("weapon_tier3_sniper",  "Tier 3 Sniper",  "UI_Icon_Renders_tier3_sniper.png",
                RewardVisualCategory.Weapon, SlotCategory.Weapon, RewardTier.Super,  false),
            ("aviator_glasses",      "Aviator Glasses", "ui_icon_aviator_glasses_easter.png",
                RewardVisualCategory.Cosmetic, SlotCategory.Cosmetic, RewardTier.Super, false),
            ("baseball_cap",         "Baseball Cap",    "ui_icon_baseball_cap_easter.png",
                RewardVisualCategory.Cosmetic, SlotCategory.Cosmetic, RewardTier.Super, false),
        };

        foreach (var s in spec)
        {
            var iconPath = $"{SpritesRoot}/{s.icon}";
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null)
            {
                Debug.LogError($"[WheelDistributionApplier] icon missing: {iconPath} — skipping {s.id}");
                continue;
            }

            Sprite wheelIcon = null;
            if (s.vc == RewardVisualCategory.Weapon)
            {
                string wheelName = System.IO.Path.GetFileNameWithoutExtension(s.icon) + "_wheel.png";
                string wheelPath = $"{SpritesRoot}/Generated/{wheelName}";
                wheelIcon = AssetDatabase.LoadAssetAtPath<Sprite>(wheelPath);
                if (wheelIcon == null)
                    Debug.LogWarning($"[WheelDistributionApplier] wheelIcon missing for {s.id}: {wheelPath} (run Vertigo → Normalize Wheel Icons)");
            }

            if (byId.TryGetValue(s.id, out var existing) && existing != null)
            {

                bool dirty = false;
                if (existing.icon != icon)         { existing.icon = icon; dirty = true; }
                if (wheelIcon != null && existing.wheelIcon != wheelIcon)
                                                   { existing.wheelIcon = wheelIcon; dirty = true; }
                if (dirty)
                {
                    EditorUtility.SetDirty(existing);
                    Debug.Log($"[WheelDistributionApplier] repaired {s.id} (icon/wheelIcon)");
                }
                continue;
            }

            var asset = ScriptableObject.CreateInstance<RewardDefinition>();
            asset.rewardId         = s.id;
            asset.displayName      = s.name;
            asset.icon             = icon;
            asset.wheelIcon        = wheelIcon;
            asset.visualCategory   = s.vc;
            asset.slotCategory     = s.sc;
            asset.minZoneTier      = s.tier;
            asset.scalesWithZone   = s.scales;
            asset.displayAsMultiplier = true;
            asset.isDeath          = false;

            var path = $"{RewardsRoot}/{FolderForVisualCategory(s.vc)}/Reward_{s.id}.asset";
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path);
            byId[s.id] = asset;
            Debug.Log($"[WheelDistributionApplier] created {path}");
        }

        return byId;
    }

    static void ApplyTierClassification(Dictionary<string, RewardDefinition> byId)
    {
        var tiers = new Dictionary<string, RewardTier>
        {
            { "gold",            RewardTier.Normal },

            { "chestGold",       RewardTier.Super },
            { "chestSuper",      RewardTier.Super },
            { "healthshot_regen",RewardTier.Super },
            { "helmet",          RewardTier.Super },
            { "bayonet_easter",  RewardTier.Super },
            { "bayonet_summer",  RewardTier.Super },
            { "aviator_glasses", RewardTier.Super },
            { "baseball_cap",    RewardTier.Super },
            { "weapon_tier3_shotgun", RewardTier.Super },
            { "weapon_tier3_smg",     RewardTier.Super },
            { "weapon_tier3_sniper",  RewardTier.Super },

            { "chestSilver",     RewardTier.Safe },
            { "chestB",          RewardTier.Safe },
            { "healthshot_neuro",RewardTier.Safe },
            { "rifle",           RewardTier.Safe },
            { "sniper",          RewardTier.Safe },
            { "knife",           RewardTier.Safe },
            { "weapon_tier2_rifle", RewardTier.Safe },
            { "weapon_tier2_mle",   RewardTier.Safe },

            { "grenade_m67",     RewardTier.Safe },
        };

        foreach (var kv in tiers)
        {
            if (!byId.TryGetValue(kv.Key, out var r)) continue;
            if (r.minZoneTier == kv.Value) continue;
            Undo.RecordObject(r, "Apply Distribution Tier");
            r.minZoneTier = kv.Value;
            EditorUtility.SetDirty(r);
            Debug.Log($"[WheelDistributionApplier] tier {r.rewardId} → {kv.Value}");
        }
    }

    static void FixGrenadeAsset(Dictionary<string, RewardDefinition> byId)
    {
        if (!byId.TryGetValue("grenade", out var r)) return;
        Undo.RecordObject(r, "Fix grenade asset");
        r.rewardId    = "grenade_m67";
        r.displayName = "M67 Grenade";
        r.minZoneTier = RewardTier.Safe;
        r.slotCategory = SlotCategory.Throwable;
        EditorUtility.SetDirty(r);
        byId["grenade_m67"] = r;
        byId.Remove("grenade");
        Debug.Log("[WheelDistributionApplier] fixed Reward_grenade → rewardId=grenade_m67, tier=Safe");
    }

    struct PlanSlice
    {
        public string rewardId;
        public int amount;
        public float weight;
        public PlanSlice(string id, int amount, float weight)
        {
            this.rewardId = id; this.amount = amount; this.weight = weight;
        }
    }

    static void BuildAndAssignSlices(Dictionary<string, RewardDefinition> byId)
    {

        var normalPlan = new[]
        {
            new PlanSlice("death",                 0,    1f),
            new PlanSlice("cash",                  500,  2f),
            new PlanSlice("gold",                  5,    1f),
            new PlanSlice("pistol",                150,  2f),
            new PlanSlice("pistol_alt",            150,  1f),
            new PlanSlice("smg",                   150,  2f),
            new PlanSlice("shotgun",               150,  2f),
            new PlanSlice("armor",                 200,  2f),
            new PlanSlice("vest",                  200,  2f),
            new PlanSlice("grenade_m26",           1,    1f),
            new PlanSlice("molotov",               1,    1f),
            new PlanSlice("chestBronze",           1,    1f),
            new PlanSlice("chestSmall",            1,    1f),
            new PlanSlice("chestStandart",         1,    1f),
        };
        var normalRules = new ZonePoolRules
        {
            slotCount = 8, allowDeath = true,
            enforceUniqueIcons = true, useWeightedRandom = true,
            useRecentIconMemory = true, recentIconPenalty = 0.25f,
            allowFallbackWhenPoolInsufficient = true,
            deathSlots = 1, currencySlots = 1, consumableSlots = 1, throwableSlots = 1,
            weaponSlots = 1, compactSlots = 1, chestSlots = 1, cosmeticSlots = 0,
            goldSlots = 1, flexSlots = 0,
        };

        var safePlan = new[]
        {

            new PlanSlice("cash",                  2000, 2f),
            new PlanSlice("weapon_tier1_shotgun",  1,    1f),
            new PlanSlice("weapon_tier2_rifle",    1,    2f),
            new PlanSlice("weapon_tier2_mle",      1,    2f),

            new PlanSlice("rifle",                 250,  2f),
            new PlanSlice("sniper",                250,  2f),
            new PlanSlice("knife",                 250,  2f),
            new PlanSlice("pistol",                100,  1f),
            new PlanSlice("smg",                   100,  1f),

            new PlanSlice("grenade_m67",           1,    2f),
            new PlanSlice("grenade_m26",           1,    1f),

            new PlanSlice("healthshot_neuro",      1,    2f),
            new PlanSlice("molotov",               1,    1f),

            new PlanSlice("chestSilver",           1,    2f),
            new PlanSlice("chestB",                1,    2f),
            new PlanSlice("chestStandart",         1,    1f),
            new PlanSlice("gold",                  10,   1f),
        };
        var safeRules = new ZonePoolRules
        {
            slotCount = 8, allowDeath = false,
            enforceUniqueIcons = true, useWeightedRandom = true,
            useRecentIconMemory = true, recentIconPenalty = 0.25f,
            allowFallbackWhenPoolInsufficient = true,
            deathSlots = 0, currencySlots = 1, consumableSlots = 1, throwableSlots = 1,
            weaponSlots = 2, compactSlots = 0, chestSlots = 1, cosmeticSlots = 0,
            goldSlots = 1, flexSlots = 1,
        };

        var superPlan = new[]
        {
            new PlanSlice("gold",                  50,   1f),
            new PlanSlice("weapon_tier3_sniper",   1,    1f),
            new PlanSlice("weapon_tier3_smg",      1,    1f),
            new PlanSlice("weapon_tier3_shotgun",  1,    1f),
            new PlanSlice("healthshot_regen",      1,    1f),
            new PlanSlice("bayonet_easter",        1,    1f),
            new PlanSlice("bayonet_summer",        1,    1f),
            new PlanSlice("aviator_glasses",       1,    1f),
            new PlanSlice("baseball_cap",          1,    1f),
            new PlanSlice("helmet",                1,    1f),
            new PlanSlice("chestGold",             1,    1f),
            new PlanSlice("chestSuper",            1,    1f),
        };
        var superRules = new ZonePoolRules
        {
            slotCount = 8, allowDeath = false,
            enforceUniqueIcons = true, useWeightedRandom = true,
            useRecentIconMemory = true, recentIconPenalty = 0.25f,
            allowFallbackWhenPoolInsufficient = true,
            deathSlots = 0, currencySlots = 0, consumableSlots = 1, throwableSlots = 0,
            weaponSlots = 3, compactSlots = 0, chestSlots = 1, cosmeticSlots = 2,
            goldSlots = 1, flexSlots = 0,
        };

        ApplyZone(byId, "NormalZone", "Nrm",  normalPlan, normalRules);
        ApplyZone(byId, "SafeZone",   "Safe", safePlan,   safeRules);
        ApplyZone(byId, "SuperZone",  "Sup",  superPlan,  superRules);
    }

    static void ApplyZone(Dictionary<string, RewardDefinition> byId,
                          string zoneAssetName, string zonePrefix,
                          PlanSlice[] plan, ZonePoolRules rules)
    {
        var zonePath = $"{ZonesRoot}/{zoneAssetName}.asset";
        var zone = AssetDatabase.LoadAssetAtPath<ZoneConfig>(zonePath);
        if (zone == null)
        {
            Debug.LogError($"[WheelDistributionApplier] missing zone asset: {zonePath}");
            return;
        }

        var slices = new List<SliceDefinition>(plan.Length);
        foreach (var p in plan)
        {
            if (!byId.TryGetValue(p.rewardId, out var reward) || reward == null)
            {
                Debug.LogError($"[WheelDistributionApplier] {zoneAssetName}: reward '{p.rewardId}' not found — slice skipped.");
                missingPlanIds.Add(p.rewardId);
                continue;
            }
            var slice = LoadOrCreateSlice(zoneAssetName, zonePrefix, p.rewardId, reward, p.amount, p.weight);
            slices.Add(slice);
        }

        Undo.RecordObject(zone, "Apply Distribution Plan");
        zone.slices = slices.ToArray();
        zone.poolRules = rules;
        EditorUtility.SetDirty(zone);
        Debug.Log($"[WheelDistributionApplier] {zoneAssetName}: {slices.Count} slices, slotCount={rules.slotCount}.");
    }

    static SliceDefinition LoadOrCreateSlice(string zoneAssetName, string zonePrefix, string rewardId,
                                             RewardDefinition reward, int amount, float weight)
    {
        var zoneFolder = zoneAssetName.Replace("Zone", "");
        var dir = $"{SlicePlansRoot}/{zoneFolder}";
        EnsureFolder(dir);
        var path = $"{dir}/Slice_Plan_{zonePrefix}_{rewardId}.asset";
        var slice = AssetDatabase.LoadAssetAtPath<SliceDefinition>(path);
        if (slice == null)
        {
            slice = ScriptableObject.CreateInstance<SliceDefinition>();
            AssetDatabase.CreateAsset(slice, path);
        }
        Undo.RecordObject(slice, "Apply Distribution Plan");
        slice.reward = reward;
        slice.amount = Mathf.Max(0, amount);
        slice.weight = Mathf.Max(0f, weight);
        EditorUtility.SetDirty(slice);
        return slice;
    }

    static string FolderForVisualCategory(RewardVisualCategory vc)
    {
        switch (vc)
        {
            case RewardVisualCategory.Death:
            case RewardVisualCategory.Cash:
            case RewardVisualCategory.Coin:      return "Currency";
            case RewardVisualCategory.Weapon:    return "Weapons";
            case RewardVisualCategory.Chest:     return "Chests";
            case RewardVisualCategory.Throwable: return "Throwables";
            case RewardVisualCategory.Consumable:return "Consumables";
            case RewardVisualCategory.Cosmetic:
            case RewardVisualCategory.Compact:   return "Cosmetics";
            default: return "Currency";
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
#endif
