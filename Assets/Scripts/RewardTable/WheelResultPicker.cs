using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
internal class WheelResultPicker
{
    internal struct ComputedSlot
    {
        internal ZoneRewardEntry entry;
        internal int final_amount;
    }

    private const int WheelSlotCapacity = 8;
    private static SlotCategory[] CategoryOrder =
    {
        SlotCategory.Death,
        SlotCategory.Currency,
        SlotCategory.Other,
        SlotCategory.AllCards,
        SlotCategory.Special,
    };

    private ComputedSlot[] wheel_slots;

    private System.Random rng = new System.Random();
    private CurrencyRules currency_rules;
    private RewardTableConfig reward_table;

    private RewardCandidateQuery candidates;
    private RewardPoolPicker pool_picker;
    private List<ZoneRewardEntry> picked_list;

    internal ComputedSlot[] WheelSlots
    {
        get
        {
            return wheel_slots;
        }
    }

    internal WheelResultPicker(CurrencyRules currency_rules_src, RewardTableConfig reward_table_src)
    {
        currency_rules = currency_rules_src;
        this.reward_table = reward_table_src;
        wheel_slots = new ComputedSlot[WheelSlotCapacity];
        picked_list = new List<ZoneRewardEntry>(WheelSlotCapacity);
        candidates = new RewardCandidateQuery(currency_rules_src);
        pool_picker = new RewardPoolPicker(rng);
    }

    internal void LoadZone(WheelVisual zone, int zone_idx, bool revive = false)
    {
        SampleByCategoryQuotas(zone, zone_idx, revive);
    }

    internal SpinResult Spin()
    {
        int chosen = rng.Next(0, wheel_slots.Length);

        ComputedSlot slot = wheel_slots[chosen];
        SpinResult result;
        result.slice_idx = chosen;
        result.entry = slot.entry;
        result.amount = slot.final_amount;
        return result;
    }

    private void SampleByCategoryQuotas(WheelVisual zone, int zone_idx, bool revive)
    {
        RewardTableConfig.ZoneTable zone_table = reward_table.GetForTier(zone.tier, revive);
        RewardTableConfig.PoolRules rules = zone_table.poolRules;

        candidates.Build(zone_table, zone.tier, rules.allowDeath, CategoryOrder);

        picked_list.Clear();
        pool_picker.BeginZone();

        for (int c = 0; c < CategoryOrder.Length; c++)
        {
            pool_picker.BeginCategory();

            SlotCategory cat = CategoryOrder[c];
            int quota = SlotCategoryHelper.QuotaFor(zone_table.quotas, rules.allowDeath, cat);
            if (quota > 0)
            {
                List<ZoneRewardEntry> pool = candidates.GetPool(cat);
                for (int i = 0; i < quota; i++)
                {
                    ZoneRewardEntry pick = pool_picker.Pick(pool);
                    picked_list.Add(pick);
                    pool_picker.Accept(pick);
                }
            }
        }

        ShufflePickedList();
        for (int i = 0; i < wheel_slots.Length; i++)
        {
            ZoneRewardEntry entry = picked_list[i];
            wheel_slots[i] = new ComputedSlot
            {
                entry = entry,
                final_amount = entry.reward.isDeath ? 0 : currency_rules.AmountForReward(entry.reward, zone_idx)
            };
        }
    }

    private void ShufflePickedList()
    {
        for (int i = picked_list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            var tmp = picked_list[i];
            picked_list[i] = picked_list[j];
            picked_list[j] = tmp;
        }
    }

    private class RewardPoolPicker
    {
        private System.Random rng;
        private HashSet<Sprite> picked_icons = new HashSet<Sprite>();
        private HashSet<string> picked_families = new HashSet<string>();
        private Dictionary<string, List<ZoneRewardEntry>> family_buckets = new Dictionary<string, List<ZoneRewardEntry>>();
        private List<string> family_keys = new List<string>();
        private List<ZoneRewardEntry> singletons = new List<ZoneRewardEntry>();

        internal RewardPoolPicker(System.Random rng)
        {
            this.rng = rng;
        }

        internal void BeginZone()
        {
            picked_icons.Clear();
        }

        internal void BeginCategory()
        {
            picked_families.Clear();
        }

