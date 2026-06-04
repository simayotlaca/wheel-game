using System.Collections.Generic;

namespace VertigoWheel
{
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
}
