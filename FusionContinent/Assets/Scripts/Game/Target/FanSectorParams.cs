using UnityEngine;

namespace FusionContinent.Target
{
    /// <summary>
    /// 扇形索敌参数。由「初始点 → 中点」确定扇形朝向，中点落在扇形中心轴上。
    /// </summary>
    public struct FanSectorParams
    {
        /// <summary>扇形顶点（初始点）</summary>
        public Vector3 Origin;

        /// <summary>扇形中心轴上的参考点（中点），与 Origin 共同决定朝向</summary>
        public Vector3 CenterPoint;

        /// <summary>扇形总张角（度），默认 30°（中心轴左右各一半）</summary>
        public float AngleDegrees;

        /// <summary>最大检测距离；≤0 表示不限制距离，仅按角度过滤</summary>
        public float MaxDistance;

        public FanSectorParams(Vector3 origin, Vector3 centerPoint, float angleDegrees = 30f, float maxDistance = 0f)
        {
            Origin = origin;
            CenterPoint = centerPoint;
            AngleDegrees = angleDegrees;
            MaxDistance = maxDistance;
        }
    }
}
