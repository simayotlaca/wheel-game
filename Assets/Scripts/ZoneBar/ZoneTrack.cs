using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class ZoneTrack : MonoBehaviour
{
    #region setup
    [SerializeField] private RunSession controller;
    [SerializeField] private RectTransform items_container;
    [SerializeField] private ZoneStyleConfig config;
    [SerializeField] private CanvasGroup zone_group;
    [SerializeField] private Button spin_button;

    [Header("Chip Refs (baked into ui_zone_bar_design1 prefab)")]
    [SerializeField] private GameObject chip_object;
    [SerializeField] private TMP_Text chip_number_value;
    [SerializeField] private Image chip_body_image;

    private Vector2 base_anchored;
    private int pending_zone;
    private bool is_sliding;
    private Tween slide_tween;
    private bool initial_refreshed;

    [SerializeField] private ZoneSlot[] items;
    #endregion

    void Awake()
    {
        base_anchored = items_container.anchoredPosition;
        spin_button.onClick.AddListener(OnSpin);
    }

    void OnDestroy()
    {
        spin_button.onClick.RemoveListener(OnSpin);
    }

    void OnEnable()
    {
        ClearAllSlots();
        controller.OnZoneChanged += HandleZoneChanged;
        controller.OnStateChanged += HandleRunStateChanged;
        controller.OnBusyChanged += HandleRunAvailabilityChanged;
        TryInitialRefresh();
        RefreshSpinButton();
    }

    void Start()
    {
        TryInitialRefresh();
    }

    void OnDisable()
    {
        controller.OnZoneChanged -= HandleZoneChanged;
        controller.OnStateChanged -= HandleRunStateChanged;
        controller.OnBusyChanged -= HandleRunAvailabilityChanged;
        TweenLifetime.StopIfAlive(slide_tween);
        is_sliding = false;
        initial_refreshed = false;
        items_container.anchoredPosition = base_anchored;
    }

    public void SetInteractive(bool active)
    {
        zone_group.alpha = 1f;
        zone_group.blocksRaycasts = active;
        zone_group.interactable = active;
    }

    private void OnSpin()
    {
        controller.RequestSpin();
    }

    private void TryInitialRefresh()
    {
        if (!initial_refreshed && controller.Zones != null)
        {
            Refresh(controller.Zones.CurrentZone);
            initial_refreshed = true;
        }
    }

    private void HandleZoneChanged(int zone)
    {
        if (is_sliding)
        {
            ApplyPendingZone();
        }
        StartSlide(zone);
        RefreshSpinButton();
    }

    private void HandleRunStateChanged(RunState _)
    {
        RefreshSpinButton();
    }

    private void HandleRunAvailabilityChanged()
    {
        RefreshSpinButton();
    }

    private void RefreshSpinButton()
    {
        bool can_spin = controller.CanSpin;
        if (spin_button.interactable != can_spin)
            spin_button.interactable = can_spin;
    }

    private void StartSlide(int new_zone)
    {
        if (config.slideDuration <= 0f)
        {
            pending_zone = new_zone;
            is_sliding = true;
            ApplyPendingZone();
        }
        else
        {
            UpdateChipForZone(new_zone);

            int anchor = Mathf.Clamp(config.activeSlotIndex, 0, items.Length - 1);
            items[anchor].MarkPast(1);

            pending_zone = new_zone;
            is_sliding = true;

            items_container.anchoredPosition = base_anchored;
            Vector2 to = base_anchored + new Vector2(-config.slideAmount, 0f);

            slide_tween = Tween.UIAnchoredPosition(items_container, to, config.slideDuration, Ease.OutCubic)
                .OnComplete(ApplyPendingZone);
        }
    }

    private void ApplyPendingZone()
    {
        if (is_sliding)
        {
            is_sliding = false;
            Refresh(pending_zone);
            items_container.anchoredPosition = base_anchored;
        }
    }

    private void UpdateChipForZone(int zone)
    {
        int first_zone = controller.Zones.FirstZoneIndex;
        SetChipVisible(zone >= first_zone);

        if (zone >= first_zone)
        {
            SetChipZone(zone);
            SetChipTier(controller.Zones.GetZoneTier(zone));
        }
    }

    private void SetChipVisible(bool visible)
    {
        if (chip_object.activeSelf != visible)
        {
            chip_object.SetActive(visible);
        }
    }

    private void SetChipZone(int zone)
    {
        chip_number_value.text = zone.ToString();
    }

    private void SetChipTier(RewardTier tier)
    {
        Sprite sprite = config.spriteNeutral;
        Color tint = config.tintNormal;
        Color label = config.labelNormal;

        switch (tier)
        {
            case RewardTier.Safe:
                if (config.spriteSafe != null)
                {
                    sprite = config.spriteSafe;
                }
                tint = Color.white;
                label = config.labelSafe;
                break;

            case RewardTier.Super:
                if (config.spriteSuper != null)
                {
                    sprite = config.spriteSuper;
                }
                tint = Color.white;
                label = config.labelSuper;
                break;
        }

        if (sprite != null)
        {
            chip_body_image.sprite = sprite;
        }

        chip_body_image.color = tint;
        chip_number_value.color = label;
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].SetEmpty();
        }
    }

    private void Refresh(int current_zone)
    {
        if (items.Length != 0)
        {
            int total = items.Length;
            int anchor = Mathf.Clamp(config.activeSlotIndex, 0, total - 1);
            int first_zone = controller.Zones.FirstZoneIndex;
            int max_zone = controller.Zones.MaxZoneIndex;
            int zone_count = max_zone - first_zone + 1;

            for (int i = 0; i < total; i++)
            {
                ZoneSlot item = items[i];
                int offset = i - anchor;
                int zone = current_zone + offset;

                if (zone < first_zone)
                {
                    item.SetEmpty();
                }
                else
                {
                    if (zone > max_zone)
                    {
                        zone = first_zone + ((zone - first_zone) % zone_count);
                    }
                    RewardTier t = controller.Zones.GetZoneTier(zone);
                    bool is_active = offset == 0;
                    bool is_past = offset < 0;
                    int past_dist = is_past ? -offset : 0;

                    item.SetState(zone, is_active, is_past, past_dist, t);
                }
            }

            UpdateChipForZone(current_zone);
        }
    }
}
}
