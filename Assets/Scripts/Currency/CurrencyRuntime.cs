using System.Collections.Generic;
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

internal class CurrencyWallet
{
    private class RewardBalance
    {
        internal int pending;
        internal int banked;
    }

    private Dictionary<RewardDefinition, RewardBalance> rewards = new Dictionary<RewardDefinition, RewardBalance>();

    private RewardDefinition cash_reward;
    private RewardDefinition gold_reward;

    internal CurrencyWallet(CurrencyConfig currency_config)
    {
        cash_reward = currency_config.cashReward;
        gold_reward = currency_config.goldReward;
    }

    internal int Cash
    {
        get
        {
            return GetBanked(cash_reward);
        }
    }

    internal int Gold
    {
        get
        {
            return GetBanked(gold_reward);
        }
    }

    internal bool HasPending
    {
        get
        {
            foreach (var kv in rewards)
            {
                if (kv.Value.pending > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal void AddPending(RewardDefinition reward, int amount)
    {
        if (amount > 0)
        {
            RewardBalance balance = GetOrCreateBalance(reward);
            balance.pending += amount;
        }
    }

    internal int AddPendingAndGetTotal(RewardDefinition reward, int amount)
    {
        AddPending(reward, amount);
        return GetPending(reward);
    }

    internal int GetPending(RewardDefinition reward)
    {
        return rewards[reward].pending;
    }

    internal void ClearPending()
    {
        foreach (var kv in rewards)
        {
            kv.Value.pending = 0;
        }
    }

    internal void BankPending()
    {
        foreach (var kv in rewards)
        {
            RewardBalance balance = kv.Value;
            if (balance.pending > 0)
            {
                balance.banked += balance.pending;
                balance.pending = 0;
            }
        }
    }

    internal bool TrySpendGold(int amount)
    {
        if (amount >= 0 && Gold >= amount)
        {
            if (amount > 0)
            {
                GetOrCreateBalance(gold_reward).banked -= amount;
            }
            return true;
        }
        return false;
    }

    internal void ResetTo(int cash, int gold)
    {
        rewards.Clear();

        AddBanked(cash_reward, cash);
        AddBanked(gold_reward, gold);
    }

    private void AddBanked(RewardDefinition reward, int amount)
    {
        if (amount > 0)
        {
            GetOrCreateBalance(reward).banked += amount;
        }
    }

    private int GetBanked(RewardDefinition reward)
    {
        if (rewards.TryGetValue(reward, out RewardBalance balance))
        {
            return balance.banked;
        }
        return 0;
    }

    private RewardBalance GetOrCreateBalance(RewardDefinition reward)
    {
        if (!rewards.TryGetValue(reward, out RewardBalance balance))
        {
            balance = new RewardBalance();
            rewards[reward] = balance;
        }
        return balance;
    }
}

internal class ReviveSystem
{
    private int revive_count;
    private CurrencyRules currency_rules;

    internal ReviveSystem(CurrencyRules rules)
    {
        currency_rules = rules;
    }

    internal int CurrentCost
    {
        get
        {
            return currency_rules.ReviveCostForCount(revive_count);
        }
    }

    internal bool CanAfford(CurrencyWallet inventory)
    {
        return inventory.Gold >= CurrentCost;
    }

    internal bool TryRevive(CurrencyWallet inventory)
    {
        if (inventory.TrySpendGold(CurrentCost))
        {
            revive_count++;
            return true;
        }
        return false;
    }

    internal void Reset()
    {
        revive_count = 0;
    }
}

}
