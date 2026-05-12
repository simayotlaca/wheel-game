using PrimeTween;
using UnityEngine;

public class SpinHintUI : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private RectTransform spinButtonScaleRoot;
    [SerializeField] private SpinRewardFlyAnimator spinFlyAnimator;
    [SerializeField] private WheelAnimationConfig animConfig;
    [SerializeField] private float idleDelay = 5f;

    private Tween hint_tween;
    private System.Action onHintUp;
    private System.Action onHintDown;

    private float idle_timer = 0f;
    private bool is_hinting = false;
    private int hint_loop_count = 0;
    private Vector3 original_scale;

    void Awake()
    {
        if (animConfig == null)
        {
            Debug.LogError("SpinHintUI: animConfig is not assigned.", this);
            enabled = false;
            return;
        }

        onHintUp = OnHintScaleUpComplete;
        onHintDown = OnHintScaleDownComplete;
    }

    void OnEnable()
    {
        if (controller != null)
            controller.OnZoneChanged += HandleZoneChanged;
        ResetIdleTimer();
    }

    void OnDisable()
    {
        if (controller != null)
            controller.OnZoneChanged -= HandleZoneChanged;
        StopHint();
    }

    void OnDestroy()
    {
        KillHintTween();
    }

    void HandleZoneChanged(int zone, ZoneType type) => ResetIdleTimer();

    void Update()
    {
        if (controller == null) return;

        if (!controller.CanSpin || (spinFlyAnimator != null && spinFlyAnimator.IsBusy))
        {
            if (is_hinting) StopHint();
            idle_timer = 0f;
            return;
        }

        if (!is_hinting)
        {
            idle_timer += Time.deltaTime;
            if (idle_timer >= idleDelay)
                StartHint();
        }
    }

    public void ResetIdleTimer()
    {
        idle_timer = 0f;
        StopHint();
    }

    void StartHint()
    {
        if (is_hinting || spinButtonScaleRoot == null) return;

        is_hinting = true;
        hint_loop_count = 0;
        original_scale = spinButtonScaleRoot.localScale;

        KillHintTween();
        hint_tween = Tween.Scale(
            spinButtonScaleRoot,
            original_scale * animConfig.hintPulseScale,
            animConfig.hintPulseDuration,
            Ease.InOutQuad).OnComplete(onHintUp);
    }

    void OnHintScaleUpComplete()
    {
        if (!is_hinting) return;
        hint_tween = Tween.Scale(
            spinButtonScaleRoot,
            original_scale,
            animConfig.hintPulseDuration,
            Ease.InOutQuad).OnComplete(onHintDown);
    }

    void OnHintScaleDownComplete()
    {
        if (!is_hinting) return;

        hint_loop_count++;

        if (hint_loop_count < animConfig.hintMaxLoops)
        {
            hint_tween = Tween.Scale(
                spinButtonScaleRoot,
                original_scale * animConfig.hintPulseScale,
                animConfig.hintPulseDuration,
                Ease.InOutQuad).OnComplete(onHintUp);
        }
        else
        {
            is_hinting = false;
            if (spinButtonScaleRoot != null)
                spinButtonScaleRoot.localScale = original_scale;
        }
    }

    void StopHint()
    {
        if (!is_hinting) return;
        is_hinting = false;
        KillHintTween();
        if (spinButtonScaleRoot != null)
            spinButtonScaleRoot.localScale = original_scale;
    }

    void KillHintTween()
    {
        if (hint_tween.isAlive) hint_tween.Stop();
    }
}
