using UnityEngine;

public partial class WheelLogic
{
    private void SampleUniqueIcons(ZoneConfig zone, int slotsToFill)
    {
        int poolSize = BuildAvailablePool(zone);
        if (poolSize == 0) return;

        int remaining = poolSize;

        for (int slot = 0; slot < slotsToFill; slot++)
        {
            if (remaining == 0)
            {
                remaining = BuildAvailablePool(zone);
                if (remaining == 0) return;
            }

            int[] cumulative = GetUniformCumulative(remaining);
            int picked = PickSlice(remaining, cumulative);
            if (picked < 0 || picked >= remaining) picked = remaining - 1;

            bool foundUnique = false;
            for (int offset = 0; offset < remaining; offset++)
            {
                int candidateIdx = (picked + offset) % remaining;
                int poolIdx = pool_indices[candidateIdx];
                if (CanPlaceCandidate(zone.slices[poolIdx], slot))
                {
                    picked = candidateIdx;
                    foundUnique = true;
                    break;
                }
            }

            int chosenPoolIdx = pool_indices[picked];
            wheelSlots[slot] = zone.slices[chosenPoolIdx];

            pool_indices[picked] = pool_indices[remaining - 1];
            remaining--;

#if UNITY_EDITOR
            if (!foundUnique)
            {
                string id = wheelSlots[slot]?.reward?.rewardId ?? "null";
                DebugLogger.LogWarning($"[WheelLogic] Pool '{zone.name}' cannot satisfy composition rules for {slotsToFill} reward slots — slot {slot} fell back to '{id}' violating an icon/category cap. Add more distinct-icon, non-chest slices to the pool.");
            }
#endif
        }
    }

    private void InsertDeathAtRandomSlot(ZoneConfig zone)
    {
        SliceDefinition deathSlice = PickRandomDeathFromPool(zone);
        if (deathSlice == null) return;

        int[] cumulative = GetUniformCumulative(WheelSlotCapacity);
        int deathSlot = PickSlice(WheelSlotCapacity, cumulative);
        if (deathSlot < 0 || deathSlot >= WheelSlotCapacity) deathSlot = WheelSlotCapacity - 1;

        for (int i = WheelSlotCapacity - 1; i > deathSlot; i--)
            wheelSlots[i] = wheelSlots[i - 1];
        wheelSlots[deathSlot] = deathSlice;
    }

    private int BuildAvailablePool(ZoneConfig zone)
    {
        if (pool_indices.Length < zone.slices.Length)
            pool_indices = new int[zone.slices.Length];

        int n = 0;
        for (int i = 0; i < zone.slices.Length; i++)
        {
            if (IsDeath(zone.slices[i])) continue;
            pool_indices[n++] = i;
        }
        return n;
    }

    private SliceDefinition PickRandomDeathFromPool(ZoneConfig zone)
    {
        int deathCount = 0;
        for (int i = 0; i < zone.slices.Length; i++)
            if (IsDeath(zone.slices[i])) deathCount++;
        if (deathCount == 0) return null;

        int[] cumulative = GetUniformCumulative(deathCount);
        int nth = PickSlice(deathCount, cumulative);
        if (nth < 0 || nth >= deathCount) nth = deathCount - 1;

        int seen = 0;
        for (int i = 0; i < zone.slices.Length; i++)
        {
            if (!IsDeath(zone.slices[i])) continue;
            if (seen == nth) return zone.slices[i];
            seen++;
        }
        return null;
    }

    private static int CategoryCap(RewardVisualCategory cat)
    {
        switch (cat)
        {
            case RewardVisualCategory.Death: return 1;
            case RewardVisualCategory.Chest: return 1;
            default: return int.MaxValue;
        }
    }

    private bool CanPlaceCandidate(SliceDefinition candidate, int placedCount)
    {
        if (candidate == null || candidate.reward == null) return true;
        Sprite candidateIcon = candidate.reward.icon;
        var candidateCat = candidate.reward.visualCategory;
        var candidateFamily = candidate.reward.visualFamily;
        int catCap = CategoryCap(candidateCat);
        int catCount = 0;

        for (int i = 0; i < placedCount; i++)
        {
            SliceDefinition placed = wheelSlots[i];
            if (placed == null || placed.reward == null) continue;
            if (candidateIcon != null && placed.reward.icon == candidateIcon) return false;
            if (!string.IsNullOrEmpty(candidateFamily) && placed.reward.visualFamily == candidateFamily) return false;
            if (placed.reward.visualCategory == candidateCat && ++catCount >= catCap) return false;
        }
        return true;
    }

    private bool PoolContainsDeath(ZoneConfig zone)
    {
        for (int i = 0; i < zone.slices.Length; i++)
            if (IsDeath(zone.slices[i])) return true;
        return false;
    }

    private static bool IsDeath(SliceDefinition s)
    {
        return s != null && s.reward != null && s.reward.isDeath;
    }
}
