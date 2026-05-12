public class ZoneManager
{
    private readonly WheelConfig config;
    private int current_zone;

    public int CurrentZone => current_zone;
    public ZoneType CurrentZoneType => config.GetZoneType(current_zone);
    public ZoneConfig CurrentZoneConfig => config.PickZoneFor(current_zone);

    public ZoneManager(WheelConfig config)
    {
        if (config == null)
            throw new System.ArgumentNullException(nameof(config));
        this.config = config;
        current_zone = 1;
    }

    public void Reset()
    {
        current_zone = 1;
    }

    public void Advance()
    {
        current_zone++;
    }
}
