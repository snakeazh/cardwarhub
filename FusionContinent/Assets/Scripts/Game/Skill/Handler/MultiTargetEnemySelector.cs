using System.Collections.Generic;
using FusionContinent.Target;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 多目标敌人选取（技能图 MultiTargetProjectile 等节点使用）。
    /// 实现委托 <see cref="EnemyTargetFinder.SelectEnemies"/>，保持对外 API 不变。
    /// </summary>
    public static class MultiTargetEnemySelector
    {
        /// <summary>
        /// 按距离从近到远选取 count 个不重复敌人；primaryTarget 优先占位。
        /// </summary>
        /// <param name="fromPosition">索敌起点（通常为弹道出生点）</param>
        /// <param name="count">需要的目标数量</param>
        /// <param name="primaryTarget">技能上下文中的主目标，可为 null</param>
        public static List<MonsterCard> SelectNearestEnemies(
            Vector3 fromPosition,
            int count,
            MonsterCard primaryTarget = null)
        {
            return EnemyTargetFinder.SelectEnemies(fromPosition, count, primaryTarget);
        }
    }
}
