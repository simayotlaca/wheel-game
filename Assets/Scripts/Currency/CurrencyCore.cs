using System.Collections.Generic;
using UnityEngine;

namespace VertigoWheel
{
public sealed class CurrencyRules
{
    private readonly CurrencyConfig config;

    public CurrencyRules(CurrencyConfig config)
    {
        this.config = config;
    }

    public RewardDefinition CashReward => config.cashReward;
    public RewardDefinition GoldReward => config.goldReward;

    public int AmountForReward(RewardDefinition reward, int zone)
    {
        int amount = 1;

        if (reward != null)
        {
            if (reward == config.cashReward)
            {
                amount = CashAmountForZone(zone);
            }
            else if (reward == config.goldReward)
            {
                amount = GoldAmountForZone(zone);
            }
            else if (reward.slotCategory == SlotCategory.AllCards)
            {
                amount = CardAmountForZone(zone);
            }
        }

        return amount;
    }

    //each revive bumps the next cost, simple but enough for this case
    public int ReviveCostForCount(int revive_count)
    {
        return config.reviveBaseCost + revive_count * config.reviveCostIncreasePerRevive;
    }

    //i wanted cash to feel more random, gold and cards more like steps
    //not super fancy but it makes rewards feel different as zones go up
    private int CashAmountForZone(int zone)
    {
        int z = Mathf.Max(1, zone) - 1;
        int min = config.cashMinBase + config.cashMinIncreasePerZone * z;
        int max = config.cashMaxBase + config.cashMaxIncreasePerZone * z;

        if (max < min)
        {
            max = min;
        }

        return Random.Range(min, max + 1);
    }

    private int GoldAmountForZone(int zone)
    {
        int z = Mathf.Max(1, zone) - 1;
        int every = Mathf.Max(1, config.goldIncreaseEveryZones);
        int steps = z / every;

        return config.goldBaseAmount + steps * config.goldIncreaseAmount;
    }

    private int CardAmountForZone(int zone)
    {
        int z = Mathf.Max(1, zone) - 1;
        int every = Mathf.Max(1, config.cardIncreaseEveryZones);
        int steps = z / every;
        int amount = config.cardBaseAmount + steps * config.cardIncreaseAmount;

        return Mathf.Min(amount, config.cardMaxAmount);
    }
}

public class CurrencyWallet
{
    private sealed class RewardBalance
    {
        public int pending;
        public int banked;
    }

    private readonly Dictionary<RewardDefinition, RewardBalance> rewards = new Dictionary<RewardDefinition, RewardBalance>(32);
    private readonly Dictionary<string, RewardDefinition> reward_by_id = new Dictionary<string, RewardDefinition>(32);
    private readonly Dictionary<string, int> banked_save_buffer = new Dictionary<string, int>(32);

    private int pending_reward_count;

    private readonly RewardDefinition cash_reward;
    private readonly RewardDefinition gold_reward;

    public CurrencyWallet(CurrencyConfig currency_config, RewardTableConfig reward_table_cfg, MetaProgressConfig meta_cfg)
    {
        if (currency_config == null)
        {
            throw new System.ArgumentNullException(nameof(currency_config));
        }

        cash_reward = currency_config.cashReward;
        gold_reward = currency_config.goldReward;

        IndexReward(cash_reward);
        IndexReward(gold_reward);
        IndexRewardTable(reward_table_cfg);
        IndexMetaProgress(meta_cfg);
    }

    public int Cash => GetBanked(cash_reward);

    public int Gold => GetBanked(gold_reward);

    public bool HasPending => pending_reward_count > 0;

    public IReadOnlyDictionary<string, int> BankedForSave => BuildBankedSaveData();

    public void AddPending(RewardDefinition reward, int amount)
    {
        if (amount > 0 && IsUsableReward(reward))
        {
            IndexReward(reward);
            RewardBalance balance = GetOrCreateBalance(reward);
            if (balance.pending == 0)
            {
                pending_reward_count++;
            }
            balance.pending += amount;
        }
    }

    public bool TryGetPending(RewardDefinition reward, out int amount)
    {
        amount = 0;
        if (reward != null && rewards.TryGetValue(reward, out RewardBalance balance) && balance.pending > 0)
        {
            amount = balance.pending;
            return true;
        }
        return false;
    }

