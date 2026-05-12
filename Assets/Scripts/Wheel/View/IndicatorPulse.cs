using PrimeTween;
using UnityEngine;

public class IndicatorPulse : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    [SerializeField, Range(2f, 30f)] private float kickAngle    = 15f;
    [SerializeField, Min(0.02f)]     private float kickDuration = 0.08f;

    [SerializeField, Min(0f)] private float minVelocityForKick = 30f;

    private bool kick_in_flight;

    public void ResetKickFlag()
    {
        kick_in_flight = false;
        if (target != null)
        {
            Tween.StopAll(onTarget: target);
            target.localRotation = Quaternion.identity;
        }
    }

    void Awake()
    {
        if (target == null) target = transform as RectTransform;
        if (target != null) target.localRotation = Quaternion.identity;
    }

    void OnDisable()
    {
        kick_in_flight = false;
        if (target != null)
        {
            target.localRotation = Quaternion.identity;
            Tween.StopAll(onTarget: target);
        }
    }

    public void Tick(float wheelAngularVelocity)
    {
        if (kick_in_flight) return;
        if (target == null) return;
        if (Mathf.Abs(wheelAngularVelocity) < minVelocityForKick) return;

        kick_in_flight = true;
        Tween.LocalRotation(target, new Vector3(0f, 0f, kickAngle), kickDuration, Ease.OutQuad)
            .OnComplete(OnKickPeak);
    }

    private void OnKickPeak()
    {
        if (target == null)
        {
            kick_in_flight = false;
            return;
        }
        Tween.LocalRotation(target, Vector3.zero, kickDuration, Ease.OutQuad)
            .OnComplete(OnKickReturn);
    }

    private void OnKickReturn()
    {

        if (target != null) target.localRotation = Quaternion.identity;
        kick_in_flight = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (target == null) target = transform as RectTransform;
    }
#endif
}
