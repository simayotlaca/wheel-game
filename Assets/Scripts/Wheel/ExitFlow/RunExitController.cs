using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class RunExitController : MonoBehaviour
{
    [Header("Game references")]
    [SerializeField] private WheelController wheel;
    [SerializeField] private ExitConfirmPanel exitConfirmPanel;
    [SerializeField] private DeathGameOverPanel deathPanel;
    [SerializeField] private DeathConfirmPanel deathConfirmPanel;
    [SerializeField] private RewardCollectAnimator collectAnimator;

    [Header("Exit button")]
    [SerializeField] private Button exitButton;

    private ExitFlowState state = ExitFlowState.None;

    private bool _reviveInFlight;

    public ExitFlowState State => state;

    public event Action<ExitFlowState> OnStateChanged;

    void OnEnable()
    {
        if (wheel != null) wheel.OnDeathHit += HandleDeathHit;
        if (exitButton != null) exitButton.onClick.AddListener(PressExit);
    }

    void OnDisable()
    {
        if (wheel != null) wheel.OnDeathHit -= HandleDeathHit;
        if (exitButton != null) exitButton.onClick.RemoveListener(PressExit);
    }

    public void PressExit()
    {
        if (wheel == null) return;

        ExitKind kind = ExitContext.Classify(wheel);

        if (kind == ExitKind.FreshStart)
        {
            SetState(ExitFlowState.FreshStartConfirm);
            if (exitConfirmPanel != null) exitConfirmPanel.ShowFreshStart();
        }
        else if (kind == ExitKind.SafeExit)
        {
            SetState(ExitFlowState.CollectConfirm);
            if (exitConfirmPanel != null) exitConfirmPanel.ShowSafeExit();
        }
        else
        {

            NotifyExitUnavailable();
        }
    }

    public event Action OnExitUnavailable;

    private void NotifyExitUnavailable()
    {
        OnExitUnavailable?.Invoke();
        if (exitButton != null)
        {
            Tween.PunchScale(exitButton.transform, new Vector3(0.08f, 0.08f, 0f), 0.25f, useUnscaledTime: true);
        }
    }

    public void ConfirmFreshStart()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.HideAll();
        SetState(ExitFlowState.None);
        if (wheel != null) wheel.Restart();
    }

    public void ConfirmCollect()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.HideAll();
        SetState(ExitFlowState.None);
        if (collectAnimator != null) collectAnimator.PlayCollectAndLeave();
    }

    public void CancelExit()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.HideAll();
        SetState(ExitFlowState.None);
    }

    public void PressGiveUp()
    {
        SetState(ExitFlowState.GiveUpConfirm);
        if (deathPanel != null)        deathPanel.SetCardVisible(false);
        if (deathConfirmPanel != null) deathConfirmPanel.Show();
    }

    public void ConfirmLoseRewards()
    {
        if (deathConfirmPanel != null) deathConfirmPanel.Hide();
        if (deathPanel != null)        deathPanel.Hide();
        SetState(ExitFlowState.None);
        if (wheel != null) wheel.Restart();
    }

    public void CancelGiveUp()
    {
        SetState(ExitFlowState.DeathSkull);
        if (deathConfirmPanel != null) deathConfirmPanel.Hide();
        if (deathPanel != null)        deathPanel.ShowDeathCard();
    }

    public bool PressRevive()
    {
        if (wheel == null) return false;
        if (_reviveInFlight) return false;
        _reviveInFlight = true;
        if (!wheel.TryRevive()) { _reviveInFlight = false; return false; }
        if (deathConfirmPanel != null) deathConfirmPanel.Hide();
        if (deathPanel != null)        deathPanel.Hide();
        SetState(ExitFlowState.None);
        return true;
    }

    private void HandleDeathHit()
    {

        _reviveInFlight = false;
        SetState(ExitFlowState.DeathSkull);
        if (deathConfirmPanel != null) deathConfirmPanel.Hide();
        if (deathPanel != null)        deathPanel.ShowDeathCard();
    }

    private void SetState(ExitFlowState newState)
    {
        if (state == newState) return;
        state = newState;
        OnStateChanged?.Invoke(state);
    }

}
