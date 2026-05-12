using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;

public class DeathGameOverPanel : MonoBehaviour
{
    [Header("Exit controller")]
    [SerializeField] private RunExitController exitController;

    [Header("Wheel controller")]
    [SerializeField] private WheelController controller;

    [Header("Animation")]
    [SerializeField] private WheelAnimationConfig animConfig;

    [Header("Layout")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button giveUpButton;
    [SerializeField] private Button reviveButton;

    [Header("Revive cost label")]
    [SerializeField] private TMP_Text reviveCost_value;

    [SerializeField] private CanvasGroup rewardPanelGroup;
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private GameObject deathEdgeGlow;

    [Header("HUD demoter")]
    [SerializeField] private HudOverlayDemoter hudDemoter;

    private bool last_revive_interactable;
    private int last_written_revive_cost = int.MinValue;
    private CanvasGroup revive_button_group;
    private bool had_rewards_at_stake = true;

    private Tween scale_tween;

    void Awake()
    {
        if (exitController == null) {
            Debug.LogError("exitController not assigned", this);
            enabled = false;
            return;
        }
        if (controller == null) {
            Debug.LogError("controller not assigned", this);
            enabled = false;
            return;
        }
        if (animConfig == null) {
            Debug.LogError("animConfig not assigned", this);
            enabled = false;
            return;
        }
        if (panelRoot == null) {
            Debug.LogError("panelRoot not assigned", this);
            enabled = false;
            return;
        }
        if (giveUpButton == null) {
            Debug.LogError("giveUpButton not assigned", this);
            enabled = false;
            return;
        }
        if (reviveButton == null) {
            Debug.LogError("reviveButton not assigned", this);
            enabled = false;
            return;
        }

        revive_button_group = reviveButton.GetComponent<CanvasGroup>();

        giveUpButton.onClick.AddListener(OnGiveUpClicked);
        reviveButton.onClick.AddListener(OnReviveClicked);
    }

    void OnDestroy()
    {
        if (scale_tween.isAlive) scale_tween.Stop();
        if (giveUpButton != null) giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
        if (reviveButton != null) reviveButton.onClick.RemoveListener(OnReviveClicked);
    }

    void OnEnable()
    {
        ApplyHidden();
    }

    void OnDisable()
    {
        if (scale_tween.isAlive) scale_tween.Stop();

        if (hudDemoter != null) hudDemoter.SetForDeath(false);
    }

    void Update()
    {

        if (controller == null) return;
        if (exitController == null) return;
        ExitFlowState state = exitController.State;
        if (state != ExitFlowState.DeathSkull && state != ExitFlowState.GiveUpConfirm) return;

        if (reviveButton != null && controller.Inventory != null && controller.Config != null)
        {
            int cost = controller.CurrentReviveCost;
            bool can_revive = controller.Inventory.Gold >= cost;
            if (can_revive != last_revive_interactable)
            {
                SetReviveAffordable(can_revive);
                last_revive_interactable = can_revive;
            }

            if (reviveCost_value != null && cost != last_written_revive_cost)
            {
                reviveCost_value.SetText("{0}", cost);
                last_written_revive_cost = cost;
            }
        }
    }

    void SetReviveAffordable(bool can_afford)
    {
        if (reviveButton == null) return;
        reviveButton.interactable = can_afford;
        if (revive_button_group != null) {
            if (can_afford) {
                revive_button_group.alpha = 1f;
            }
            else {
                revive_button_group.alpha = 0.5f;
            }
        }
    }

    public void ShowDeathCard()
    {
        if (panelRoot == null) return;

        if (giveUpButton != null) giveUpButton.interactable = true;

        if (cardRoot != null) cardRoot.gameObject.SetActive(true);
        if (panelRoot != null) panelRoot.gameObject.SetActive(true);

        WheelConfig config = null;
        if (controller != null) {
            config = controller.Config;
        }
        if (config != null)
        {
            int cost = controller.CurrentReviveCost;
            if (reviveCost_value != null)
            {
                reviveCost_value.SetText("{0}", cost);
                last_written_revive_cost = cost;
            }

            if (reviveButton != null && controller.Inventory != null)
            {
                bool can_revive = controller.Inventory.Gold >= cost;
                SetReviveAffordable(can_revive);
                last_revive_interactable = can_revive;
            }
        }

        RewardInventory inventory = null;
        if (controller != null) {
            inventory = controller.Inventory;
        }
        had_rewards_at_stake = false;
        if (inventory != null && inventory.Pending != null && inventory.Pending.Count > 0) {
            had_rewards_at_stake = true;
        }

        SetOverlayActive(true);
        SetCardRootX(0f);
        SetRewardPanelVisible(true);

        if (hudDemoter != null) hudDemoter.SetForDeath(true);
        if (deathEdgeGlow != null) deathEdgeGlow.SetActive(true);
        if (scale_tween.isAlive) scale_tween.Stop();
        panelRoot.localScale = Vector3.zero;
        scale_tween = Tween.Scale(panelRoot, Vector3.one, animConfig.panelShowDuration, Ease.OutBack);
    }

    public void SetCardVisible(bool visible)
    {
        if (cardRoot != null) cardRoot.gameObject.SetActive(visible);
        if (deathEdgeGlow != null) deathEdgeGlow.SetActive(visible);
    }

    public void Hide()
    {
        ApplyHidden();
    }

    void OnGiveUpClicked()
    {
        if (exitController == null) return;
        if (!had_rewards_at_stake)
        {
            if (giveUpButton != null) giveUpButton.interactable = false;
            exitController.ConfirmLoseRewards();
            return;
        }
        exitController.PressGiveUp();
    }

    void OnReviveClicked()
    {

        if (reviveButton != null) reviveButton.interactable = false;
        last_revive_interactable = false;

        bool ok = exitController != null && exitController.PressRevive();

        if (!ok && reviveButton != null)
        {
            reviveButton.interactable = true;
            last_revive_interactable = true;
        }
    }

    void SetRewardPanelVisible(bool atStake)
    {
        if (rewardPanelGroup == null) return;
        if (!rewardPanelGroup.gameObject.activeSelf)
            rewardPanelGroup.gameObject.SetActive(true);
        rewardPanelGroup.alpha = DeathOverlayStyle.RewardListOverlayAlpha;
        rewardPanelGroup.interactable = !atStake;
        rewardPanelGroup.blocksRaycasts = !atStake;
    }

    void SetCardRootX(float x)
    {
        if (cardRoot == null) return;
        Vector2 p = cardRoot.anchoredPosition;
        p.x = x;
        cardRoot.anchoredPosition = p;
    }

    void RestoreRewardPanel()
    {
        if (rewardPanelGroup != null)
        {
            if (!rewardPanelGroup.gameObject.activeSelf)
                rewardPanelGroup.gameObject.SetActive(true);
            rewardPanelGroup.alpha = DeathOverlayStyle.RewardListPromotedAlpha;
            rewardPanelGroup.interactable = true;
            rewardPanelGroup.blocksRaycasts = true;
        }
        if (hudDemoter != null) hudDemoter.SetForDeath(false);
    }

    void ApplyHidden()
    {
        if (scale_tween.isAlive) scale_tween.Stop();
        if (panelRoot != null) panelRoot.localScale = Vector3.zero;
        SetOverlayActive(false);
        if (deathEdgeGlow != null) deathEdgeGlow.SetActive(false);
        RestoreRewardPanel();
    }

    private void SetOverlayActive(bool active)
    {
        if (panelRoot == null) return;
        if (panelRoot.parent != null)
            panelRoot.parent.gameObject.SetActive(active);
        panelRoot.gameObject.SetActive(active);
    }

}
