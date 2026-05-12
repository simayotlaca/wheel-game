using UnityEngine;
using UnityEngine.UI;

public class ZoneHUD : MonoBehaviour
{
    [SerializeField] private WheelController controller;
    [SerializeField] private RunExitController exitController;
    [SerializeField] private CanvasGroup zoneGroup;
    [SerializeField] private Canvas zoneCanvas;

    [SerializeField] private Button spinButton;
    [SerializeField] private SpinButtonAnimator spinAnimator;
    [SerializeField] private SpinRewardFlyAnimator spinFlyAnimator;

    private SpinButtonAnimator.State _lastTarget;
    private bool _buttonStateInitialized;
    private bool _zoneSubscribed;

    void Awake()
    {
        if (controller     == null) { Debug.LogError("[ZoneHUD] controller not assigned",     this); enabled = false; return; }
        if (exitController == null) { Debug.LogError("[ZoneHUD] exitController not assigned", this); enabled = false; return; }
        if (spinButton     == null) { Debug.LogError("[ZoneHUD] spinButton not assigned",     this); enabled = false; return; }

        spinButton.onClick.AddListener(OnSpin);
    }

    void OnEnable()
    {
        if (exitController != null && !_zoneSubscribed)
        {
            exitController.OnStateChanged += HandleExitStateChanged;
            _zoneSubscribed = true;

            HandleExitStateChanged(exitController.State);
        }
    }

    void OnDisable()
    {
        if (exitController != null && _zoneSubscribed)
        {
            exitController.OnStateChanged -= HandleExitStateChanged;
            _zoneSubscribed = false;
        }
    }

    void OnDestroy()
    {
        if (spinButton != null) spinButton.onClick.RemoveListener(OnSpin);
    }

    void HandleExitStateChanged(ExitFlowState state)
    {
        bool overlayActive = state == ExitFlowState.DeathSkull
                          || state == ExitFlowState.GiveUpConfirm
                          || state == ExitFlowState.CollectConfirm
                          || state == ExitFlowState.FreshStartConfirm;

        if (zoneGroup != null)
        {
            zoneGroup.alpha = overlayActive
                ? DeathOverlayStyle.ZoneBarOverlayAlpha
                : DeathOverlayStyle.ZoneBarPromotedAlpha;

            zoneGroup.blocksRaycasts = !overlayActive;
            zoneGroup.interactable   = !overlayActive;
        }

        if (zoneCanvas != null)
        {
            zoneCanvas.sortingOrder = overlayActive
                ? UICanvasOrders.RewardListBelowOverlay
                : UICanvasOrders.HUDPromoted;
        }
    }

    void Update()
    {
        if (controller == null) return;

        bool flyBusy = spinFlyAnimator != null && spinFlyAnimator.IsBusy;
        SpinButtonAnimator.State target = (controller.CanSpin && !flyBusy)
            ? SpinButtonAnimator.State.Ready
            : SpinButtonAnimator.State.Spinning;

        if (!_buttonStateInitialized || target != _lastTarget)
        {
            if (spinAnimator != null) spinAnimator.SetState(target);
            else if (spinButton != null)
                spinButton.interactable = target == SpinButtonAnimator.State.Ready;
            _lastTarget = target;
        }
        _buttonStateInitialized = true;
    }

    void OnSpin()
    {
        if (controller == null) return;
        controller.RequestSpin();
    }
}
