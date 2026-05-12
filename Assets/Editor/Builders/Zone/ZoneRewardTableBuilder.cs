#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ZoneRewardTableBuilder
{
    const string ZonesRoot      = "Assets/ScriptableObjects/Zones";
    const string ZoneTablesRoot = "Assets/ScriptableObjects/Generated/ZoneTables";
    const string SlicePlansRoot = "Assets/ScriptableObjects/Generated/SlicePlans";

    static readonly (ZoneType type, string tablePath, string zoneAssetPath, string label)[] Zones =
    {
        (ZoneType.Normal, ZoneTablesRoot + "/ZoneRewardTable_Normal.asset", ZonesRoot + "/NormalZone.asset", "Normal"),
        (ZoneType.Safe,   ZoneTablesRoot + "/ZoneRewardTable_Safe.asset",   ZonesRoot + "/SafeZone.asset",   "Safe"),
        (ZoneType.Super,  ZoneTablesRoot + "/ZoneRewardTable_Super.asset",  ZonesRoot + "/SuperZone.asset",  "Super"),
    };

    static ZonePoolRules DefaultRulesFor(ZoneType type)
    {
        var rules = new ZonePoolRules
        {
            slotCount                       = 8,
            allowDeath                      = type == ZoneType.Normal,
            enforceUniqueIcons              = true,
            useWeightedRandom               = true,
            useRecentIconMemory             = true,
            recentIconPenalty               = 0.25f,
            allowFallbackWhenPoolInsufficient = true,
        };

        switch (type)
        {
            case ZoneType.Normal:
                rules.deathSlots      = 1;
                rules.currencySlots   = 1;
                rules.consumableSlots = 1;
                rules.throwableSlots  = 1;
                rules.weaponSlots     = 1;
                rules.compactSlots    = 1;
                rules.chestSlots      = 1;
                rules.cosmeticSlots   = 0;
                rules.goldSlots       = 1;
                rules.flexSlots       = 0;
                break;
            case ZoneType.Safe:
                rules.deathSlots      = 0;
                rules.currencySlots   = 1;
                rules.consumableSlots = 1;
                rules.throwableSlots  = 1;
                rules.weaponSlots     = 2;
                rules.compactSlots    = 0;
                rules.chestSlots      = 1;
                rules.cosmeticSlots   = 0;
                rules.goldSlots       = 1;
                rules.flexSlots       = 1;
                break;
            case ZoneType.Super:
                rules.deathSlots      = 0;
                rules.currencySlots   = 0;
                rules.consumableSlots = 1;
                rules.throwableSlots  = 0;
                rules.weaponSlots     = 3;
                rules.compactSlots    = 0;
                rules.chestSlots      = 1;
                rules.cosmeticSlots   = 2;
                rules.goldSlots       = 1;
                rules.flexSlots       = 0;
                break;
        }
        return rules;
    }

    public static void Bootstrap()
    {
        int created = 0, updated = 0, skipped = 0;

        foreach (var z in Zones)
        {
            var zone = AssetDatabase.LoadAssetAtPath<ZoneConfig>(z.zoneAssetPath);
            if (zone == null)
            {
                Debug.LogError($"[ZoneRewardTableBuilder] Missing ZoneConfig at {z.zoneAssetPath} — skipped.");
                skipped++;
                continue;
            }

            var table = AssetDatabase.LoadAssetAtPath<ZoneRewardTable>(z.tablePath);
            bool isNew = table == null;
            if (isNew)
            {
                table = ScriptableObject.CreateInstance<ZoneRewardTable>();
                AssetDatabase.CreateAsset(table, z.tablePath);
            }

            table.zoneType = z.type;
            table.targetZone = zone;
            table.poolRules = zone.poolRules.slotCount > 0
                ? zone.poolRules
                : DefaultRulesFor(z.type);
            table.entries = BuildEntriesFromZone(zone);
            EditorUtility.SetDirty(table);

            if (isNew) { created++; Debug.Log($"[ZoneRewardTableBuilder] CREATED {z.tablePath} ({table.entries.Length} entries)"); }
            else       { updated++; Debug.Log($"[ZoneRewardTableBuilder] UPDATED {z.tablePath} ({table.entries.Length} entries)"); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"=== [ZoneRewardTableBuilder] BOOTSTRAP DONE — created:{created} updated:{updated} skipped:{skipped} ===");
    }

    static ZoneRewardEntry[] BuildEntriesFromZone(ZoneConfig zone)
    {
        if (zone.slices == null || zone.slices.Length == 0)
            return new ZoneRewardEntry[0];

        var list = new List<ZoneRewardEntry>(zone.slices.Length);
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var slice = zone.slices[i];
            if (slice == null) continue;
            list.Add(new ZoneRewardEntry
            {
                reward = slice.reward,
                amount = slice.amount,
                weight = slice.weight,
            });
        }
        return list.ToArray();
    }

    public static void Validate()
    {
        int errors = 0, warnings = 0, infos = 0;

        var allTables = new List<ZoneRewardTable>(Zones.Length);

        foreach (var z in Zones)
        {
            var table = AssetDatabase.LoadAssetAtPath<ZoneRewardTable>(z.tablePath);
            if (table == null)
            {
                Debug.LogError($"[Validate:{z.label}] Table asset missing at {z.tablePath} — run Bootstrap first.");
                errors++;
                continue;
            }
            allTables.Add(table);

            if (table.targetZone == null)
            {
                Debug.LogError($"[Validate:{z.label}] targetZone is null on {z.tablePath}.");
                errors++;
                continue;
            }
            if (table.zoneType != table.targetZone.type)
            {
                Debug.LogError($"[Validate:{z.label}] zoneType {table.zoneType} ≠ targetZone.type {table.targetZone.type}.");
                errors++;
            }

            ValidatePoolRules(table, z.label, ref errors, ref warnings);

            int entryCount = table.entries != null ? table.entries.Length : 0;
            var iconCounts = new Dictionary<Sprite, int>();

            for (int i = 0; i < entryCount; i++)
            {
                var e = table.entries[i];

                if (e.reward == null)
                {
                    Debug.LogError($"[Validate:{z.label}] entry #{i} has null reward.");
                    errors++;
                    continue;
                }

                if (e.reward.icon == null)
                {
                    Debug.LogError($"[Validate:{z.label}] entry #{i} ({e.reward.rewardId}) reward.icon is null.");
                    errors++;
                }

                if (e.amount < 0)
                {
                    Debug.LogError($"[Validate:{z.label}] entry #{i} ({e.reward.rewardId}) amount {e.amount} must be ≥ 0.");
                    errors++;
                }
                if (e.weight <= 0f)
                {
                    Debug.LogWarning($"[Validate:{z.label}] entry #{i} ({e.reward.rewardId}) weight {e.weight} ≤ 0 — reward will never be sampled.");
                    warnings++;
                }

                if (e.reward.icon != null)
                    iconCounts[e.reward.icon] = iconCounts.TryGetValue(e.reward.icon, out var c) ? c + 1 : 1;
            }

            if (table.zoneType != ZoneType.Normal)
            {
                for (int i = 0; i < entryCount; i++)
                {
                    var e = table.entries[i];
                    if (e.reward != null && e.reward.isDeath)
                    {
                        Debug.LogError($"[Validate:{z.label}] death reward '{e.reward.rewardId}' in non-Normal zone — runtime filters it out, asset should remove it.");
                        errors++;
                    }
                }
            }

            if (table.zoneType == ZoneType.Normal)
            {
                bool hasDeath = false;
                for (int i = 0; i < entryCount; i++)
                {
                    var e = table.entries[i];
                    if (e.reward != null && e.reward.isDeath) { hasDeath = true; break; }
                }
                if (!hasDeath)
                {
                    Debug.LogWarning($"[Validate:{z.label}] no death reward in entries — Normal zone is expected to include death.");
                    warnings++;
                }
            }

            foreach (var kv in iconCounts)
            {
                if (kv.Value > 1)
                {
                    Debug.LogWarning($"[Validate:{z.label}] icon '{kv.Key.name}' appears {kv.Value}× — runtime unique-icon dedup will allow only one per spin face.");
                    warnings++;
                }
            }

            ValidateAgainstZoneConfig(table, z.label, ref errors, ref warnings);

            int reachable = 0;
            for (int i = 0; i < entryCount; i++)
            {
                var e = table.entries[i];
                if (e.reward != null && e.reward.icon != null && e.weight > 0f) reachable++;
            }
            Debug.Log($"[Validate:{z.label}] {entryCount} entries, {reachable} reachable.");
            infos++;
        }

        Debug.Log($"=== [ZoneRewardTableBuilder] VALIDATE DONE — errors:{errors} warnings:{warnings} infos:{infos} ===");
    }

    static void ValidatePoolRules(ZoneRewardTable table, string label, ref int errors, ref int warnings)
    {
        var actual = table.poolRules;

        const int WheelSlotCapacity = 8;
        if (actual.slotCount != WheelSlotCapacity)
        {
            Debug.LogError($"[Validate:{label}] poolRules.slotCount={actual.slotCount} ≠ wheel capacity {WheelSlotCapacity}.");
            errors++;
        }

        int sum = actual.deathSlots + actual.currencySlots + actual.consumableSlots
                + actual.throwableSlots + actual.weaponSlots + actual.compactSlots
                + actual.chestSlots + actual.cosmeticSlots + actual.goldSlots + actual.flexSlots;
        if (sum != actual.slotCount)
        {
            Debug.LogError($"[Validate:{label}] slot quotas (incl. flex) sum to {sum}, must equal slotCount {actual.slotCount}.");
            errors++;
        }

        if (!actual.allowDeath && actual.deathSlots > 0)
        {
            Debug.LogError($"[Validate:{label}] allowDeath=false but deathSlots={actual.deathSlots} — runtime EffectiveDeathSlots forces 0, so the asset is misleading.");
            errors++;
        }
        if (actual.allowDeath && actual.deathSlots == 0)
        {
            Debug.LogWarning($"[Validate:{label}] allowDeath=true but deathSlots=0 — death will never appear.");
            warnings++;
        }

        bool expectDeath = table.zoneType == ZoneType.Normal;
        if (expectDeath && !actual.allowDeath)
        {
            Debug.LogError($"[Validate:{label}] {table.zoneType} zone is expected to allow death (allowDeath=true).");
            errors++;
        }
        if (!expectDeath && actual.allowDeath)
        {
            Debug.LogError($"[Validate:{label}] {table.zoneType} zone must not allow death (allowDeath=false).");
            errors++;
        }

        if (actual.useRecentIconMemory && (actual.recentIconPenalty <= 0f || actual.recentIconPenalty > 1f))
        {
            Debug.LogWarning($"[Validate:{label}] recentIconPenalty {actual.recentIconPenalty} should be in (0, 1].");
            warnings++;
        }

        ValidateEntryCoverage(table, label, ref errors, ref warnings);

        ValidateRewardClassification(table, label, ref errors, ref warnings);
    }

    static void ValidateEntryCoverage(ZoneRewardTable table, string label, ref int errors, ref int warnings)
    {
        var counts = new System.Collections.Generic.Dictionary<SlotCategory, int>();
        if (table.entries != null)
        {
            for (int i = 0; i < table.entries.Length; i++)
            {
                var reward = table.entries[i].reward;
                if (reward == null) continue;
                var cat = SlotCategoryHelper.For(reward);
                counts[cat] = counts.TryGetValue(cat, out var c) ? c + 1 : 1;
            }
        }

        bool fallback = table.poolRules.allowFallbackWhenPoolInsufficient;
        foreach (SlotCategory cat in System.Enum.GetValues(typeof(SlotCategory)))
        {
            int quota = SlotCategoryHelper.QuotaFor(table.poolRules, cat);
            int have = counts.TryGetValue(cat, out var c) ? c : 0;
            if (quota == 0) continue;
            if (have < quota)
            {
                if (fallback)
                {
                    Debug.LogWarning($"[Validate:{label}] {cat} quota={quota} but only {have} entries — fallback allowed, will fill from any category.");
                    warnings++;
                }
                else
                {
                    Debug.LogError($"[Validate:{label}] {cat} quota={quota} but only {have} entries — sampling will fail.");
                    errors++;
                }
            }
        }
    }

    static void ValidateRewardClassification(ZoneRewardTable table, string label, ref int errors, ref int warnings)
    {
        if (table.entries == null) return;
        int zoneTier = (int)table.zoneType;
        for (int i = 0; i < table.entries.Length; i++)
        {
            var r = table.entries[i].reward;
            if (r == null) continue;

            if (r.slotCategory == SlotCategory.Unassigned && !r.isDeath)
            {
                Debug.LogError($"[Validate:{label}] reward '{r.rewardId}' has slotCategory=Unassigned — set it on the RewardDefinition asset.");
                errors++;
            }

            if ((int)r.minZoneTier > zoneTier)
            {
                Debug.LogError($"[Validate:{label}] reward '{r.rewardId}' minZoneTier={r.minZoneTier} but table zone is {table.zoneType} — reward will be filtered out at runtime.");
                errors++;
            }
        }
    }

    static void ValidateAgainstZoneConfig(ZoneRewardTable table, string label, ref int errors, ref int warnings)
    {
        var zone = table.targetZone;
        if (zone == null || zone.slices == null) return;

        var tableSigs   = new List<string>(table.entries.Length);
        var tableLookup = new Dictionary<string, int>();
        for (int i = 0; i < table.entries.Length; i++)
        {
            var e = table.entries[i];
            string sig = SignatureOf(e.reward, e.amount, e.weight);
            tableSigs.Add(sig);
            tableLookup[sig] = tableLookup.TryGetValue(sig, out var c) ? c + 1 : 1;
        }

        var zoneSigs = new List<string>(zone.slices.Length);
        var zoneLookup = new Dictionary<string, int>();
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var s = zone.slices[i];
            if (s == null)
            {
                Debug.LogError($"[Validate:{label}] ZoneConfig.slices[{i}] is null.");
                errors++;
                continue;
            }
            string sig = SignatureOf(s.reward, s.amount, s.weight);
            zoneSigs.Add(sig);
            zoneLookup[sig] = zoneLookup.TryGetValue(sig, out var c) ? c + 1 : 1;
        }

        foreach (var kv in zoneLookup)
        {
            int inTable = tableLookup.TryGetValue(kv.Key, out var c) ? c : 0;
            if (inTable < kv.Value)
            {
                Debug.LogError($"[Validate:{label}] ZoneConfig has slice '{kv.Key}' ({kv.Value}×) but table only has {inTable} — bootstrap drift.");
                errors++;
            }
        }

        foreach (var kv in tableLookup)
        {
            int inZone = zoneLookup.TryGetValue(kv.Key, out var c) ? c : 0;
            if (inZone < kv.Value)
            {
                Debug.LogError($"[Validate:{label}] table has entry '{kv.Key}' ({kv.Value}×) but ZoneConfig only has {inZone} — bootstrap drift.");
                errors++;
            }
        }

        bool orderMatches = tableSigs.Count == zoneSigs.Count;
        if (orderMatches)
            for (int i = 0; i < tableSigs.Count; i++)
                if (tableSigs[i] != zoneSigs[i]) { orderMatches = false; break; }
        if (!orderMatches)
        {
            Debug.LogWarning($"[Validate:{label}] entries[] order differs from ZoneConfig.slices[] order — content matches, position differs.");
            warnings++;
        }
    }

    static string SignatureOf(RewardDefinition reward, int amount, float weight)
    {
        string id = reward != null ? reward.rewardId ?? "<noid>" : "<null>";
        return $"{id}|amount={amount}|weight={weight}";
    }

    public static void SyncZoneConfigFromTables()
    {

        var loaded = new List<(ZoneRewardTable table, ZoneConfig zone, string label)>();
        foreach (var z in Zones)
        {
            var table = AssetDatabase.LoadAssetAtPath<ZoneRewardTable>(z.tablePath);
            if (table == null)
            {
                Debug.LogError($"[Sync] Missing table at {z.tablePath} — run Bootstrap first. Aborted.");
                return;
            }
            if (table.targetZone == null)
            {
                Debug.LogError($"[Sync:{z.label}] table.targetZone is null. Aborted.");
                return;
            }

            if ((table.entries == null || table.entries.Length == 0)
                && table.targetZone.slices != null && table.targetZone.slices.Length > 0)
            {
                Debug.LogError($"[Sync:{z.label}] table has 0 entries but ZoneConfig has {table.targetZone.slices.Length} slices — refusing to wipe. Aborted.");
                return;
            }
            loaded.Add((table, table.targetZone, z.label));
        }

        Debug.Log("[Sync] PRE-SYNC SNAPSHOT (current ZoneConfig.slices[]):");
        foreach (var (table, zone, label) in loaded)
            LogZoneSnapshot(label, zone);

        int totalCreated = 0, totalReused = 0;
        foreach (var (table, zone, label) in loaded)
        {
            int created, reused;
            var newSlices = ResolveSlicesFromTable(table, zone, label, out created, out reused);
            zone.slices = newSlices;
            zone.poolRules = table.poolRules;
            EditorUtility.SetDirty(zone);
            Debug.Log($"[Sync:{label}] wrote {newSlices.Length} slices  (reused:{reused}  created:{created})  +  poolRules mirrored");
            totalCreated += created;
            totalReused += reused;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Sync] Total: reused:{totalReused}  created:{totalCreated}");

        Validate();
    }

    static void LogZoneSnapshot(string label, ZoneConfig zone)
    {
        if (zone == null || zone.slices == null) { Debug.Log($"  {label}: (null)"); return; }
        var sb = new StringBuilder();
        sb.AppendLine($"  {label} — {zone.slices.Length} slices:");
        for (int i = 0; i < zone.slices.Length; i++)
        {
            var s = zone.slices[i];
            string path = s != null ? AssetDatabase.GetAssetPath(s) : "<null>";
            string sig  = s != null ? SignatureOf(s.reward, s.amount, s.weight) : "<null>";
            sb.AppendFormat("    [{0,2}] {1,-50} {2}\n", i, System.IO.Path.GetFileNameWithoutExtension(path), sig);
        }
        Debug.Log(sb.ToString());
    }

    static SliceDefinition[] ResolveSlicesFromTable(ZoneRewardTable table, ZoneConfig zone, string label, out int created, out int reused)
    {
        created = 0;
        reused = 0;

        var currentMap = new Dictionary<string, SliceDefinition>();
        if (zone.slices != null)
        {
            foreach (var s in zone.slices)
            {
                if (s == null) continue;
                string sig = SignatureOf(s.reward, s.amount, s.weight);
                if (!currentMap.ContainsKey(sig)) currentMap[sig] = s;
            }
        }

        var result = new SliceDefinition[table.entries != null ? table.entries.Length : 0];
        for (int i = 0; i < result.Length; i++)
        {
            var e = table.entries[i];
            string sig = SignatureOf(e.reward, e.amount, e.weight);

            if (currentMap.TryGetValue(sig, out var inZone))
            {
                result[i] = inZone;
                reused++;
                continue;
            }

            var anyMatch = FindAnySliceWithSignature(e.reward, e.amount, e.weight);
            if (anyMatch != null)
            {
                result[i] = anyMatch;
                reused++;
                continue;
            }

            result[i] = CreateNewSliceForEntry(e, table.zoneType, label);
            created++;
        }
        return result;
    }

    static SliceDefinition FindAnySliceWithSignature(RewardDefinition reward, int amount, float weight)
    {
        if (reward == null) return null;
        string[] guids = AssetDatabase.FindAssets("t:SliceDefinition");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var slice = AssetDatabase.LoadAssetAtPath<SliceDefinition>(path);
            if (slice == null) continue;
            if (slice.reward != reward) continue;
            if (slice.amount != amount) continue;
            if (!Mathf.Approximately(slice.weight, weight)) continue;
            return slice;
        }
        return null;
    }

    static SliceDefinition CreateNewSliceForEntry(ZoneRewardEntry e, ZoneType zoneType, string label)
    {
        string prefix = zoneType == ZoneType.Normal ? "Nrm"
                      : zoneType == ZoneType.Safe   ? "Safe"
                      : "Sup";
        string zoneFolder = zoneType == ZoneType.Normal ? "Normal"
                          : zoneType == ZoneType.Safe   ? "Safe"
                          : "Super";
        string dir = $"{SlicePlansRoot}/{zoneFolder}";
        WheelSceneSetup.EnsureDir(dir);
        string rid = e.reward != null ? e.reward.rewardId : "noId";
        string baseName = $"Slice_{prefix}_{rid}_synced";
        string path = $"{dir}/{baseName}.asset";
        int n = 1;
        while (AssetDatabase.LoadAssetAtPath<SliceDefinition>(path) != null)
        {
            path = $"{dir}/{baseName}_{n}.asset";
            n++;
        }

        var slice = ScriptableObject.CreateInstance<SliceDefinition>();
        slice.reward = e.reward;
        slice.amount = e.amount;
        slice.weight = e.weight;
        AssetDatabase.CreateAsset(slice, path);
        Debug.Log($"[Sync:{label}] CREATED new slice {path} (no signature match in project)");
        return slice;
    }

    public static void DiffReport()
    {
        foreach (var z in Zones)
        {
            var table = AssetDatabase.LoadAssetAtPath<ZoneRewardTable>(z.tablePath);
            var zone = AssetDatabase.LoadAssetAtPath<ZoneConfig>(z.zoneAssetPath);

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"────── {z.label.ToUpper()} ZONE ──────");

            int tableCount = table != null && table.entries != null ? table.entries.Length : 0;
            int zoneCount  = zone  != null && zone.slices  != null ? zone.slices.Length  : 0;
            sb.AppendLine($"  ZoneConfig.slices: {zoneCount}    ZoneRewardTable.entries: {tableCount}");
            sb.AppendLine();

            if (table != null)
            {
                var actual = table.poolRules;
                var seed = DefaultRulesFor(z.type);
                sb.AppendLine("  POOL RULES (table value | default seed):");
                AppendRuleRow(sb, "slotCount",          actual.slotCount,          seed.slotCount);
                AppendRuleRow(sb, "allowDeath",         actual.allowDeath,         seed.allowDeath);
                AppendRuleRow(sb, "enforceUniqueIcons", actual.enforceUniqueIcons, seed.enforceUniqueIcons);
                AppendRuleRow(sb, "useWeightedRandom",  actual.useWeightedRandom,  seed.useWeightedRandom);
                sb.AppendLine("  SLOT QUOTAS (table | default seed):");
                AppendRuleRow(sb, "deathSlots",         actual.deathSlots,         seed.deathSlots);
                AppendRuleRow(sb, "currencySlots",      actual.currencySlots,      seed.currencySlots);
                AppendRuleRow(sb, "consumableSlots",    actual.consumableSlots,    seed.consumableSlots);
                AppendRuleRow(sb, "throwableSlots",     actual.throwableSlots,     seed.throwableSlots);
                AppendRuleRow(sb, "weaponSlots",        actual.weaponSlots,        seed.weaponSlots);
                AppendRuleRow(sb, "compactSlots",       actual.compactSlots,       seed.compactSlots);
                AppendRuleRow(sb, "chestSlots",         actual.chestSlots,         seed.chestSlots);
                AppendRuleRow(sb, "cosmeticSlots",      actual.cosmeticSlots,      seed.cosmeticSlots);
                AppendRuleRow(sb, "goldSlots",          actual.goldSlots,          seed.goldSlots);
                AppendRuleRow(sb, "flexSlots",          actual.flexSlots,          seed.flexSlots);
                int sum = actual.deathSlots + actual.currencySlots + actual.consumableSlots
                        + actual.throwableSlots + actual.weaponSlots + actual.compactSlots
                        + actual.chestSlots + actual.cosmeticSlots + actual.goldSlots + actual.flexSlots;
                sb.AppendFormat("    {0} sum                       {1,-12} | {2}\n",
                    sum == actual.slotCount ? "✓" : "✗", sum, actual.slotCount);
                sb.AppendLine("  MEMORY / FALLBACK:");
                AppendRuleRow(sb, "useRecentIconMemory",       actual.useRecentIconMemory,             seed.useRecentIconMemory);
                AppendRuleRow(sb, "recentIconPenalty",         actual.recentIconPenalty,               seed.recentIconPenalty);
                AppendRuleRow(sb, "allowFallbackOnInsufficient", actual.allowFallbackWhenPoolInsufficient, seed.allowFallbackWhenPoolInsufficient);
                sb.AppendLine();
            }

            sb.AppendFormat("  {0,-3} {1,-25} {2,-40} {3,8} {4,8}\n", "#", "REWARD", "SPRITE", "AMOUNT", "WEIGHT");
            sb.AppendLine("  ────────────────────────────────────────────────────────────────────────────────");

            int rows = System.Math.Max(tableCount, zoneCount);
            for (int i = 0; i < rows; i++)
            {
                string fromZone = i < zoneCount  ? FormatRow(zone.slices[i])  : "(missing in ZoneConfig)";
                string fromTbl  = i < tableCount ? FormatRow(table.entries[i]) : "(missing in table)";
                bool match = fromZone == fromTbl;
                sb.AppendFormat("  {0,-3} {1} {2}\n", i, match ? "✓" : "✗", fromTbl);
                if (!match)
                    sb.AppendFormat("       └ ZoneConfig: {0}\n", fromZone);
            }

            Debug.Log(sb.ToString());
        }
    }

    static string FormatRow(SliceDefinition s)
    {
        if (s == null) return "(null slice)";
        var rid = s.reward != null ? s.reward.rewardId : "<no reward>";
        var spr = s.reward != null && s.reward.icon != null ? s.reward.icon.name : "<no icon>";
        return string.Format("{0,-25} {1,-40} {2,8} {3,8}", rid, spr, s.amount, s.weight);
    }

    static string FormatRow(ZoneRewardEntry e)
    {
        var rid = e.reward != null ? e.reward.rewardId : "<no reward>";
        var spr = e.reward != null && e.reward.icon != null ? e.reward.icon.name : "<no icon>";
        return string.Format("{0,-25} {1,-40} {2,8} {3,8}", rid, spr, e.amount, e.weight);
    }

    static void AppendRuleRow(StringBuilder sb, string name, object actual, object seed)
    {
        bool match = object.Equals(actual, seed);
        sb.AppendFormat("    {0} {1,-22} {2,-12} | {3}\n",
            match ? "=" : "≠",
            name,
            actual,
            seed);
    }
}
#endif
