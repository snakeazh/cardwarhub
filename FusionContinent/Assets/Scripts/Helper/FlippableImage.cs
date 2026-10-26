using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Extensions
{
    [AddComponentMenu("UI/Flippable Image")]
    [DisallowMultipleComponent]
    public class FlippableImage : Image
    {
        [SerializeField] private bool m_FlipX = false;
        [SerializeField] private bool m_FlipY = false;

        public bool flipX
        {
            get => m_FlipX;
            set { if (m_FlipX == value) return; m_FlipX = value; SetVerticesDirty(); }
        }

        public bool flipY
        {
            get => m_FlipY;
            set { if (m_FlipY == value) return; m_FlipY = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            if ((!m_FlipX && !m_FlipY) || vh.currentVertCount == 0)
                return;

            var v = new UIVertex();
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;

            // 先扫描当前 mesh 的 UV 边界
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                var uv = v.uv0;
                if (uv.x < minU) minU = uv.x;
                if (uv.x > maxU) maxU = uv.x;
                if (uv.y < minV) minV = uv.y;
                if (uv.y > maxV) maxV = uv.y;
            }

            // 按边界镜像 UV：new = min + max - old
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                var uv = v.uv0;
                if (m_FlipX) uv.x = minU + maxU - uv.x;
                if (m_FlipY) uv.y = minV + maxV - uv.y;
                v.uv0 = uv;
                vh.SetUIVertex(v, i);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}