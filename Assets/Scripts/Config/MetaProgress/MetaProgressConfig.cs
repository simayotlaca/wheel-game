using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
//cards stay under their reward family, then chain_order sorts inside that group
#region setup
[Serializable]
public class ProgressCardEntry
{
    public string second_dimension_key;
    [Min(0)] public int chain_order;
    public Sprite target_icon;
    [Min(1)] public int limit = 100;
    [Min(0)] public int initial_amount;
    public string title_text;
    public string tier_text;
    public string subtitle_text;
}


[Serializable]
public class ProgressFamilyPool
{
    public string family_key;
    public RewardDefinition point_reward;

    public List<ProgressCardEntry> cards = new();
}

#endregion

public struct EntryView
{
    public RewardDefinition point_reward;
    public string family_key;
    public int chain_order;
    public string second_dimension_key;
    public Sprite target_icon;
    public int limit;
    public int initial_amount;
    public string title_text;
    public string tier_text;
    public string subtitle_text;
}


[CreateAssetMenu(fileName = "MetaProgressConfig", menuName = "Vertigo Wheel/Config/Meta Progress Config")]
public class MetaProgressConfig : ScriptableObject
{
    public List<ProgressFamilyPool> familyPools = new();

    public RewardDefinition overflowReward;

    private List<EntryView> effective_entries = new();

    public IReadOnlyList<EntryView> ConvertedArray => effective_entries;

    private void OnEnable()
    {
        ArraySolver();
    }

    private void OnValidate()
    {
        ArraySolver();
    }

    private void ArraySolver()
    {
        effective_entries.Clear();
        //i flatten the family list here, easier for panel to read later
        foreach (var pool in familyPools)
        {
            var sortedCards = new List<ProgressCardEntry>(pool.cards);
            sortedCards.Sort(s_compareByChainOrder);

            foreach (var card in sortedCards)
            {
                effective_entries.Add(new EntryView
                {
                    point_reward = pool.point_reward,
                    family_key = pool.family_key,
                    chain_order = card.chain_order,
                    second_dimension_key = card.second_dimension_key,
                    target_icon = card.target_icon,
                    limit = card.limit,
                    initial_amount = card.initial_amount,
                    title_text = card.title_text,
                    tier_text = card.tier_text,
                    subtitle_text = card.subtitle_text,
                });
            }
        }
    }

    private static readonly System.Comparison<ProgressCardEntry> s_compareByChainOrder = CompareByChainOrder;

    private static int CompareByChainOrder(ProgressCardEntry a, ProgressCardEntry b)
    {
        return a.chain_order.CompareTo(b.chain_order);
    }
}
}
