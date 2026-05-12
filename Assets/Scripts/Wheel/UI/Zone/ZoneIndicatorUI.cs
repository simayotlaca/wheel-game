using PrimeTween;
using UnityEngine;

namespace Wheel.UI.Zone
{

    public class ZoneIndicatorUI : MonoBehaviour
    {
        [SerializeField] private WheelController controller;
        [SerializeField] private RectTransform   itemsContainer;
        [SerializeField] private ZoneChipUI      chip;
        [SerializeField] private int             activeSlotIndex = 6;

        [Header("Slide animation")]
        [SerializeField, Min(0f)] private float slideDuration = 0.24f;
        [SerializeField, Min(0f)] private float slideAmount   = 68f;

        private Vector2 _baseAnchored;
        private int     _pendingZone;
        private bool    _isSliding;
        private Tween   _slideTween;

        private ZoneItemUI[] items;

        void Awake()
        {
            if (controller == null)     Debug.LogError("ZoneIndicatorUI: controller not wired.", this);
            if (itemsContainer == null) Debug.LogError("ZoneIndicatorUI: itemsContainer not wired.", this);
            if (chip == null)           Debug.LogError("ZoneIndicatorUI: chip not wired.", this);
            if (itemsContainer != null) _baseAnchored = itemsContainer.anchoredPosition;
        }

        void OnEnable()
        {
            GatherItems();
            ClearAllSlots();
            if (controller != null)
            {
                controller.OnZoneChanged += HandleZoneChanged;
                int z = controller.Zones != null ? controller.Zones.CurrentZone : 1;
                Refresh(z);
            }
        }

        void OnDisable()
        {
            if (controller != null) controller.OnZoneChanged -= HandleZoneChanged;
            if (_slideTween.isAlive) _slideTween.Stop();
            _isSliding = false;
            if (itemsContainer != null) itemsContainer.anchoredPosition = _baseAnchored;
        }

        void HandleZoneChanged(int zone, ZoneType type)
        {

            if (_isSliding) ApplyPendingZone();
            StartSlide(zone);
        }

        void StartSlide(int newZone)
        {
            if (itemsContainer == null || slideDuration <= 0f)
            {
                _pendingZone = newZone;
                _isSliding   = true;
                ApplyPendingZone();
                return;
            }

            UpdateChipForZone(newZone);

            if (items != null)
            {
                int anchor = Mathf.Clamp(activeSlotIndex, 0, items.Length - 1);
                if (items[anchor] != null)
                {
                    items[anchor].SetActive(false);
                    items[anchor].SetPast(true, 1);
                }
            }

            _pendingZone = newZone;
            _isSliding   = true;
            itemsContainer.anchoredPosition = _baseAnchored;

            Vector2 to = _baseAnchored + new Vector2(-slideAmount, 0f);
            _slideTween = Tween.UIAnchoredPosition(itemsContainer, to, slideDuration, Ease.OutCubic, useUnscaledTime: true)
                .OnComplete(ApplyPendingZone);
        }

        void ApplyPendingZone()
        {
            if (!_isSliding) return;
            _isSliding = false;
            Refresh(_pendingZone);
            if (itemsContainer != null) itemsContainer.anchoredPosition = _baseAnchored;
        }

        void UpdateChipForZone(int zone)
        {
            if (chip == null || controller == null || controller.Config == null) return;
            chip.SetVisible(zone >= 1);
            if (zone < 1) return;
            chip.SetZone(zone);
            chip.SetZoneType(controller.Config.GetZoneType(zone));
        }

        void GatherItems()
        {
            if (itemsContainer == null) { items = null; return; }
            int count = itemsContainer.childCount;
            items = new ZoneItemUI[count];
            for (int i = 0; i < count; i++)
                items[i] = itemsContainer.GetChild(i).GetComponent<ZoneItemUI>();
        }

        void ClearAllSlots()
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null) items[i].SetEmpty();
        }

        void Refresh(int currentZone)
        {
            if (items == null || items.Length == 0) return;
            if (controller == null || controller.Config == null) return;

            int total  = items.Length;
            int anchor = Mathf.Clamp(activeSlotIndex, 0, total - 1);

            for (int i = 0; i < total; i++)
            {
                ZoneItemUI item = items[i];
                if (item == null) continue;

                int zone = currentZone + (i - anchor);
                if (zone < 1) { item.SetEmpty(); continue; }

                ZoneType t   = controller.Config.GetZoneType(zone);
                bool isActive = i == anchor;
                bool isPast   = zone < currentZone;
                int  pastDist = isPast ? currentZone - zone : 0;

                item.SetZone(zone);
                item.SetActive(isActive);
                item.SetPast(isPast, pastDist);
                item.SetTier(t);
            }

            UpdateChipForZone(currentZone);
        }
    }
}
