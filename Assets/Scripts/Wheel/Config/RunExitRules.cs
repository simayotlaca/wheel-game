using UnityEngine;

[CreateAssetMenu(
    fileName = "RunExitRules",
    menuName = "Vertigo/Wheel/Run Exit Rules")]
public sealed class RunExitRules : ScriptableObject
{
    [SerializeField] private bool allowExitOnNormalZones = true;

    public bool AllowExitOnNormalZones => allowExitOnNormalZones;
}
