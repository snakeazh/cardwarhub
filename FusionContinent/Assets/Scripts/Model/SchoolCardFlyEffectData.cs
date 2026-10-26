using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.Model
{
    /// <summary>
    /// 流派卡牌飞入特效数据
    /// </summary>
    public class SchoolCardFlyEffectData
    {
        /// <summary>
        /// 起始位置（世界坐标）
        /// </summary>
        public Vector3 StartPosition { get; set; }

        /// <summary>
        /// 流派卡ID（SchoolCardConfig.Id）
        /// </summary>
        public int SchoolCardId { get; set; }

        /// <summary>
        /// 流派ID列表
        /// </summary>
        public List<int> SchoolIds { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public float Duration { get; set; }

        public SchoolCardFlyEffectData(Vector3 startPosition, int schoolCardId, float duration)
        {
            StartPosition = startPosition;
            SchoolCardId = schoolCardId;
            SchoolIds = null;
            Duration = duration;
        }

        public SchoolCardFlyEffectData(Vector3 startPosition, List<int> schoolIds, float duration)
        {
            StartPosition = startPosition;
            SchoolCardId = 0;
            SchoolIds = schoolIds;
            Duration = duration;
        }

    }
}
