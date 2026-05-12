using System.Collections.Generic;
using UnityEngine;

public partial class WheelLogic
{
    public const int WheelSlotCapacity = 8;

    private readonly SliceDefinition[] wheelSlots = new SliceDefinition[WheelSlotCapacity];
    private int slotCount;

    private bool has_sampled;
    private ZoneType last_zone_type;

    private int last_zone_idx = int.MinValue;

    private readonly bool resampleEverySpin = false;

    private readonly System.Random rng;

    private int[] cum_weights = new int[WheelSlotCapacity];
    private int[] pool_indices = new int[WheelSlotCapacity];

    private readonly HashSet<Sprite> previousFaceIcons = new HashSet<Sprite>();

    private static readonly SlotCategory[] CategoryOrder =
    {
        SlotCategory.Compact,
        SlotCategory.Death,
        SlotCategory.Currency,
        SlotCategory.Consumable,
        SlotCategory.Throwable,
        SlotCategory.Weapon,
        SlotCategory.Chest,
        SlotCategory.Cosmetic,
        SlotCategory.Gold,
    };

    private const int CategoryBucketCount = (int)SlotCategory.Gold + 1;
    private readonly List<SliceDefinition>[] by_category;
    private readonly List<SliceDefinition> picked_list = new List<SliceDefinition>(WheelSlotCapacity);
    private readonly HashSet<Sprite> picked_icons = new HashSet<Sprite>();
    private readonly HashSet<string> picked_families = new HashSet<string>();

    private readonly HashSet<Sprite> check_icons = new HashSet<Sprite>();
    private readonly HashSet<string> check_families = new HashSet<string>();
    private readonly List<SliceDefinition> fallback_list = new List<SliceDefinition>(16);

    public bool forceNoBombNextSpin;

    public int SliceCount => slotCount;

    public SliceDefinition[] WheelSlots => wheelSlots;

    public WheelLogic() : this(0) { }

    public WheelLogic(int seed)
    {
        rng = seed == 0 ? new System.Random() : new System.Random(seed);
        by_category = new List<SliceDefinition>[CategoryBucketCount];
        for (int i = 0; i < by_category.Length; i++)
            by_category[i] = new List<SliceDefinition>(4);
    }

    private int PickSlice(int totalWeight, int[] cumulativeWeights)
    {
        if (totalWeight <= 0 || cumulativeWeights == null || cumulativeWeights.Length == 0)
            return 0;

        int pick = rng.Next(0, totalWeight);

        int chosen = cumulativeWeights.Length - 1;
        for (int i = 0; i < cumulativeWeights.Length; i++)
        {
            if (pick < cumulativeWeights[i])
            {
                chosen = i;
                break;
            }
        }
        return chosen;
    }

    public void LoadZone(ZoneConfig zone)
    {
        LoadZone(zone, int.MinValue);
    }

    public void LoadZone(ZoneConfig zone, int zoneIndex)
    {
        slotCount = 0;
        if (zone == null || zone.slices == null || zone.slices.Length == 0) return;

        bool shouldResample = resampleEverySpin
                              || !has_sampled
                              || zone.type != last_zone_type
                              || zoneIndex != last_zone_idx;

        slotCount = WheelSlotCapacity;

        if (!shouldResample) return;

        for (int i = 0; i < WheelSlotCapacity; i++) wheelSlots[i] = null;

        bool sampledNew = false;
        if (zone.poolRules.slotCount > 0)
        {
            sampledNew = SampleByCategoryQuotas(zone, zoneIndex);
        }

        if (!sampledNew)
        {
            for (int i = 0; i < WheelSlotCapacity; i++) wheelSlots[i] = null;
            bool zoneNeedsDeath = zone.type == ZoneType.Normal && PoolContainsDeath(zone);
            int rewardSlotsToFill = zoneNeedsDeath ? WheelSlotCapacity - 1 : WheelSlotCapacity;
            SampleUniqueIcons(zone, rewardSlotsToFill);
            if (zoneNeedsDeath) InsertDeathAtRandomSlot(zone);
        }

        has_sampled = true;
        last_zone_type = zone.type;
        last_zone_idx = zoneIndex;
    }

    public SpinResult Spin(int currentZone)
    {
        if (slotCount == 0) return SpinResult.Invalid;

        int[] cumulative = GetUniformCumulative(slotCount);
        int chosen = PickSlice(slotCount, cumulative);
        if (chosen < 0 || chosen >= slotCount) chosen = slotCount - 1;

        SliceDefinition slice = wheelSlots[chosen];
        if (slice == null) return SpinResult.Invalid;

        if (forceNoBombNextSpin && slice.reward != null && slice.reward.isDeath)
        {
            for (int i = 0; i < slotCount; i++)
            {
                SliceDefinition s = wheelSlots[i];
                if (s != null && s.reward != null && !s.reward.isDeath)
                {
                    chosen = i;
                    slice = s;
                    break;
                }
            }
        }
        forceNoBombNextSpin = false;

        SpinResult result;
        result.sliceIndex = chosen;
        result.isDeath = slice.reward != null && slice.reward.isDeath;
        result.amount = (slice.reward != null && slice.reward.isDeath) ? 0 : slice.amount;
        result.isValid = true;
        return result;
    }

    public SliceDefinition GetSlice(int index)
    {
        if (index < 0 || index >= slotCount) return null;
        return wheelSlots[index];
    }

    private int[] GetUniformCumulative(int n)
    {
        if (cum_weights.Length != n)
        {
            cum_weights = new int[n];
        }
        for (int i = 0; i < n; i++) cum_weights[i] = i + 1;
        return cum_weights;
    }
}
