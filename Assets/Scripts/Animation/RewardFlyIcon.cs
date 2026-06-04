using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
[System.Serializable]
public struct RewardFlyMotionSettings
{
    [Min(0.05f)] public float duration;
    public bool use_unscaled_time;
    public AnimationCurve travel_curve;
    public AnimationCurve offset_curve;
    public AnimationCurve scale_curve;
    public AnimationCurve alpha_curve;
    public Vector2[] icon_offsets;

    internal int IconCount
    {
        get
        {
            return icon_offsets.Length;
        }
    }

    internal Vector3 GetIconOffset(int index)
    {
        Vector2 offset = icon_offsets[index];
        return new Vector3(offset.x, offset.y, 0f);
    }
}

[RequireComponent(typeof(CanvasGroup), typeof(Image))]
public class RewardFlyIcon : MonoBehaviour
{
    [SerializeField] private RectTransform rt;
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private RewardFlyMotionSettings motion;

    internal RectTransform Rect
    {
        get
        {
            return rt;
        }
    }

    internal CanvasGroup CanvasGroup
    {
        get
        {
            return cg;
        }
    }

    internal RewardFlyMotionSettings Motion
    {
        get
        {
            return motion;
        }
    }

    internal int IconCount
    {
        get
        {
            return motion.IconCount;
        }
    }

    internal void Configure(Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = true;

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        cg.alpha = 1f;
    }
}
}
