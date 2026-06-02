using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
[RequireComponent(typeof(CanvasGroup), typeof(Image))]
public class RewardFlyIcon : MonoBehaviour
{
    [SerializeField] private RectTransform rt;
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup cg;

    public RectTransform Rect => rt;

    public CanvasGroup CanvasGroup => cg;

    public void Configure(Sprite sprite, Vector2 size, Color tint)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.color = tint;

        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        cg.alpha = 1f;
    }
}
}