    public void ClearPending()
    {
        foreach (var kv in rewards)
        {
            kv.Value.pending = 0;
        }
        pending_reward_count = 0;
    }

    public void BankPending()
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
        pending_reward_count = 0;
    }

    public bool TrySpendGold(int amount)
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

    public void RestoreFrom(int saved_cash, int saved_gold, IReadOnlyDictionary<string, int> saved_banked)
    {
        rewards.Clear();
        pending_reward_count = 0;

        AddBanked(cash_reward, saved_cash);
        AddBanked(gold_reward, saved_gold);

        if (saved_banked != null)
        {
            foreach (var kv in saved_banked)
            {
                if (kv.Value > 0 && reward_by_id.TryGetValue(kv.Key, out RewardDefinition reward) && reward != cash_reward && reward != gold_reward)
                {
                    AddBanked(reward, kv.Value);
                }
            }
        }
    }

    private void AddBanked(RewardDefinition reward, int amount)
    {
        if (amount > 0 && IsUsableReward(reward))
        {
            IndexReward(reward);
            GetOrCreateBalance(reward).banked += amount;
        }
    }

    private int GetBanked(RewardDefinition reward)
    {
        if (reward != null && rewards.TryGetValue(reward, out RewardBalance balance))
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

    private IReadOnlyDictionary<string, int> BuildBankedSaveData()
    {
        banked_save_buffer.Clear();
        foreach (var kv in rewards)
        {
            RewardDefinition reward = kv.Key;
            int amount = kv.Value.banked;
            if (amount > 0 && IsUsableReward(reward))
            {
                banked_save_buffer.TryGetValue(reward.rewardId, out int existing);
                banked_save_buffer[reward.rewardId] = existing + amount;
            }
        }
        return banked_save_buffer;
    }

    private void IndexRewardTable(RewardTableConfig reward_table_cfg)
    {
        if (reward_table_cfg == null)
        {
            return;
        }

        IndexZoneTable(reward_table_cfg.normalZone);
        IndexZoneTable(reward_table_cfg.safeZone);
        IndexZoneTable(reward_table_cfg.superZone);
    }

    private void IndexZoneTable(RewardTableConfig.ZoneTable zone_table)
    {
        if (zone_table == null)
        {
            return;
        }

        IndexPool(zone_table.deathPool);
        IndexPool(zone_table.otherPool);
        IndexPool(zone_table.allCardsPool);
        IndexPool(zone_table.specialPool);
    }

    private void IndexPool(ZoneRewardEntry[] pool)
    {
        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null)
            {
                IndexReward(pool[i].reward);
            }
        }
    }

    private void IndexMetaProgress(MetaProgressConfig meta_cfg)
    {
        if (meta_cfg == null)
        {
            return;
        }

        IndexReward(meta_cfg.overflowReward);
        List<ProgressFamilyPool> family_pools = meta_cfg.familyPools;
        if (family_pools == null)
        {
            return;
        }

        for (int i = 0; i < family_pools.Count; i++)
        {
            ProgressFamilyPool pool = family_pools[i];
            if (pool != null)
            {
                IndexReward(pool.point_reward);
            }
        }
    }

    private void IndexReward(RewardDefinition reward)
    {
        if (IsUsableReward(reward) && !reward_by_id.ContainsKey(reward.rewardId))
        {
            reward_by_id[reward.rewardId] = reward;
        }
    }

    private static bool IsUsableReward(RewardDefinition reward)
    {
        return reward != null && !string.IsNullOrEmpty(reward.rewardId);
    }
}

public class ReviveSystem
{
    private int revive_count;
    private readonly CurrencyRules currency_rules;

    public ReviveSystem(CurrencyRules rules)
    {
        this.currency_rules = rules;
        this.revive_count = 0;
    }

    public int CurrentCost => currency_rules.ReviveCostForCount(revive_count);

    public int ReviveCount => revive_count;

    public bool CanAfford(CurrencyWallet inventory)
    {
        return inventory.Gold >= CurrentCost;
    }

    public bool TryRevive(CurrencyWallet inventory)
    {
        if (inventory.TrySpendGold(CurrentCost))
        {
            revive_count++;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        revive_count = 0;
    }

    public void RestoreCount(int saved)
    {
        revive_count = Mathf.Max(0, saved);
    }
}
}
