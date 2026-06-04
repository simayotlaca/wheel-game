using System;
using System.Collections.Generic;

namespace VertigoWheel
{
internal class MetaProgressModel
{
    internal struct MetaChunk
    {
        internal int card_index;
        internal int amount;
        internal bool completes_card;
    }

    internal struct ProgressAllocation
    {
        internal List<MetaChunk> meta_chunks;
        internal int overflow_amount;
    }

    internal struct ProgressApplied
    {
        internal bool valid;
        internal int old_total;
        internal int new_total;
        internal int limit;
    }

    private class CardState
    {
        internal RewardDefinition point_reward;
        internal int chain_order;
        internal int limit;
        internal int current_total;
        internal int committed_total;
    }

    private EntryView[] entries;
    private CardState[] cards;
    private Dictionary<RewardDefinition, int> reward_to_card_index;
    private Dictionary<string, int[]> family_to_chain;
    private RewardDefinition overflow_reward;

    internal int CardCount
    {
        get
        {
            return cards.Length;
        }
    }

    internal RewardDefinition OverflowReward
    {
        get
        {
            return overflow_reward;
        }
    }

    internal MetaProgressModel(EntryView[] src_entries, RewardDefinition overflow_reward_src)
    {
        overflow_reward = overflow_reward_src;

        int n = src_entries.Length;
        entries = new EntryView[n];
        cards = new CardState[n];
        for (int i = 0; i < n; i++)
        {
            EntryView e = src_entries[i];
            entries[i] = e;
            int starting_total = e.initial_amount;
            cards[i] = new CardState
            {
                point_reward = e.point_reward,
                chain_order = e.chain_order,
                limit = e.limit,
                current_total = starting_total,
                committed_total = starting_total,
            };
        }

        reward_to_card_index = new Dictionary<RewardDefinition, int>(n);
        var family_groups = new Dictionary<string, List<int>>();
        for (int i = 0; i < n; i++)
        {
            CardState c = cards[i];
            if (!reward_to_card_index.ContainsKey(c.point_reward))
            {
                reward_to_card_index[c.point_reward] = i;
            }
            string family = GetFamilyKey(c.point_reward);
            if (!string.IsNullOrEmpty(family))
            {
                if (!family_groups.TryGetValue(family, out List<int> list))
                {
                    list = new List<int>();
                    family_groups[family] = list;
                }
                list.Add(i);
            }
        }

        family_to_chain = new Dictionary<string, int[]>(family_groups.Count);
        foreach (var kv in family_groups)
        {
            int[] arr = kv.Value.ToArray();
            Array.Sort(arr, CompareByChainOrder);
            family_to_chain[kv.Key] = arr;
        }
    }

    private int CompareByChainOrder(int a, int b)
    {
        int cmp = cards[a].chain_order.CompareTo(cards[b].chain_order);
        if (cmp == 0)
        {
            return a.CompareTo(b);
        }
        return cmp;
    }

    private static string GetFamilyKey(RewardDefinition reward)
    {
        return reward != null ? reward.visualFamily : string.Empty;
    }

    internal EntryView GetEntry(int index)
    {
        return entries[index];
    }

    internal int GetCurrentTotal(int index)
    {
        return cards[index].current_total;
    }

    internal int GetLimit(int index)
    {
        return cards[index].limit;
    }

    private bool TryFindCardIndex(RewardDefinition reward, out int index)
    {
        if (reward != null && reward_to_card_index.TryGetValue(reward, out index))
        {
            return true;
        }
        index = 0;
        return false;
    }

    internal bool IsRewardTracked(RewardDefinition reward)
    {
        return reward != null && reward_to_card_index.ContainsKey(reward);
    }

    internal bool HasUnbankedProgress
    {
        get
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].current_total > cards[i].committed_total)
                {
                    return true;
                }
            }
            return false;
        }
    }

    private int GetRemainingCapacity(int index)
    {
        int remaining = cards[index].limit - cards[index].current_total;
        if (remaining < 0)
        {
            remaining = 0;
        }
        return remaining;
    }

    internal ProgressAllocation AllocateProgress(RewardDefinition reward, int amount)
    {
        var alloc = new ProgressAllocation { meta_chunks = new List<MetaChunk>(), overflow_amount = 0 };

        int root_idx;
        if (!TryFindCardIndex(reward, out root_idx))
        {
            alloc.overflow_amount = amount;
            return alloc;
        }

        string family = GetFamilyKey(cards[root_idx].point_reward);
        int remaining = amount;

        if (!string.IsNullOrEmpty(family) && family_to_chain.TryGetValue(family, out int[] chain))
        {
            for (int i = 0; i < chain.Length && remaining > 0; i++)
            {
                remaining = AllocateToSingle(chain[i], remaining, alloc);
            }
        }
        else
        {
            remaining = AllocateToSingle(root_idx, remaining, alloc);
        }

        alloc.overflow_amount = remaining;
        return alloc;
    }

    private int AllocateToSingle(int idx, int remaining, ProgressAllocation alloc)
    {
        int cap = GetRemainingCapacity(idx);
        if (cap <= 0)
        {
            return remaining;
        }
        int take = Math.Min(remaining, cap);
        alloc.meta_chunks.Add(new MetaChunk
        {
            card_index = idx,
            amount = take,
            completes_card = take >= cap
        });
        return remaining - take;
    }

    internal ProgressApplied ApplyProgress(int index, int delta)
    {
        ProgressApplied result = default;
        CardState c = cards[index];
        if (c.current_total >= c.limit)
        {
            return result;
        }
        int old_total = c.current_total;
        int new_total = old_total + delta;
        if (new_total > c.limit)
        {
            new_total = c.limit;
        }
        c.current_total = new_total;

        result.valid = true;
        result.old_total = old_total;
        result.new_total = new_total;
        result.limit = c.limit;
        return result;
    }

    internal void Commit()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].committed_total = cards[i].current_total;
        }
    }

    internal void Revert()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].current_total = cards[i].committed_total;
        }
    }
}
}
