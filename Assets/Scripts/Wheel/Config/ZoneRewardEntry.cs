using UnityEngine;

[System.Serializable]
public struct ZoneRewardEntry
{
    public RewardDefinition reward;

    [Min(1)]
    public int amount;

    [Min(0f)]
    public float weight;
}
