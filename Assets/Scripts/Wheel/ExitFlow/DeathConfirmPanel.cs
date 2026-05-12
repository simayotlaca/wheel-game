using UnityEngine;
using UnityEngine.UI;

public class DeathConfirmPanel : MonoBehaviour
{
    [Header("Exit controller")]
    [SerializeField] private RunExitController exitController;

    [Header("Buttons")]
    [SerializeField] private Button loseRewardsButton;
    [SerializeField] private Button goBackButton;

    [Header("Shared overlay")]
    [SerializeField] private Image sharedBackdrop;

    void Awake()
    {
        if (loseRewardsButton != null) loseRewardsButton.onClick.AddListener(OnLoseRewardsClicked);
        if (goBackButton != null) goBackButton.onClick.AddListener(OnGoBackClicked);
    }

    void OnDestroy()
    {
        if (loseRewardsButton != null) loseRewardsButton.onClick.RemoveListener(OnLoseRewardsClicked);
        if (goBackButton != null) goBackButton.onClick.RemoveListener(OnGoBackClicked);
    }

    public void Show()
    {
        if (loseRewardsButton != null) loseRewardsButton.interactable = true;
        if (goBackButton != null) goBackButton.interactable = true;
        if (sharedBackdrop != null) sharedBackdrop.color = DeathOverlayStyle.GiveUpBackdropDim;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (sharedBackdrop != null) sharedBackdrop.color = DeathOverlayStyle.MainBackdropDim;
        gameObject.SetActive(false);
    }

    void OnLoseRewardsClicked()
    {

        if (loseRewardsButton != null) loseRewardsButton.interactable = false;
        if (exitController != null) exitController.ConfirmLoseRewards();
    }

    void OnGoBackClicked()
    {
        if (exitController != null) exitController.CancelGiveUp();
    }

}
