using UnityEngine;

namespace VertigoWheel
{
internal class CurrencyRules
{
    private CurrencyConfig config;

    internal CurrencyRules(CurrencyConfig config)
    {
        this.config = config;
    }

    internal RewardDefinition CashReward
    {
        get
        {
            return config.cashReward;
        }
    }

    internal RewardDefinition GoldReward
    {
        get
        {
            return config.goldReward;
        }
    }

    internal int AmountForReward(RewardDefinition reward, int zone)
    {
        switch (reward.amountMode)
        {
            case RewardAmountMode.CashProgression:
                return CashAmount();
            case RewardAmountMode.GoldProgression:
                return ProgressionAmountForZone(
                    zone,
                    config.goldBaseAmount,
                    config.goldIncreaseEveryZones,
                    config.goldIncreaseAmount,
                    int.MaxValue);
            case RewardAmountMode.CardProgression:
                return ProgressionAmountForZone(
                    zone,
                    config.cardBaseAmount,
                    config.cardIncreaseEveryZones,
                    config.cardIncreaseAmount,
                    config.cardMaxAmount);
            default:
                return reward.fixedAmount;
        }
    }

    internal int ReviveCostForCount(int revive_count)
    {
        return config.reviveBaseCost + revive_count * config.reviveCostIncreasePerRevive;
    }

    private int CashAmount()
    {
        return Random.Range(config.cashMinBase, config.cashMaxBase + 1);
    }

    private static int ProgressionAmountForZone(
        int zone,
        int base_amount,
        int increase_every_zones,
        int increase_amount,
        int max_amount)
    {
        int z = zone - 1;
        int steps = z / increase_every_zones;
        int amount = base_amount + steps * increase_amount;
        return Mathf.Min(amount, max_amount);
    }
}
}
