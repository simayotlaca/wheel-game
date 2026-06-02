using UnityEngine;

namespace VertigoWheel
{
[CreateAssetMenu(fileName = "CurrencyConfig", menuName = "Vertigo Wheel/Config/Currency Config")]
public class CurrencyConfig : ScriptableObject
{
    [Header("Currency Rewards")]
    public RewardDefinition cashReward;
    public RewardDefinition goldReward;

    [Header("Initial Wallet (first launch only)")]
    [Min(0)] public int initialCash = 0;

    [Min(0)] public int initialGold = 200;

    public bool resetSaveOnLaunch = false;

    [Header("Cash Progression")]
    [Min(0)] public int cashMinBase = 320;
    [Min(0)] public int cashMaxBase = 520;
    [Min(0)] public int cashMinIncreasePerZone = 0;
    [Min(0)] public int cashMaxIncreasePerZone = 0;

    [Header("Gold Progression")]
    [Min(0)] public int goldBaseAmount = 1;
    [Min(1)] public int goldIncreaseEveryZones = 3;
    [Min(0)] public int goldIncreaseAmount = 0;

    [Header("Card Progression")]
    [Min(0)] public int cardBaseAmount = 1;
    [Min(1)] public int cardIncreaseEveryZones = 5;
    [Min(0)] public int cardIncreaseAmount = 1;
    [Min(1)] public int cardMaxAmount = 3;

    [Header("Revive Cost")]
    [Min(0)] public int reviveBaseCost = 25;
    [Min(0)] public int reviveCostIncreasePerRevive = 25;
}
}
