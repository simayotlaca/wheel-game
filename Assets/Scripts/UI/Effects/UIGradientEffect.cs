using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheel
{
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIGradientEffect : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (IsActive() && vh.currentVertCount != 0)
        {
            UIVertex v = default;
            float min_y = float.MaxValue;
            float max_y = float.MinValue;
            int count = vh.currentVertCount;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float y = v.position.y;
                if (y < min_y)
                {
                    min_y = y;
                }
                if (y > max_y)
                {
                    max_y = y;
                }
            }

            float range = max_y - min_y;
            if (range > 0f)
            {
                for (int i = 0; i < count; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    float t = (v.position.y - min_y) / range;
                    v.color = Color.Lerp(bottomColor, topColor, t);
                    vh.SetUIVertex(v, i);
                }
            }
        }
    }
}
}
