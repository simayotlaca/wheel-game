using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
public class ZoneTrack : MonoBehaviour
{
    [SerializeField] private RunSession controller;
    [SerializeField] private ZoneStyleConfig config;
    [SerializeField] private CanvasGroup zone_group;
    [SerializeField] private Button spin_button;

    [Header("Chip Refs (baked into ui_zone_bar_design1 prefab)")]
    [SerializeField] private GameObject chip_object;
    [SerializeField] private TMP_Text chip_number_value;
    [SerializeField] private Image chip_body_image;

    private RunEventPass event_pass;

    [SerializeField] private ZoneSlot[] items;

#if UNITY_EDITOR
    private void OnValidate()
    {
        BindSceneComponent(ref spin_button, "ui_button_spin_center");
    }

    private void BindSceneComponent<T>(ref T target, string object_name) where T : Component
    {
        T component = FindSceneComponent<T>(object_name);
        if (component != null)
        {
            target = component;
        }
    }

    private T FindSceneComponent<T>(string object_name) where T : Component
    {
        if (!gameObject.scene.IsValid()) return null;

        T[] components = FindObjectsOfType<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null) continue;
            if (component.gameObject.scene != gameObject.scene) continue;
            if (component.gameObject.name != object_name) continue;
            return component;
        }

        return null;
    }
#endif

    void Awake()
    {
        event_pass = new RunEventPass(controller.Events);
        spin_button.onClick.AddListener(OnSpin);
    }

    void OnDestroy()
    {
        spin_button.onClick.RemoveListener(OnSpin);
    }

    void OnEnable()
    {
        event_pass.Subscribe<RunZoneChangedEvent>(HandleZoneChanged);
        event_pass.Subscribe<RunStateChangedEvent>(HandleRunStateChanged);
        Refresh(controller.CurrentZone);
        RefreshSpinButton();
    }

    void OnDisable()
    {
        event_pass.ReleaseAll();
    }

    internal void SetInteractive(bool active)
    {
        zone_group.blocksRaycasts = active;
        zone_group.interactable = active;
    }

    private void OnSpin()
    {
        controller.RequestSpin();
    }

    private void HandleZoneChanged(RunZoneChangedEvent evt)
    {
        Refresh(evt.current_zone);
        RefreshSpinButton();
    }

    private void HandleRunStateChanged(RunStateChangedEvent evt)
    {
        spin_button.interactable = GameRules.CanTransition(evt.current_state, RunState.Spinning);
    }

    private void RefreshSpinButton()
    {
        spin_button.interactable = controller.CanSpin;
    }

    private void UpdateChipForZone(int zone)
    {
        chip_object.SetActive(true);
        TextTransformer.SetNumber(chip_number_value, zone);
        RewardTier tier = controller.GetZoneTier(zone);
        ZoneChipStyle style = config.ResolveChipStyle(tier);
        chip_body_image.sprite = style.sprite;
        chip_body_image.color = style.body_tint;
        chip_number_value.color = style.label_color;
    }

    private void Refresh(int current_zone)
    {
        int anchor = items.Length / 2;
        int first_zone = controller.FirstZoneIndex;
        int max_zone = controller.MaxZoneIndex;
        int zone_count = max_zone - first_zone + 1;

        for (int i = 0; i < items.Length; i++)
        {
            ZoneSlot item = items[i];
            int offset = i - anchor;
            int zone = current_zone + offset;

            if (zone < first_zone)
            {
                item.SetEmpty();
                continue;
            }

            if (zone > max_zone)
            {
                zone = first_zone + ((zone - first_zone) % zone_count);
            }

            RewardTier t = controller.GetZoneTier(zone);
            bool is_active = offset == 0;
            bool is_past = offset < 0;
            int past_dist = is_past ? -offset : 0;

            item.SetState(zone, is_active, is_past, past_dist, t);
        }

        UpdateChipForZone(current_zone);
    }
}
}
