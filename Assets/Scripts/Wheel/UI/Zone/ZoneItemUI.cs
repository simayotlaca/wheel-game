using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneItemUI : MonoBehaviour
{
    private const float PastAlphaBase = 0.75f;
    private const float PastAlphaStep = 0.18f;
    private const float PastAlphaMin = 0.18f;

    [Header("Wired by ZoneBarBuilder")]
    [SerializeField] private TMP_Text normalNumber_value;
    [SerializeField] private Image slotBaseImage;

    [Header("Colors — overwritten by ZoneBarBuilder")]
    [SerializeField] private Color colorPast   = Color.white;
    [SerializeField] private Color colorFuture = Color.white;
    [SerializeField] private Color colorSafe   = Color.white;
    [SerializeField] private Color colorSuper  = Color.white;

    private int      _zone;
    private bool     _empty = true;
    private bool     _active;
    private ZoneType _tier;
    private bool     _past;
    private int      _pastDistance;

    public void SetEmpty()
    {
        if (_empty) return;
        _empty = true;
        Render();
    }

    public void SetZone(int zone)
    {
        if (!_empty && _zone == zone) return;
        _empty = false;
        _zone = zone;
        Render();
    }

    public void SetActive(bool isActive)
    {
        if (_active == isActive) return;
        _active = isActive;
        Render();
    }

    public void SetTier(ZoneType tier)
    {
        if (_tier == tier) return;
        _tier = tier;
        Render();
    }

    public void SetPast(bool isPast, int distance)
    {
        if (_past == isPast && _pastDistance == distance) return;
        _past = isPast;
        _pastDistance = distance;
        Render();
    }

    private void Render()
    {
        if (_empty)
        {
            SetTMPActive(normalNumber_value, false);
            SetGraphicActive(slotBaseImage, false);
            return;
        }

        SetTMPActive(normalNumber_value, !_active);
        SetGraphicActive(slotBaseImage, !_active);

        if (!_active && normalNumber_value != null)
        {
            normalNumber_value.text  = _zone.ToString();
            normalNumber_value.color = ResolveNormalColor();
        }
    }

    private Color ResolveNormalColor()
    {
        if (_past)
        {

            Color baseColor;
            switch (_tier)
            {
                case ZoneType.Super: baseColor = colorSuper; break;
                case ZoneType.Safe:  baseColor = colorSafe;  break;
                default:             baseColor = colorPast;  break;
            }
            baseColor.a = colorPast.a;
            baseColor.a *= Mathf.Clamp(
                PastAlphaBase - (_pastDistance - 1) * PastAlphaStep,
                PastAlphaMin,
                PastAlphaBase);
            return baseColor;
        }
        switch (_tier)
        {
            case ZoneType.Super: return colorSuper;
            case ZoneType.Safe:  return colorSafe;
            default:             return colorFuture;
        }
    }

    private static void SetTMPActive(TMP_Text t, bool a)
    {
        if (t != null && t.gameObject.activeSelf != a) t.gameObject.SetActive(a);
    }

    private static void SetGraphicActive(Graphic g, bool a)
    {
        if (g != null && g.gameObject.activeSelf != a) g.gameObject.SetActive(a);
    }
}
