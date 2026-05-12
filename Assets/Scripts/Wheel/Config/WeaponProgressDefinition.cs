using UnityEngine;

[CreateAssetMenu(fileName = "WeaponProgressDefinition", menuName = "Wheel/WeaponProgressDefinition")]
public class WeaponProgressDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;

    [Min(1)]
    public int requiredPoints = 100;

    public string pointsRewardId;

    public bool initiallyVisible = true;
}
