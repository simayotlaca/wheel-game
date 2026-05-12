using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPanel : MonoBehaviour
{
    [Header("Exit controller")]
    [SerializeField] private RunExitController exitController;

    [Header("Fresh start")]
    [SerializeField] private GameObject freshStartRoot;
    [SerializeField] private Button freshStartConfirmButton;
    [SerializeField] private Button freshStartCancelButton;

    [Header("Safe exit / collect")]
    [SerializeField] private GameObject safeExitRoot;
    [SerializeField] private Button safeExitConfirmButton;
    [SerializeField] private Button safeExitCancelButton;

    [Header("HUD demoter")]
    [SerializeField] private HudOverlayDemoter hudDemoter;

    void Awake()
    {
        if (freshStartRoot != null) freshStartRoot.SetActive(false);
        if (safeExitRoot   != null) safeExitRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (freshStartConfirmButton != null) freshStartConfirmButton.onClick.AddListener(OnFreshStartConfirm);
        if (freshStartCancelButton  != null) freshStartCancelButton.onClick.AddListener(OnCancel);
        if (safeExitConfirmButton   != null) safeExitConfirmButton.onClick.AddListener(OnSafeExitConfirm);
        if (safeExitCancelButton    != null) safeExitCancelButton.onClick.AddListener(OnCancel);
    }

    void OnDisable()
    {
        if (freshStartConfirmButton != null) freshStartConfirmButton.onClick.RemoveListener(OnFreshStartConfirm);
        if (freshStartCancelButton  != null) freshStartCancelButton.onClick.RemoveListener(OnCancel);
        if (safeExitConfirmButton   != null) safeExitConfirmButton.onClick.RemoveListener(OnSafeExitConfirm);
        if (safeExitCancelButton    != null) safeExitCancelButton.onClick.RemoveListener(OnCancel);

        SetHudDemoted(false);
    }

    public void ShowFreshStart()
    {
        if (freshStartRoot != null) freshStartRoot.SetActive(true);
        if (safeExitRoot   != null) safeExitRoot.SetActive(false);
        SetHudDemoted(true);
    }

    public void ShowSafeExit()
    {
        if (freshStartRoot != null) freshStartRoot.SetActive(false);
        if (safeExitRoot   != null) safeExitRoot.SetActive(true);
        SetHudDemoted(true);
    }

    public void HideAll()
    {
        if (freshStartRoot != null) freshStartRoot.SetActive(false);
        if (safeExitRoot   != null) safeExitRoot.SetActive(false);
        SetHudDemoted(false);
    }

    void OnFreshStartConfirm()
    {
        if (exitController != null) exitController.ConfirmFreshStart();
    }

    void OnSafeExitConfirm()
    {
        if (exitController != null) exitController.ConfirmCollect();
    }

    void OnCancel()
    {
        if (exitController != null) exitController.CancelExit();
    }

    void SetHudDemoted(bool demoted)
    {
        if (hudDemoter != null) hudDemoter.SetDemoted(demoted);
    }
}
