using UnityEngine;

namespace FusionContinent.Target
{
    /// <summary>
    /// 单次攻击/技能释放的目标快照（由原 TarGetStruct 迁移）。
    /// 在动画播放与命中帧之间传递，避免目标中途死亡后丢失落点。
    /// </summary>
    public struct BattleTargetInfo
    {
        /// <summary>是否为怪物目标；false 表示仅使用 targetPosition（如地面点）</summary>
        public bool isEnemy;

        /// <summary>
        /// 目标世界坐标（已含 GlobalConst.monsterPosOffset）。
        /// 敌人死亡后仍使用创建时缓存的坐标作为兜底落点。
        /// </summary>
        public Vector3 targetPosition;

        /// <summary>怪物引用；非敌人目标或仅坐标释放时为 null</summary>
        public MonsterCard target;

        /// <summary>
        /// 创建快照时的怪物 uniqueId。
        /// 对象池复用同一 MonsterCard 后 uniqueId 会变化，用于区分「原目标」与「池化新怪」。
        /// </summary>
        public string targetUniqueId;
    }
}
