using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Horizontal Gradient")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIHorizontalGradient : BaseMeshEffect
{
    public Color leftColor = Color.white;
    public Color rightColor = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex v = default;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        int count = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float x = v.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        float range = maxX - minX;
        if (range <= 0f) return;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float t = (v.position.x - minX) / range;
            v.color = Color.Lerp(leftColor, rightColor, t);
            vh.SetUIVertex(v, i);
        }
    }
}
