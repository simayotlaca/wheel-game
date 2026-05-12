using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup), typeof(Image))]
public class RewardFlyIcon : MonoBehaviour
{
    private RectTransform rt;
    private Image image;
    private CanvasGroup cg;

    public RectTransform Rect => rt;
    public CanvasGroup CanvasGroup => cg;

    void Awake()
    {
        rt = transform as RectTransform;
        image = GetComponent<Image>();
        cg = GetComponent<CanvasGroup>();
    }

    public void Configure(Sprite sprite, Vector2 size)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        cg.alpha = 1f;
    }
}
