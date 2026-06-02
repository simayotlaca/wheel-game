using UnityEngine;

namespace VertigoWheel
{
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Rect last_area;
    private Vector2Int last_screen;

    void Awake()
    {
        rt = (RectTransform)transform;
    }

    void OnEnable()
    {
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        if (rt != null)
        {
            if (Screen.safeArea != last_area ||
                Screen.width != last_screen.x ||
                Screen.height != last_screen.y)
            {
                Apply();
            }
        }
    }

    //i turn safe area pixels into anchors here so panel stays out of notch home bar stuff
    //i remember last screen too because this can get called a lot when layout moves
    private void Apply()
    {
        Rect area = Screen.safeArea;
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (screen.x > 0 && screen.y > 0)
        {
            Vector2 min = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            Vector2 max = new Vector2(area.xMax / screen.x, area.yMax / screen.y);

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            last_area = area;
            last_screen = screen;
        }
    }
}
}
