using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
[Serializable]
public class ProgressCardEntry
{
    [Min(0)] public int chain_order;
    public Sprite target_icon;
    [Min(1)] public int limit;
    [Min(0)] public int initial_amount;
    public string title_text;
    public string tier_text;
    public string subtitle_text;
}


[Serializable]
public class ProgressFamilyPool
{
    public RewardDefinition point_reward;

    public List<ProgressCardEntry> cards = new();
}

internal struct EntryView
{
    internal RewardDefinition point_reward;
    internal int chain_order;
    internal Sprite target_icon;
    internal int limit;
    internal int initial_amount;
    internal string title_text;
    internal string tier_text;
    internal string subtitle_text;
}

[Serializable]
public struct MetaProgressCardTiming
{
    [Min(0f)] public float activate_fade_time;
    [Range(0.1f, 1f)] public float activate_start_scale;
    [Min(0f)] public float complete_feedback_duration;
    [Min(0f)] public float puzzle_punch_scale;
    [Min(0f)] public float puzzle_punch_duration;
    [Min(0f)] public float fill_duration;
    [Min(0f)] public float count_duration;
}


[CreateAssetMenu(fileName = "MetaProgressConfig", menuName = "Vertigo Wheel/Config/Meta Progress Config")]
public class MetaProgressConfig : ScriptableObject
{
    [Header("Card Animation")]
    public MetaProgressCardTiming cardTiming;

    [Header("Cards")]
    public List<ProgressFamilyPool> familyPools = new();

    public RewardDefinition overflowReward;

    private EntryView[] entries = Array.Empty<EntryView>();

    internal EntryView[] Entries
    {
        get
        {
            return entries;
        }
    }

    private void OnEnable()
    {
        RefreshEntries();
    }

    private void OnValidate()
    {
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        entries = Array.Empty<EntryView>();

        EntryView[] next_entries = new EntryView[CountEntries()];
        int next_entry = 0;

        foreach (var pool in familyPools)
        {
            var sorted_cards = new List<ProgressCardEntry>(pool.cards);
            sorted_cards.Sort(CompareByChainOrder);

            foreach (var card in sorted_cards)
            {
                next_entries[next_entry] = new EntryView
                {
                    point_reward = pool.point_reward,
                    chain_order = card.chain_order,
                    target_icon = card.target_icon,
                    limit = card.limit,
                    initial_amount = card.initial_amount,
                    title_text = card.title_text,
                    tier_text = card.tier_text,
                    subtitle_text = card.subtitle_text,
                };
                next_entry++;
            }
        }

        entries = next_entries;
    }

    private static int CompareByChainOrder(ProgressCardEntry a, ProgressCardEntry b)
    {
        return a.chain_order.CompareTo(b.chain_order);
    }

    private int CountEntries()
    {
        int count = 0;
        foreach (var pool in familyPools)
        {
            count += pool.cards.Count;
        }
        return count;
    }
}
}
