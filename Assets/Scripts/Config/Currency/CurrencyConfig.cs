using UnityEngine;

namespace VertigoWheel
{
[CreateAssetMenu(fileName = "CurrencyConfig", menuName = "Vertigo Wheel/Config/Currency Config")]
public class CurrencyConfig : ScriptableObject
{
    [Header("Currency Rewards")]
    public RewardDefinition cashReward;
    public RewardDefinition goldReward;

    [Header("Initial Wallet")]
    [Min(0)] public int initialCash;

    [Min(0)] public int initialGold;

    [Header("Cash Amount")]
    [Min(0)] public int cashMinBase;
    [Min(0)] public int cashMaxBase;

    [Header("Gold Amount")]
    [Min(0)] public int goldBaseAmount;
    [Min(1)] public int goldIncreaseEveryZones;
    [Min(0)] public int goldIncreaseAmount;

    [Header("Card Progression")]
    [Min(0)] public int cardBaseAmount;
    [Min(1)] public int cardIncreaseEveryZones;
    [Min(0)] public int cardIncreaseAmount;
    [Min(1)] public int cardMaxAmount;

    [Header("Revive Cost")]
    [Min(0)] public int reviveBaseCost;
    [Min(0)] public int reviveCostIncreasePerRevive;
}
}
