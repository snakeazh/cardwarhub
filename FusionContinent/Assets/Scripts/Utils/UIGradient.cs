using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 图片渐变特效：按指定角度对 Graphic 的顶点颜色做双色渐变。
/// 挂在带有 Image / Text 等 Graphic 组件的物体上即可生效，颜色会与原顶点色相乘。
/// </summary>
[AddComponentMenu("UI/Effects/UIGradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [SerializeField]
    [Tooltip("渐变起始颜色（渐变方向起点）")]
    private Color mColor1 = Color.white;
    [SerializeField]
    [Tooltip("渐变结束颜色（渐变方向终点）")]
    private Color mColor2 = Color.white;
    [SerializeField, Range(-180f, 180f)]
    [Tooltip("渐变角度：0 = 自下而上，90 = 自左而右")]
    private float mAngle = 0f;
    [SerializeField]
    [Tooltip("勾选后忽略图片宽高比，渐变沿屏幕方向均匀分布")]
    private bool mIgnoreRatio = true;

    public Color Color1
    {
        get { return mColor1; }
        set { mColor1 = value; Refresh(); }
    }

    public Color Color2
    {
        get { return mColor2; }
        set { mColor2 = value; Refresh(); }
    }

    public float Angle
    {
        get { return mAngle; }
        set { mAngle = value; Refresh(); }
    }

    public bool IgnoreRatio
    {
        get { return mIgnoreRatio; }
        set { mIgnoreRatio = value; Refresh(); }
    }

    private void Refresh()
    {
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!enabled) return;

        Rect rect = graphic.rectTransform.rect;

        // 渐变方向：0 度为垂直向上
        float rad = mAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        if (!mIgnoreRatio)
        {
            dir.x *= rect.width;
            dir.y *= rect.height;
            dir.Normalize();
        }

        // 顶点在渐变方向上的投影范围
        UIVertex vertex = default;
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float proj = Vector2.Dot(vertex.position, dir);
            min = Mathf.Min(min, proj);
            max = Mathf.Max(max, proj);
        }

        float range = max - min;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = range > 0f ? (Vector2.Dot(vertex.position, dir) - min) / range : 0f;
            vertex.color *= Color.Lerp(mColor2, mColor1, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