        internal ZoneRewardEntry Pick(List<ZoneRewardEntry> pool)
        {
            BuildOuters(pool);
            int outer_count = family_keys.Count + singletons.Count;

            int outer_idx = rng.Next(0, outer_count);
            if (outer_idx < family_keys.Count)
            {
                List<ZoneRewardEntry> bucket = family_buckets[family_keys[outer_idx]];
                return bucket[rng.Next(0, bucket.Count)];
            }
            return singletons[outer_idx - family_keys.Count];
        }

        internal void Accept(ZoneRewardEntry entry)
        {
            picked_icons.Add(entry.reward.icon);
            if (HasRewardFamily(entry.reward))
            {
                picked_families.Add(entry.reward.visualFamily);
            }
        }

        private void BuildOuters(List<ZoneRewardEntry> pool)
        {
            foreach (var kv in family_buckets)
            {
                kv.Value.Clear();
            }
            family_keys.Clear();
            singletons.Clear();

            for (int i = 0; i < pool.Count; i++)
            {
                ZoneRewardEntry entry = pool[i];
                RewardDefinition reward = entry.reward;
                if (!CanPickWithoutDuplicate(reward))
                {
                    continue;
                }

                if (!HasRewardFamily(reward))
                {
                    singletons.Add(entry);
                    continue;
                }

                string family = reward.visualFamily;
                if (!family_buckets.TryGetValue(family, out var bucket))
                {
                    bucket = new List<ZoneRewardEntry>();
                    family_buckets[family] = bucket;
                }
                if (bucket.Count == 0)
                {
                    family_keys.Add(family);
                }
                bucket.Add(entry);
            }
        }

        private bool CanPickWithoutDuplicate(RewardDefinition reward)
        {
            if (picked_icons.Contains(reward.icon))
            {
                return false;
            }
            if (HasRewardFamily(reward) && picked_families.Contains(reward.visualFamily))
            {
                return false;
            }
            return true;
        }

        private static bool HasRewardFamily(RewardDefinition reward)
        {
            return !string.IsNullOrEmpty(reward.visualFamily);
        }
    }
}

internal struct SpinResult
{
    internal int slice_idx;
    internal ZoneRewardEntry entry;
    internal int amount;

    internal bool IsDeath
    {
        get
        {
            return entry.reward.isDeath;
        }
    }
}

internal class RewardCandidateQuery
{
    private const int CategoryBucketCount = (int)SlotCategory.Special + 1;
    private List<ZoneRewardEntry>[] by_category;
    private ZoneRewardEntry cash_entry;
    private ZoneRewardEntry gold_entry;

    internal RewardCandidateQuery(CurrencyRules currency_rules_src)
    {
        by_category = new List<ZoneRewardEntry>[CategoryBucketCount];
        for (int i = 0; i < by_category.Length; i++)
        {
            by_category[i] = new List<ZoneRewardEntry>();
        }
        cash_entry = new ZoneRewardEntry { reward = currency_rules_src.CashReward };
        gold_entry = new ZoneRewardEntry { reward = currency_rules_src.GoldReward };
    }

    internal List<ZoneRewardEntry> GetPool(SlotCategory cat)
    {
        return by_category[(int)cat];
    }

    internal void Build(RewardTableConfig.ZoneTable zt, RewardTier zone_tier, bool allow_death, SlotCategory[] category_order)
    {
        for (int i = 0; i < by_category.Length; i++)
        {
            by_category[i].Clear();
        }

        for (int c = 0; c < category_order.Length; c++)
        {
            SlotCategory cat = category_order[c];

            if (cat == SlotCategory.Currency)
            {
                AddCandidateIfEligible(cat, cash_entry, zone_tier, allow_death);
                AddCandidateIfEligible(cat, gold_entry, zone_tier, allow_death);
                continue;
            }

            var entries = SlotCategoryHelper.PoolEntriesFor(zt, cat);
            for (int i = 0; i < entries.Length; i++)
            {
                AddCandidateIfEligible(cat, entries[i], zone_tier, allow_death);
            }
        }
    }

    private void AddCandidateIfEligible(SlotCategory cat, ZoneRewardEntry entry, RewardTier zone_tier, bool allow_death)
    {
        if (entry.reward.CanAppearIn(zone_tier, allow_death))
        {
            by_category[(int)cat].Add(entry);
        }
    }
}
}
