using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Vertical Gradient")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIVerticalGradient : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex v = default;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        int count = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float y = v.position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        float range = maxY - minY;
        if (range <= 0f) return;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float t = (v.position.y - minY) / range;
            v.color = Color.Lerp(bottomColor, topColor, t);
            vh.SetUIVertex(v, i);
        }
    }
}
