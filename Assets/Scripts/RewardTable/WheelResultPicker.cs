using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
public class WheelResultPicker
{
    public struct ComputedSlot
    {
        public ZoneRewardEntry entry;
        public int final_amount;
    }

    public const int WheelSlotCapacity = 8;

    private readonly ComputedSlot[] wheel_slots = new ComputedSlot[WheelSlotCapacity];
    private int slot_count;

    private readonly System.Random rng;
    private readonly CurrencyRules currency_rules;
    private readonly RewardTableConfig reward_table;

    private readonly RewardCandidateQuery candidates;
    //i keep these lists as fields because this picker runs every zone load
    //clearing them felt simpler than making new ones every time
    private readonly List<ZoneRewardEntry> picked_list = new List<ZoneRewardEntry>(WheelSlotCapacity);
    private readonly HashSet<Sprite> picked_icons = new HashSet<Sprite>();

    private readonly HashSet<string> picked_families = new HashSet<string>();
    private readonly Dictionary<string, List<ZoneRewardEntry>> family_buckets = new Dictionary<string, List<ZoneRewardEntry>>();
    private readonly List<string> family_keys = new List<string>();
    private readonly List<ZoneRewardEntry> singletons = new List<ZoneRewardEntry>();

    public ComputedSlot[] WheelSlots => wheel_slots;

    public WheelResultPicker(int seed, CurrencyRules currency_rules_src, RewardTableConfig reward_table_src)
    {
        //seed 0 is normal random, other seeds are for replaying a bad draw when i need to
        if (seed == 0)
        {
            rng = new System.Random();
        }
        else
        {
            rng = new System.Random(seed);
        }
        currency_rules = currency_rules_src;
        this.reward_table = reward_table_src;
        candidates = new RewardCandidateQuery(currency_rules_src);
    }

    public bool LoadZone(WheelVisual zone, int zone_idx)
    {
        if (SampleByCategoryQuotas(zone, zone_idx))
        {
            slot_count = WheelSlotCapacity;
            return true;
        }
        string msg = $"reward table quota sampling failed for tier {zone.tier}, config invariant broken";
        Debug.LogError(msg);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        throw new System.InvalidOperationException(msg);
#else
        slot_count = 0;
        return false;
#endif
    }

    public SpinResult Spin()
    {
        if (slot_count != 0)
        {
            int chosen = rng.Next(0, slot_count);

            ComputedSlot slot = wheel_slots[chosen];
            SpinResult result;
            result.slice_idx = chosen;
            result.is_death = slot.entry.reward.isDeath;
            result.amount = slot.final_amount;
            result.is_valid = true;
            return result;
        }
        return SpinResult.Invalid;
    }

    public ComputedSlot GetSlot(int index)
    {
        return wheel_slots[index];
    }

    private bool SampleByCategoryQuotas(WheelVisual zone, int zone_idx)
    {
        var zt = reward_table.GetForTier(zone.tier);
        var rules = zt.poolRules;

        candidates.Build(zt, new RewardCandidateQuery.Context
        {
            zone_tier = zone.tier,
            allow_death = rules.allowDeath,
        });

        picked_list.Clear();
        picked_icons.Clear();

        //i fill 8 slots by category first, in this order because it is easier to see in config
        //each category takes its own quota from the zone table
        //i reset picked_families per category, same family can show in another category but not twice inside one
        for (int c = 0; c < RewardCandidateQuery.CategoryOrder.Length; c++)
        {
            picked_families.Clear();

            SlotCategory cat = RewardCandidateQuery.CategoryOrder[c];
            int quota = SlotCategoryHelper.QuotaFor(zt.quotas, rules.allowDeath, cat);
            if (quota > 0)
            {
                var pool = candidates.ForCategory(cat);
                for (int i = 0; i < quota; i++)
                {
                    var pick = PickFromPool(pool, rules);
                    if (pick != null)
                    {
                        picked_list.Add(pick);
                        picked_icons.Add(pick.reward.icon);
                        if (!string.IsNullOrEmpty(pick.reward.visualFamily))
                        {
                            picked_families.Add(pick.reward.visualFamily);
                        }
                    }
                }
            }
        }

        if (picked_list.Count != WheelSlotCapacity)
        {
            return false;
        }

        //after that they are grouped too neatly, so i shuffle them
        //otherwise coins sit together too often and the wheel looks fake
        for (int i = picked_list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            var tmp = picked_list[i];
            picked_list[i] = picked_list[j];
            picked_list[j] = tmp;
        }
        for (int i = 0; i < WheelSlotCapacity; i++)
        {
            var entry = picked_list[i];
            int final_amount = 0;
            if (!entry.reward.isDeath)
            {
                final_amount = currency_rules.AmountForReward(entry.reward, zone_idx);
            }
            wheel_slots[i] = new ComputedSlot
            {
                entry = entry,
                final_amount = final_amount
            };
        }

        return true;
    }

