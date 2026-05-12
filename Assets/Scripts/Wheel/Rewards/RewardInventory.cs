using System.Collections.Generic;

public class RewardInventory
{
    private readonly Dictionary<string, int> pending = new Dictionary<string, int>(16);
    private readonly Dictionary<string, int> banked = new Dictionary<string, int>(32);

    private int cash;
    private int gold;

    private readonly string cash_id;
    private readonly string gold_id;

    public RewardInventory(CurrencyConfig currencyConfig)
    {
        if (currencyConfig == null)
            throw new System.ArgumentNullException(nameof(currencyConfig));

        cash_id = currencyConfig.cashCurrencyId;
        gold_id = currencyConfig.goldCurrencyId;
    }

    public int Cash => cash;
    public int Gold => gold;

    public IReadOnlyDictionary<string, int> Pending => pending;
    public IReadOnlyDictionary<string, int> Banked => banked;

    public int this[CurrencyType input]
    {
        get
        {
            switch (input)
            {
                case CurrencyType.cash: return cash;
                case CurrencyType.gold: return gold;
                default: throw new System.ArgumentOutOfRangeException(nameof(input));
            }
        }
    }

    public void AddPending(RewardDefinition reward, int amount)
    {
        if (reward == null || amount <= 0) return;
        if (string.IsNullOrEmpty(reward.rewardId)) return;

        pending.TryGetValue(reward.rewardId, out int existing);
        pending[reward.rewardId] = existing + amount;
    }

    public void ClearPending()
    {
        pending.Clear();
    }

    public void BankPending()
    {
        foreach (var kv in pending)
        {
            string id = kv.Key;
            int amount = kv.Value;

            if (IsCashIdInstance(id)) cash += amount;
            else if (IsGoldIdInstance(id)) gold += amount;

            banked.TryGetValue(id, out int existing);
            banked[id] = existing + amount;
        }
        pending.Clear();
    }

    public void AddCurrency(CurrencyType input, int amount)
    {
        if (amount <= 0) return;
        switch (input)
        {
            case CurrencyType.cash: cash += amount; break;
            case CurrencyType.gold: gold += amount; break;
            default: throw new System.ArgumentOutOfRangeException(nameof(input));
        }
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0) return false;
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    public void HardReset()
    {
        pending.Clear();
        banked.Clear();
        cash = 0;
        gold = 0;
    }

    public void RestoreFrom(int savedCash, int savedGold, IReadOnlyDictionary<string, int> savedBanked)
    {
        cash = savedCash < 0 ? 0 : savedCash;
        gold = savedGold < 0 ? 0 : savedGold;
        banked.Clear();
        if (savedBanked != null)
        {
            foreach (var kv in savedBanked)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0) continue;
                banked[kv.Key] = kv.Value;
            }
        }
    }

    public bool IsCashIdInstance(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id == cash_id;
    }

    public bool IsGoldIdInstance(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id == gold_id;
    }
}
