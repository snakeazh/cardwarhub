using FusionContinent.Skill;
using FusionContinent.Skill.Config;
using Game;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 范围伤害处理器
    /// </summary>
    public class AoeDamageHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "AoeDamage";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var hitEffect = node.GetParamString("hit_effect", "");
            var hits = AoeHitUtility.QueryMonsters(node, context);
            AoeHitUtility.AddToHitMonsters(context, hits, clearBeforeAdd: false);

            for (int i = 0; i < hits.Count; i++)
            {
                var hitTarget = hits[i].Monster;
                var damageHitPos = hits[i].HitPoint;

                if (!string.IsNullOrEmpty(hitEffect))
                {
                    EffectManager.Instance.PlayEffect(hitEffect, damageHitPos);
                }

                EventBus.Publish(new ProjectileHitEvent()
                {
                    skillContext = context,
                    hitpoint = damageHitPos,
                    monsterCard = hitTarget
                });
            }

            SkillHitTargetGuard.PruneInvalidHitMonsters(context);
        }
    }
}
