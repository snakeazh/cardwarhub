using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Skill.Config;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 多目标投射物节点：同时向多个最近敌人各发射一枚弹道
    /// </summary>
    public class MultiTargetProjectileHandler : ProjectileHandler
    {
        public override string NodeType => "MultiTargetProjectile";

        public override UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var prefab = node.GetParamString("prefab", "Prefabs/Skills/Projectile");
            var hitEffect = node.GetParamString("hit_effect", "");
            var endEffect = node.GetParamString("end_effect", "");
            var spawnType = node.GetParamString("spawn_type", "caster");
            var moveType = node.GetParamString("move_type", "Straight");
            var hitModulesRaw = node.GetParamString("hit_modules", "");
            var includePrimaryTarget = node.GetParamBool("include_primary_target", true);

            var skillCard = GetSkillCard(context);

            int targetCount = ResolveTargetCount(node, skillCard);
            LogTool.Log($"[MultiTargetProjectileHandler] 读取弹道数量: {targetCount}");

            float speed = node.GetParamFloat("speed", 10f);
            float lefttime = node.GetParamFloat("max_time", 10f);
            float bulletLength = node.GetParamFloat("bullet_length", 1f);
            float bulletWidth = node.GetParamFloat("bullet_width", 1f);
            float fixedRotation = node.GetParamFloat("fixed_rotation", -1f);
            float fixedStartX = node.GetParamFloat("fixed_start_x", -999f);
            float fixedStartY = node.GetParamFloat("fixed_start_y", -999f);
            bool spawnBurnAreaOnMove = node.GetParamBool("spawn_burn_area_on_move", false);
            string burnAreaTriggerMode = node.GetParamString("burn_area_trigger_mode", "Distance");
            float burnAreaTimeInterval = node.GetParamFloat("burn_area_time_interval", 0.2f);
            float burnAreaDistanceInterval = node.GetParamFloat("burn_area_distance_interval", 1.5f);
            string burnAreaEffect = node.GetParamString("burn_area_effect", "");
            float burnAreaDuration = node.GetParamFloat("burn_area_duration", 2f);
            float burnAreaRadius = node.GetParamFloat("burn_area_radius", 2f);
            float burnAreaTickInterval = node.GetParamFloat("burn_area_tick_interval", 1f);
            float burnAreaDamage = node.GetParamFloat("burn_area_damage", 10f);
            bool burnAreaUseSkillDamage = node.GetParamBool("burn_area_use_skill_damage", false);
            int burnAreaBuffId = node.GetParamInt("burn_area_buff_id", 0);
            float burnAreaBuffStayTime = node.GetParamFloat("burn_area_buff_stay_time", 0f);

            ApplySkillAttributeOverrides(skillCard, node, ref speed, ref lefttime, ref bulletLength, ref bulletWidth, ref fixedRotation);
            if (skillCard != null)
            {
                ApplyBurnAreaAttributeOverrides(skillCard, node,
                    ref burnAreaTimeInterval, ref burnAreaDistanceInterval,
                    ref burnAreaDuration, ref burnAreaRadius, ref burnAreaTickInterval,
                    ref burnAreaDamage, ref burnAreaBuffStayTime);
            }

            if (string.IsNullOrEmpty(prefab) && skillCard?.Config != null)
            {
                prefab = skillCard.Config.ProjectileName;
            }

            var casterPos = context.Caster?.transform.position ?? Vector3.zero;
            SkillTargetResolver.TryResolveProjectileAim(
                context, "Target", "MultiTargetProjectileHandler", out var aimPosition, out _);
            var startPos = ResolveProjectileStartPos(node, spawnType, casterPos, context, aimPosition, fixedStartX, fixedStartY);

            MonsterCard primaryTarget = null;
            if (includePrimaryTarget && context.Target != null)
            {
                primaryTarget = context.Target.GetComponent<MonsterCard>();
                if (primaryTarget == null && context.Target is MonsterCard monsterCard)
                {
                    primaryTarget = monsterCard;
                }
            }

            var enemies = MultiTargetEnemySelector.SelectNearestEnemies(startPos, targetCount, primaryTarget);
            if (enemies.Count == 0)
            {
                LogTool.LogWarning("[MultiTargetProjectileHandler] 未找到可攻击敌人，跳过发射");
                return UniTask.CompletedTask;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var enemyPos = enemy.transform.position + GlobalConst.monsterPosOffset;
                var direction = (enemyPos - startPos).normalized;
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Vector3.up;
                }

                var projectileHitModules = ParseHitModules(node, hitModulesRaw, context);

                SpawnProjectile(node, context, startPos, direction, skillCard, prefab, hitEffect, endEffect,
                    spawnType, moveType, projectileHitModules, speed, lefttime, bulletLength, bulletWidth, fixedRotation,
                    spawnBurnAreaOnMove, burnAreaTriggerMode, burnAreaTimeInterval, burnAreaDistanceInterval,
                    burnAreaEffect, burnAreaDuration, burnAreaRadius, burnAreaTickInterval, burnAreaDamage,
                    burnAreaUseSkillDamage, burnAreaBuffId, burnAreaBuffStayTime,
                    moveType == "Homing" ? enemy.transform : null);
            }

            return UniTask.CompletedTask;
        }

        private static int ResolveTargetCount(SkillNodeConfig node, ISkill skillCard)
        {
            var targetCount = node.GetParamInt("target_count", 3);
            var targetCountAttrName = node.GetParamString("target_count_attr", "");

            if (skillCard != null
                && !string.IsNullOrEmpty(targetCountAttrName)
                && Enum.TryParse<AttributeType>(targetCountAttrName, out var targetCountAttr))
            {
                targetCount = (int)skillCard.Attributes.GetFinal(targetCountAttr);
            }

            return Mathf.Max(0, targetCount);
        }

        private static void ApplySkillAttributeOverrides(
            ISkill skillCard,
            SkillNodeConfig node,
            ref float speed,
            ref float lefttime,
            ref float bulletLength,
            ref float bulletWidth,
            ref float fixedRotation)
        {
            if (skillCard == null)
            {
                return;
            }

            var speedAttrName = node.GetParamString("speed_attr", "");
            var maxTimeAttrName = node.GetParamString("max_time_attr", "");
            var bulletLengthAttrName = node.GetParamString("bullet_length_attr", "");
            var bulletWidthAttrName = node.GetParamString("bullet_width_attr", "");
            var fixedRotationAttrName = node.GetParamString("fixed_rotation_attr", "");

            if (!string.IsNullOrEmpty(speedAttrName) && Enum.TryParse<AttributeType>(speedAttrName, out var speedAttr))
            {
                speed = skillCard.Attributes.GetFinal(speedAttr);
            }

            if (!string.IsNullOrEmpty(maxTimeAttrName) && Enum.TryParse<AttributeType>(maxTimeAttrName, out var maxTimeAttr))
            {
                lefttime = skillCard.Attributes.GetFinal(maxTimeAttr);
            }

            if (!string.IsNullOrEmpty(bulletLengthAttrName) && Enum.TryParse<AttributeType>(bulletLengthAttrName, out var bulletLengthAttr))
            {
                bulletLength = skillCard.Attributes.GetFinal(bulletLengthAttr);
            }

            if (!string.IsNullOrEmpty(bulletWidthAttrName) && Enum.TryParse<AttributeType>(bulletWidthAttrName, out var bulletWidthAttr))
            {
                bulletWidth = skillCard.Attributes.GetFinal(bulletWidthAttr);
            }

            if (!string.IsNullOrEmpty(fixedRotationAttrName) && Enum.TryParse<AttributeType>(fixedRotationAttrName, out var fixedRotationAttr))
            {
                fixedRotation = skillCard.Attributes.GetFinal(fixedRotationAttr);
            }
        }

    }
}
