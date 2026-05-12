using UnityEngine;

[CreateAssetMenu(fileName = "ZoneRewardTable", menuName = "Wheel/ZoneRewardTable")]
public class ZoneRewardTable : ScriptableObject
{

    public ZoneType zoneType = ZoneType.Normal;

    public ZoneConfig targetZone;

    public ZonePoolRules poolRules;

    public ZoneRewardEntry[] entries;
}
