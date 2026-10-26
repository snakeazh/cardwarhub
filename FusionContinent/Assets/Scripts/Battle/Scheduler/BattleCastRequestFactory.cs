using FusionContinent;
using UnityEngine;

namespace Game.Battle
{
    public static class BattleCastRequestFactory
    {
        public static BattleCastRequest FromActiveSkillCard(
            SoldierCard soldier,
            int skillId,
            bool useNearestEnemy = false,
            Vector3 targetPosition = default,
            bool deferSkillCardCooldown = false)
        {
            var request = new BattleCastRequest
            {
                Kind = BattleCastKind.FullActiveSkill,
                Priority = ResolvePriority(),
                Caster = soldier,
                SkillId = skillId,
                UseNearestEnemy = useNearestEnemy,
                DeferSkillCardCooldown = deferSkillCardCooldown,
            };

            if (targetPosition != default)
            {
                request.HasTargetPosition = true;
                request.TargetPosition = targetPosition;
            }

            return request;
        }

        public static BattleCastRequest FromNormalAttack(SoldierCard soldier)
        {
            return new BattleCastRequest
            {
                Kind = BattleCastKind.NormalAttack,
                Priority = ResolvePriority(),
                Caster = soldier,
                SkillId = soldier.NormalAttackSkillId,
            };
        }

        public static BattleCastRequest FromSkillGraphPoint(
            int skillId,
            Component caster,
            Vector3 point,
            bool skipBattleRuleBeforeTrigger,
            bool skipBuffOnCast)
        {
            return new BattleCastRequest
            {
                Kind = BattleCastKind.SkillGraphToPoint,
                Priority = BattleCastPriority.Normal,
                Caster = caster as SoldierCard,
                GraphCaster = caster,
                SkillId = skillId,
                HasTargetPosition = true,
                TargetPosition = point,
                SkipBattleRuleBeforeTrigger = skipBattleRuleBeforeTrigger,
                SkipBuffOnCast = skipBuffOnCast,
            };
        }

        public static BattleCastRequest FromSkillGraphTarget(
            int skillId,
            Component caster,
            Component target,
            bool skipBattleRuleBeforeTrigger,
            bool skipBuffOnCast)
        {
            return new BattleCastRequest
            {
                Kind = BattleCastKind.SkillGraphToTarget,
                Priority = BattleCastPriority.Normal,
                Caster = caster as SoldierCard,
                GraphCaster = caster,
                SkillId = skillId,
                SkillGraphTarget = target,
                SkipBattleRuleBeforeTrigger = skipBattleRuleBeforeTrigger,
                SkipBuffOnCast = skipBuffOnCast,
            };
        }

        private static BattleCastPriority ResolvePriority()
        {
            if (Global.AutoReleaseSkillState.Value)
            {
                return BattleCastPriority.Normal;
            }

            return BattleCastPriority.High;
        }
    }
}