    //i didnt want a weapon family with 5 items to beat a single coin just because it has more entries
    //so first i pick family or loose item, then pick inside that family
    //it is not perfect math maybe, but it feels fairer for this case
    private ZoneRewardEntry PickFromPool(IReadOnlyList<ZoneRewardEntry> pool, in RewardTableConfig.PoolRules rules)
    {
        BuildOuters(pool, rules);
        int outer_count = family_keys.Count + singletons.Count;
        if (outer_count > 0)
        {
            return PickFromOuters(outer_count);
        }
        return null;
    }

    private void BuildOuters(IReadOnlyList<ZoneRewardEntry> pool, in RewardTableConfig.PoolRules rules)
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
            if (!IsEligible(entry))
            {
                continue;
            }

            string family = entry.reward.visualFamily;
            if (string.IsNullOrEmpty(family))
            {
                singletons.Add(entry);
                continue;
            }

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

    private ZoneRewardEntry PickFromOuters(int outer_count)
    {
        int outer_idx = rng.Next(0, outer_count);
        if (outer_idx < family_keys.Count)
        {
            var bucket = family_buckets[family_keys[outer_idx]];
            return bucket[rng.Next(0, bucket.Count)];
        }
        //families come first in outer_idx, so after that i land in singletons
        return singletons[outer_idx - family_keys.Count];
    }

    private bool IsEligible(ZoneRewardEntry entry)
    {
        if (picked_icons.Contains(entry.reward.icon))
        {
            return false;
        }
        if (IsFamilyAlreadyPicked(entry))
        {
            return false;
        }
        return true;
    }

    private bool IsFamilyAlreadyPicked(ZoneRewardEntry entry)
    {
        string family = entry.reward.visualFamily;
        return !string.IsNullOrEmpty(family) && picked_families.Contains(family);
    }
}

public struct SpinResult
{
    public int slice_idx;
    public int amount;
    public bool is_death;
    public bool is_valid;

    public static SpinResult Invalid => new SpinResult { is_valid = false };
}

public sealed class RewardCandidateQuery
{
    public struct Context
    {
        public RewardTier zone_tier;
        public bool allow_death;
    }

    //this is the category order i use everywhere in this picker
    public static readonly SlotCategory[] CategoryOrder =
    {
        SlotCategory.Death,
        SlotCategory.Currency,
        SlotCategory.Other,
        SlotCategory.AllCards,
        SlotCategory.Special,
    };

    private const int CategoryBucketCount = (int)SlotCategory.Special + 1;
    //i use the enum int as bucket index here, no need for dictionary
    private readonly List<ZoneRewardEntry>[] by_category;
    private readonly ZoneRewardEntry cash_entry;
    private readonly ZoneRewardEntry gold_entry;

    public RewardCandidateQuery(CurrencyRules currency_rules_src)
    {
        by_category = new List<ZoneRewardEntry>[CategoryBucketCount];
        for (int i = 0; i < by_category.Length; i++)
        {
            by_category[i] = new List<ZoneRewardEntry>(4);
        }
        cash_entry = MakeCurrencyEntry(currency_rules_src.CashReward);
        gold_entry = MakeCurrencyEntry(currency_rules_src.GoldReward);
    }

    private static ZoneRewardEntry MakeCurrencyEntry(RewardDefinition reward)
    {
        if (reward == null)
        {
            return null;
        }
        return new ZoneRewardEntry { reward = reward };
    }

    public IReadOnlyList<ZoneRewardEntry> ForCategory(SlotCategory cat)
    {
        return by_category[(int)cat];
    }

    public void Build(RewardTableConfig.ZoneTable zt, in Context ctx)
    {
        for (int i = 0; i < by_category.Length; i++)
        {
            by_category[i].Clear();
        }

        for (int c = 0; c < CategoryOrder.Length; c++)
        {
            SlotCategory cat = CategoryOrder[c];

            //currency is weird because cash gold are not written in the zone table
            //i make them once from currency rules and then treat them like normal entries
            if (cat == SlotCategory.Currency)
            {
                if (cash_entry != null && IsEligible(cash_entry, ctx))
                {
                    by_category[(int)cat].Add(cash_entry);
                }
                if (gold_entry != null && IsEligible(gold_entry, ctx))
                {
                    by_category[(int)cat].Add(gold_entry);
                }
                continue;
            }

            var entries = SlotCategoryHelper.PoolEntriesFor(zt, cat);
            if (entries == null)
            {
                continue;
            }
            for (int i = 0; i < entries.Length; i++)
            {
                if (IsEligible(entries[i], ctx))
                {
                    by_category[(int)cat].Add(entries[i]);
                }
            }
        }
    }

    private static bool IsEligible(ZoneRewardEntry entry, in Context ctx)
    {
        if (entry == null || entry.reward == null)
        {
            return false;
        }
        var r = entry.reward;
        if ((int)r.minZoneTier > (int)ctx.zone_tier)
        {
            return false;
        }
        if (r.isDeath && !ctx.allow_death)
        {
            return false;
        }
        return true;
    }
}
}
