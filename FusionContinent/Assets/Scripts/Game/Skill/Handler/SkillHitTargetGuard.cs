using Config;
using FusionContinent;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能命中目标校验：防止怪物死亡/对象池复用后，后续 Buff 节点误作用于新怪物。
    /// </summary>
    public static class SkillHitTargetGuard
    {
        public static bool IsMonsterAlive(MonsterCard monster)
        {
            return monster != null
                && monster.enabled
                && monster.monsterData.monsterState != MonsterState.die;
        }

        /// <summary>
        /// 是否仍可对怪物施加 Buff（存活 + uniqueId 与快照一致）。
        /// </summary>
        public static bool CanApplyBuffToMonster(MonsterCard monster, SkillContext context)
        {
            if (!IsMonsterAlive(monster))
            {
                return false;
            }

            if (context != null
                && context.HitMonsterSnapshotIds.TryGetValue(monster, out string snapshotId)
                && monster.uniqueId != snapshotId)
            {
                return false;
            }

            return true;
        }

        public static void SnapshotMonster(SkillContext context, MonsterCard monster)
        {
            if (context == null || monster == null)
            {
                return;
            }

            context.HitMonsterSnapshotIds[monster] = monster.uniqueId;
        }

        public static void PruneInvalidHitMonsters(SkillContext context)
        {
            if (context == null)
            {
                return;
            }

            for (int i = context.HitMonsters.Count - 1; i >= 0; i--)
            {
                if (!CanApplyBuffToMonster(context.HitMonsters[i], context))
                {
                    context.HitMonsters.RemoveAt(i);
                }
            }
        }
    }
}
