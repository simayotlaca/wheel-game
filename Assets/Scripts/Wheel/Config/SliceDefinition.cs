using UnityEngine;

[CreateAssetMenu(fileName = "SliceDefinition", menuName = "Wheel/SliceDefinition")]
public class SliceDefinition : ScriptableObject
{
    public RewardDefinition reward;

    [Min(1)]
    public int amount = 1;

    [Min(0f)]
    public float weight = 1f;
}
