using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Skill.Config;
using Game;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// AOE 范围检测共用逻辑（AoeDamage / AoeSelect）
    /// </summary>
    public static class AoeHitUtility
    {
        public struct AoeMonsterHit
        {
            public MonsterCard Monster;
            public Vector3 HitPoint;
        }

        public static List<AoeMonsterHit> QueryMonsters(SkillNodeConfig node, SkillContext context)
        {
            var shape = node.GetParamString("shape", "Circle");
            var range = node.GetParamFloat("range", 3f);
            var rangeAttr = node.GetParamString("range_attr", "");
            var rectWidth = node.GetParamFloat("rect_width", 10f);
            var rectHeight = node.GetParamFloat("rect_height", 2f);
            var centerSource = node.GetParamString("center_source", "Caster");

            if (!string.IsNullOrEmpty(rangeAttr) && context.Caster != null)
            {
                var soldierCard = context.Caster.GetComponent<SoldierCard>();
                if (soldierCard != null)
                {
                    var skillCard = soldierCard.GetSkillById(context.SkillId);
                    if (skillCard != null && Enum.TryParse<AttributeType>(rangeAttr, out var attrType))
                    {
                        range = skillCard.Attributes.GetFinal(attrType);
                    }
                }
            }

            Vector3 center;
            if (centerSource == "Caster")
            {
                center = context.Caster.transform.position + new Vector3(context.EffectOffset.x, context.EffectOffset.y, 0);
            }
            else if (centerSource == "Target")
            {
                // 与 EffectHandler(position=Target) 一致：目标位置来自 TargetPosition，而非 context.Target
                center = context.TargetPosition + new Vector3(context.EffectOffset.x, context.EffectOffset.y, 0);
            }
            else
            {
                center = context.TargetPosition + new Vector3(context.EffectOffset.x, context.EffectOffset.y, 0);
            }

            float facingAngle = 90f;
            Vector2 facingForward = Vector2.up;
            Vector2 dir = Vector2.right;
            if (context.Caster != null)
            {
                dir = ((Vector2)context.TargetPosition).normalized;
                facingAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
                facingForward = new Vector2(-dir.y, dir.x);
            }

            List<HitResult> hits = null;
            if (shape == "Circle")
            {
                hits = HitSystem2D.Circle(center, range, LayerMask.GetMask("MonsterBox"));
            }
            else if (shape == "Rectangle")
            {
                hits = HitSystem2D.Box(center, new Vector2(rectWidth, rectHeight), facingAngle, LayerMask.GetMask("MonsterBox"));
            }
            else if (shape == "Fan")
            {
                hits = HitSystem2D.Sector(center, facingForward, range, node.GetParamFloat("fan_angle", 60f), LayerMask.GetMask("MonsterBox"));
            }

            if (hits == null)
                return new List<AoeMonsterHit>();

            var result = new List<AoeMonsterHit>(hits.Count);
            for (int i = 0; i < hits.Count; i++)
            {
                MonsterCard hitTarget = hits[i].target.transform.parent.GetComponent<MonsterCard>();
                if (hitTarget == null || hitTarget.enabled == false)
                    continue;

                result.Add(new AoeMonsterHit
                {
                    Monster = hitTarget,
                    HitPoint = hits[i].hitPoint
                });
            }

            return result;
        }

        public static void AddToHitMonsters(SkillContext context, List<AoeMonsterHit> hits, bool clearBeforeAdd)
        {
            if (clearBeforeAdd)
            {
                context.HitMonsters.Clear();
                context.HitMonsterSnapshotIds.Clear();
            }

            for (int i = 0; i < hits.Count; i++)
            {
                context.RegisterHitMonster(hits[i].Monster);
            }
        }
    }
}
