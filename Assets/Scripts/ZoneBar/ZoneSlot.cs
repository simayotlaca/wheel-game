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

    internal void SetEmpty()
    {
        SetVisible(false);
    }

    internal void SetState(int zone, bool active, bool past, int distance, RewardTier tier)
    {
        bool show_num = !active;
        SetVisible(show_num);

        if (show_num)
        {
            TextTransformer.SetNumber(number_value, zone);
            number_value.color = config.ResolveSlotColor(tier, past, distance);
        }
    }

    private void SetVisible(bool visible)
    {
        slot_base_image.gameObject.SetActive(visible);
        number_value.gameObject.SetActive(visible);
    }
}
}
