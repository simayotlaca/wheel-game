using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneChipUI : MonoBehaviour
{
    [Header("Refs (baked into ui_zone_bar_design1 prefab)")]
    [SerializeField] private TMP_Text numberLabel;
    [SerializeField] private Image    bodyImage;
    [SerializeField] private Sprite   spriteNeutral;
    [SerializeField] private Sprite   spriteSafe;
    [SerializeField] private Sprite   spriteSuper;

    [Header("Per-tier body tints")]
    [SerializeField] private Color tintNormal = new Color(0.78f, 0.78f, 0.82f, 1f);
    [SerializeField] private Color tintSafe   = new Color(0.55f, 1.00f, 0.40f, 1f);
    [SerializeField] private Color tintSuper  = new Color(1.00f, 0.92f, 0.30f, 1f);

    private static readonly Color LabelNormal = new Color(0.05f, 0.05f, 0.08f, 1f);
    private static readonly Color LabelSafe   = new Color(0.78f, 1f, 0.65f, 1f);
    private static readonly Color LabelSuper  = Color.white;

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }

    public void SetZone(int zone)
    {
        if (numberLabel != null) numberLabel.text = zone.ToString();
    }

    public void SetZoneType(ZoneType type)
    {
        Sprite sprite;
        Color  tint, label;
        switch (type)
        {
            case ZoneType.Safe:
                sprite = spriteSafe != null ? spriteSafe : spriteNeutral;

                tint   = Color.white;
                label  = LabelSafe;
                break;
            case ZoneType.Super:

                sprite = spriteSuper != null ? spriteSuper : spriteNeutral;
                tint   = spriteSuper != null ? Color.white : tintSuper;
                label  = LabelSuper;
                break;
            default:
                sprite = spriteNeutral;
                tint   = tintNormal;
                label  = LabelNormal;
                break;
        }
        if (bodyImage != null)
        {
            if (sprite != null) bodyImage.sprite = sprite;
            bodyImage.color = tint;
        }
        if (numberLabel != null) numberLabel.color = label;
    }

#if UNITY_EDITOR

    void OnValidate()
    {
        bodyImage.MarkDecorative();
    }
#endif
}
