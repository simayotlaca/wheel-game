using System.Collections.Generic;

public class MetaProgressionService
{
    private readonly WeaponProgressDefinition[] definitions;

    private readonly Dictionary<string, RewardDefinition> rewards_by_id;

    private readonly Dictionary<string, int> progress = new Dictionary<string, int>(8);

    private readonly Dictionary<string, int> old_snapshot = new Dictionary<string, int>(8);

    private readonly HashSet<string> active = new HashSet<string>();

    public MetaProgressionService(WeaponProgressDefinition[] defs, RewardDefinition[] rewards)
    {
        if (defs    == null) throw new System.ArgumentNullException(nameof(defs));
        if (rewards == null) throw new System.ArgumentNullException(nameof(rewards));

        definitions = defs;

        rewards_by_id = new Dictionary<string, RewardDefinition>(rewards.Length);
        for (int i = 0; i < rewards.Length; i++)
        {
            var r = rewards[i];
            if (r == null || string.IsNullOrEmpty(r.rewardId)) continue;

            if (!rewards_by_id.ContainsKey(r.rewardId)) rewards_by_id.Add(r.rewardId, r);
        }
    }

    public RewardDefinition GetRewardFor(WeaponProgressDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.pointsRewardId)) return null;
        rewards_by_id.TryGetValue(def.pointsRewardId, out RewardDefinition r);
        return r;
    }

    public IReadOnlyList<WeaponProgressDefinition> Definitions => definitions;

    public WeaponProgressDefinition FindByRewardId(string rewardId)
    {
        if (string.IsNullOrEmpty(rewardId)) return null;
        for (int i = 0; i < definitions.Length; i++)
        {
            var d = definitions[i];
            if (d != null && d.pointsRewardId == rewardId) return d;
        }
        return null;
    }

    public int CurrentPoints(WeaponProgressDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.pointsRewardId)) return 0;
        progress.TryGetValue(def.pointsRewardId, out int v);
        return v;
    }

    public int OldPoints(WeaponProgressDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.pointsRewardId)) return 0;
        old_snapshot.TryGetValue(def.pointsRewardId, out int v);
        return v;
    }

    public bool IsActiveTarget(WeaponProgressDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.pointsRewardId)) return false;
        return active.Contains(def.pointsRewardId);
    }

    public bool IsUnlocked(WeaponProgressDefinition def)
    {
        if (def == null) return false;
        return CurrentPoints(def) >= def.requiredPoints;
    }

    public WeaponProgressDefinition AddProgress(string rewardId, int amount, out int oldVal, out int newVal)
    {
        oldVal = 0;
        newVal = 0;
        var def = FindByRewardId(rewardId);
        if (def == null) return null;
        if (amount <= 0) amount = 0;

        progress.TryGetValue(rewardId, out oldVal);
        newVal = oldVal + amount;
        old_snapshot[rewardId] = oldVal;
        progress[rewardId] = newVal;
        active.Add(rewardId);
        return def;
    }

    public void ResetAndDeactivate(WeaponProgressDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.pointsRewardId)) return;
        progress.Remove(def.pointsRewardId);
        old_snapshot.Remove(def.pointsRewardId);
        active.Remove(def.pointsRewardId);
    }

    public void ResetAll()
    {
        progress.Clear();
        old_snapshot.Clear();
        active.Clear();
    }
}
