namespace VertigoWheel
{
internal class ZoneManager
{
    private WheelConfig config;
    private int current_zone;

    internal int CurrentZone
    {
        get
        {
            return current_zone;
        }
    }

    internal int FirstZoneIndex
    {
        get
        {
            return config.FirstZoneIndex;
        }
    }

    internal int MaxZoneIndex
    {
        get
        {
            return config.MaxZoneIndex;
        }
    }

    internal ZoneManager(WheelConfig config)
    {
        this.config = config;
        current_zone = FirstZoneIndex;
    }

    internal void Reset()
    {
        current_zone = FirstZoneIndex;
    }

    internal void Advance()
    {
        current_zone = GameRules.NextZoneIndex(config, current_zone);
    }

    internal WheelVisual GetZoneVisual(int zone_idx)
    {
        return GameRules.GetZoneVisual(config, zone_idx);
    }

    internal RewardTier GetZoneTier(int zone_idx)
    {
        return GameRules.GetZoneTier(config, zone_idx);
    }
}
}
