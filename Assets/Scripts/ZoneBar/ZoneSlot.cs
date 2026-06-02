using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VertigoWheel
{
public class ZoneSlot : MonoBehaviour
{
    [SerializeField] private ZoneStyleConfig config;

    [Header("Refs")]
    [SerializeField] private TMP_Text number_value;
    [SerializeField] private Image slot_base_image;

    private int zone;
    private bool is_empty = true;
    private bool is_active;
    private RewardTier tier;
    private bool is_past;
    private int past_distance;

    //i only redraw when this slot changed, zone bar moves a lot so i kept it cheap
    public void SetEmpty()
    {
        if (!is_empty)
        {
            is_empty = true;
        }
        Render();
    }

    public void SetState(int new_zone, bool active, bool past, int distance, RewardTier new_tier)
    {
        bool changed = is_empty
            || zone != new_zone
            || is_active != active
            || is_past != past
            || past_distance != distance
            || tier != new_tier;

        if (changed)
        {
            is_empty = false;
            zone = new_zone;
            is_active = active;
            is_past = past;
            past_distance = distance;
            tier = new_tier;
            Render();
        }
    }

    public void MarkPast(int distance)
    {
        if (is_active || !is_past || past_distance != distance)
        {
            is_active = false;
            is_past = true;
            past_distance = distance;
            Render();
        }
    }

    private void Render()
    {
        bool show_num = !is_empty && !is_active;
        SetActive(slot_base_image.gameObject, show_num);
        SetActive(number_value.gameObject, show_num);

        if (show_num)
        {
            number_value.SetText("{0}", zone);
            number_value.color = GetColor();
        }
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go.activeSelf != active)
        {
            go.SetActive(active);
        }
    }

    //past zones fade by distance, the one we just left stays stronger
    //older ones go softer so they dont all look like same old slot
    private Color GetColor()
    {
        if (is_past)
        {
            Color c = tier switch
            {
                RewardTier.Super => config.tintSuper,
                RewardTier.Safe  => config.tintSafe,
                _                => config.colorPast
            };

            float alpha = config.pastAlphaBase - (past_distance - 1) * config.pastAlphaStep;
            alpha = Mathf.Clamp(alpha, config.pastAlphaMin, config.pastAlphaBase);
            c.a = config.colorPast.a * alpha;

            return c;
        }

        return tier switch
        {
            RewardTier.Super => config.tintSuper,
            RewardTier.Safe  => config.tintSafe,
            _                => config.colorFuture
        };
    }
}
}
