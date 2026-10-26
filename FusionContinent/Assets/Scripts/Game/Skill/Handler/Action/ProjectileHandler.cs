using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;
using Unity.Profiling;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 投射物节点处理器
    /// </summary>
    public class ProjectileHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "Projectile";

        public override UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var prefab = node.GetParamString("prefab", "Prefabs/Skills/Projectile");
            var hitEffect = node.GetParamString("hit_effect", "");
            var endEffect = node.GetParamString("end_effect", "");
            var spawnType = node.GetParamString("spawn_type", "caster");
            var aimTarget = node.GetParamString("aim_target", "Target");
            var projectileDirection = node.GetParamString("projectile_direction", "Target");
            var moveType = node.GetParamString("move_type", "Straight");
            var hitModulesRaw = node.GetParamString("hit_modules", "");
            var hitModules = ParseHitModules(node, hitModulesRaw, context);

            // 获取技能卡片
            var skillCard = GetSkillCard(context);

            // 投射物配置 - 支持从技能属性读取
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

            var speedAttrName = node.GetParamString("speed_attr", "");
            var maxTimeAttrName = node.GetParamString("max_time_attr", "");
            var bulletLengthAttrName = node.GetParamString("bullet_length_attr", "");
            var bulletWidthAttrName = node.GetParamString("bullet_width_attr", "");
            var fixedRotationAttrName = node.GetParamString("fixed_rotation_attr", "");

            if (skillCard != null)
            {
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

                ApplyBurnAreaAttributeOverrides(skillCard, node,
                    ref burnAreaTimeInterval, ref burnAreaDistanceInterval,
                    ref burnAreaDuration, ref burnAreaRadius, ref burnAreaTickInterval,
                    ref burnAreaDamage, ref burnAreaBuffStayTime);
            }

            var casterPos = context.Caster?.transform.position ?? Vector3.zero;
            if (!SkillTargetResolver.TryResolveProjectileAim(
                    context, aimTarget, "ProjectileHandler", out var targetPos, out var homingTransform))
            {
                return UniTask.CompletedTask;
            }

            var startPos = ResolveProjectileStartPos(node, spawnType, casterPos, context, targetPos, fixedStartX, fixedStartY);

            var direction = ResolveProjectileDirection(projectileDirection, startPos, targetPos);

            SpawnProjectile(node, context, startPos, direction, skillCard, prefab, hitEffect, endEffect,
                spawnType, moveType, hitModules, speed, lefttime, bulletLength, bulletWidth, fixedRotation,
                spawnBurnAreaOnMove, burnAreaTriggerMode, burnAreaTimeInterval, burnAreaDistanceInterval,
                burnAreaEffect, burnAreaDuration, burnAreaRadius, burnAreaTickInterval, burnAreaDamage,
                burnAreaUseSkillDamage, burnAreaBuffId, burnAreaBuffStayTime, homingTransform);

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 根据 spawn_type 解析生成点，并叠加 spawn_offset_x/y（相对基准点的世界 XY 偏移）。
        /// </summary>
        protected static Vector3 ResolveProjectileStartPos(
            SkillNodeConfig node,
            string spawnType,
            Vector3 casterPos,
            SkillContext context,
            Vector3 aimPosition,
            float fixedStartX,
            float fixedStartY)
        {
            Vector3 startPos;
            switch (spawnType)
            {
                case "target":
                    startPos = aimPosition;
                    break;
                case "direction":
                    startPos = context.TargetPosition;
                    startPos.z = aimPosition.z;
                    break;
                case "fixed":
                    startPos = new Vector3(
                        fixedStartX > -900f ? fixedStartX : aimPosition.x,
                        fixedStartY > -900f ? fixedStartY : aimPosition.y,
                        aimPosition.z);
                    break;
                case "caster":
                default:
                    startPos = (fixedStartX > -900f || fixedStartY > -900f)
                        ? new Vector3(
                            fixedStartX > -900f ? fixedStartX : casterPos.x,
                            fixedStartY > -900f ? fixedStartY : casterPos.y,
                            casterPos.z)
                        : casterPos;
                    break;
            }

            float spawnOffsetX = node.GetParamFloat("spawn_offset_x", 0f);
            float spawnOffsetY = node.GetParamFloat("spawn_offset_y", 0f);
            if (spawnOffsetX != 0f || spawnOffsetY != 0f)
            {
                startPos += new Vector3(spawnOffsetX, spawnOffsetY, 0f);
            }

            return startPos;
        }

        protected static Vector3 ResolveProjectileDirection(string projectileDirection, Vector3 startPos, Vector3 targetPos)
        {
            switch (projectileDirection)
            {
                case "Up":
                    return Vector3.up;
                case "Down":
                    return Vector3.down;
                case "Left":
                    return Vector3.left;
                case "Right":
                    return Vector3.right;
                case "Target":
                default:
                    var direction = targetPos - startPos;
                    if (direction.sqrMagnitude <= Mathf.Epsilon)
                    {
                        return Vector3.up;
                    }
                    return direction.normalized;
            }
        }

        protected void SpawnProjectile(
            SkillNodeConfig node,
            SkillContext context,
            Vector3 startPos,
            Vector3 direction,
            ISkill skillCard,
            string prefab,
            string hitEffect,
            string endEffect,
            string spawnType,
            string moveType,
            List<IProjectileHitModule> hitModules,
            float speed,
            float lefttime,
            float bulletLength,
            float bulletWidth,
            float fixedRotation,
            bool spawnBurnAreaOnMove,
            string burnAreaTriggerMode,
            float burnAreaTimeInterval,
            float burnAreaDistanceInterval,
            string burnAreaEffect,
            float burnAreaDuration,
            float burnAreaRadius,
            float burnAreaTickInterval,
            float burnAreaDamage,
            bool burnAreaUseSkillDamage,
            int burnAreaBuffId,
            float burnAreaBuffStayTime,
            Transform homingTarget = null)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BulletSpawn))
            {
                IProjectileMove move = CreateMove(moveType, direction, speed, node, context, homingTarget);

            float sizeCorrection = context.Caster != null
                ? context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SoldierBulletSizeCorrection)
                : 1f;

            GameObject gameObject = EffectManager.Instance.PlayProjectileEffect(prefab, startPos, lefttime,
                scale: new Vector3(bulletWidth * sizeCorrection, bulletLength * sizeCorrection, 1f),
                parent: GameLand.Default.Bullet);

            if (gameObject.GetComponent<Projectile_new>() == null)
            {
                gameObject.AddComponent<Projectile_new>();
            }

            Projectile_new projectile = gameObject.GetComponent<Projectile_new>();
            projectile.Config.speed = speed;
            projectile.Config.checkRadius = 0.1f;
            projectile.Config.lifeTime = lefttime;
            projectile.Config.hitEffect = hitEffect;
            projectile.Config.bulletLength = bulletLength;
            projectile.Config.bulletWidth = bulletWidth;
            projectile.Config.fixedRotation = fixedRotation;
            projectile.Config.endEffect = endEffect;
            projectile.EnableBurnAreaOnMove = spawnBurnAreaOnMove;
            projectile.BurnAreaTriggerMode = burnAreaTriggerMode;
            projectile.BurnAreaTimeInterval = burnAreaTimeInterval;
            projectile.BurnAreaDistanceInterval = burnAreaDistanceInterval;
            projectile.BurnAreaEffect = burnAreaEffect;
            projectile.BurnAreaDuration = burnAreaDuration;
            projectile.BurnAreaRadius = burnAreaRadius;
            projectile.BurnAreaTickInterval = burnAreaTickInterval;
            projectile.BurnAreaDamage = burnAreaDamage;
            projectile.BurnAreaUseSkillDamage = burnAreaUseSkillDamage;
            projectile.BurnAreaBuffId = burnAreaBuffId;
            projectile.BurnAreaBuffStayTime = burnAreaBuffStayTime;

            projectile.Init(move, hitModules, context, startPos, direction);
            if (moveType == "MoveToPosition")
            {
                projectile.DisableHitDetection = true;
            }

            ProjectileManager.Instance.Add(projectile);
            }
        }

        protected List<IProjectileHitModule> ParseHitModules(SkillNodeConfig node, string raw, SkillContext context)
        {
            var modules = new List<IProjectileHitModule>();
            if (string.Equals(raw, "None", StringComparison.OrdinalIgnoreCase))
            {
                return modules;
            }

            if (string.IsNullOrEmpty(raw))
            {
                modules.Add(new PenetrateHitModule
                {
                    MaxCount = 3,
                    curCount = 0,
                    _hitTargetDuration = 0f
                });
                return modules;
            }

            var skillCard = GetSkillCard(context);
            var buffHitProbability = BuffApplier.ResolveBuffHitProbability(node, skillCard);
            string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                var moduleType = parts[0].Trim();
                var (intParam1, floatParam1, floatParam2, boolParam1, intParam2, floatParam3,
                    intParam1Attr, floatParam1Attr, floatParam2Attr, intParam2Attr, floatParam3Attr,
                    intParam3, intParam3Attr, intParam4, intParam4Attr,
                    floatParam4, floatParam4Attr, floatParam5, floatParam5Attr, boolParam2,
                    stringParam1, stringParam2, stringParam3, stringParam4, intParam5, stringParam5) =
                    ParseModuleParams(parts);

                // 从技能属性读取（覆盖固定值）
                BuffApplier.ApplySkillAttributeOverrides(skillCard,
                    ref intParam1, ref floatParam1, ref floatParam2,
                    ref intParam2, ref floatParam3, intParam3Attr,
                    ref intParam4, ref floatParam4, ref floatParam5,
                    intParam1Attr, floatParam1Attr, floatParam2Attr, intParam2Attr, floatParam3Attr, intParam4Attr,
                    floatParam4Attr, floatParam5Attr);

                var module = CreateHitModule(moduleType, intParam1, floatParam1, floatParam2, boolParam1, context,
                    intParam2, floatParam3, intParam3, intParam3Attr, buffHitProbability, intParam4, floatParam4,
                    floatParam5, boolParam2, stringParam1, stringParam2, stringParam3, stringParam4, intParam5, stringParam5);
                if (module != null)
                    modules.Add(module);
            }
            return modules;
        }

        protected (int intParam1, float floatParam1, float floatParam2, bool boolParam1, int intParam2,
            float floatParam3, string intParam1Attr, string floatParam1Attr, string floatParam2Attr,
            string intParam2Attr, string floatParam3Attr, int intParam3, string intParam3Attr, int intParam4,
            string intParam4Attr, float floatParam4, string floatParam4Attr, float floatParam5,
            string floatParam5Attr, bool boolParam2, string stringParam1, string stringParam2,
            string stringParam3, string stringParam4, int intParam5, string stringParam5) ParseModuleParams(string[] parts)
        {
            int intParam1 = 0, intParam2 = 0, intParam3 = 0, intParam4 = 0, intParam5 = 0;
            float floatParam1 = 0f, floatParam2 = 0f, floatParam3 = 0f, floatParam4 = 1f, floatParam5 = 0f;
            bool boolParam1 = false, boolParam2 = false;
            string intParam1Attr = "", floatParam1Attr = "", floatParam2Attr = "", intParam2Attr = "",
                floatParam3Attr = "", intParam3Attr = "", intParam4Attr = "", floatParam4Attr = "",
                floatParam5Attr = "", stringParam1 = "", stringParam2 = "", stringParam3 = "",
                stringParam4 = "Enemy", stringParam5 = "";

            if (parts.Length > 1)
            {
                var p = parts[1].Split(':');
                if (p.Length > 0) int.TryParse(p[0], out intParam1);
                if (p.Length > 1) float.TryParse(p[1], out floatParam1);
                if (p.Length > 2) float.TryParse(p[2], out floatParam2);
                if (p.Length > 3) boolParam1 = p[3] == "1" || p[3].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (p.Length > 4) int.TryParse(p[4], out intParam2);
                if (p.Length > 5) float.TryParse(p[5], out floatParam3);
                if (p.Length > 6) intParam1Attr = BuffApplier.NormalizeHitModuleAttrName(p[6]);
                if (p.Length > 7) floatParam1Attr = BuffApplier.NormalizeHitModuleAttrName(p[7]);
                if (p.Length > 8) floatParam2Attr = BuffApplier.NormalizeHitModuleAttrName(p[8]);
                if (p.Length > 9) intParam2Attr = BuffApplier.NormalizeHitModuleAttrName(p[9]);
                if (p.Length > 10) floatParam3Attr = BuffApplier.NormalizeHitModuleAttrName(p[10]);
                if (p.Length > 11) int.TryParse(p[11], out intParam3);
                if (p.Length > 12) intParam3Attr = BuffApplier.NormalizeHitModuleAttrName(p[12]);
                if (p.Length > 13) int.TryParse(p[13], out intParam4);
                if (p.Length > 14) intParam4Attr = BuffApplier.NormalizeHitModuleAttrName(p[14]);
                if (p.Length > 15) float.TryParse(p[15], out floatParam4);
                if (p.Length > 16) floatParam4Attr = BuffApplier.NormalizeHitModuleAttrName(p[16]);
                if (p.Length > 17) float.TryParse(p[17], out floatParam5);
                if (p.Length > 18) floatParam5Attr = BuffApplier.NormalizeHitModuleAttrName(p[18]);
                if (p.Length > 19) boolParam2 = p[19] == "1" || p[19].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (p.Length > 20) stringParam1 = p[20];
                if (p.Length > 21) stringParam2 = p[21];
                if (p.Length > 22) stringParam3 = p[22];
                if (p.Length > 23) stringParam4 = p[23];
                if (p.Length > 24) int.TryParse(p[24], out intParam5);
                if (p.Length > 25) stringParam5 = p[25];
            }

            return (intParam1, floatParam1, floatParam2, boolParam1, intParam2, floatParam3,
                intParam1Attr, floatParam1Attr, floatParam2Attr, intParam2Attr, floatParam3Attr,
                intParam3, intParam3Attr, intParam4, intParam4Attr,
                floatParam4, floatParam4Attr, floatParam5, floatParam5Attr, boolParam2,
                stringParam1, stringParam2, stringParam3, stringParam4, intParam5, stringParam5);
        }

        protected IProjectileMove CreateMove(string moveType, Vector3 direction, float speed, SkillNodeConfig node, SkillContext context, Transform homingTarget = null)
        {
            return moveType switch
            {
                "Straight" => new StraightMove(direction, speed),
                "MoveToPosition" => new MoveToPositionMove(context.TargetPosition, speed, node.GetParamFloat("arrive_distance", 0.05f)),
                "Bounce" => new BounceMove(
                    direction,
                    speed,
                    node.GetParamInt("bounce_count"),
                    node.GetParamBool("bounce_reset_hit", true)),
                "LaserSweep" => new LaserSweepMove(speed, node.GetParamFloat("sweep_angle", 50f), node.GetParamFloat("sweep_duration", 5f)),
                "Homing" => new HomingMove(
                    node.GetParamFloat("search_speed", 3f),
                    speed,
                    node.GetParamFloat("search_duration", 5f),
                    node.GetParamFloat("homing_delay", 0f),
                    node.GetParamFloat("homing_rot_speed", 180f),
                    node.GetParamFloat("homing_strength", 0.5f),
                    node.GetParamFloat("wander_move_duration", 2f),
                    node.GetParamFloat("wander_wait_duration", 1f),
                    node.GetParamFloat("homing_move_duration", 2f),
                    node.GetParamFloat("homing_wait_duration", 1f),
                    node.GetParamFloat("wander_range", 5f),
                    homingTarget
                ),
                "Parabola" => new StraightMove(direction, speed),
                "Curve" => new StraightMove(direction, speed),
                "Spiral" => new StraightMove(direction, speed),
                _ => new StraightMove(direction, speed)
            };
        }

        protected IProjectileHitModule CreateHitModule(string moduleType, int intParam, float floatParam1,
            float floatParam2, bool boolParam, SkillContext skillContext, int intParam2 = 0,
            float floatParam3 = 0f, int intParam3 = 0, string percentBaseAttrName = null,
            float buffHitProbability = 1f, int intParam4 = 0, float floatParam4 = 1f,
            float floatParam5 = 0f, bool boolParam2 = false, string stringParam1 = null,
            string stringParam2 = null, string stringParam3 = null, string stringParam4 = null,
            int intParam5 = 0, string stringParam5 = null)
        {
            if (skillContext?.Caster is not SoldierCard caster)
            {
                LogTool.LogWarning("[ProjectileHandler] 攻击方不是 SoldierCard，无法造成伤害");
                return new PenetrateHitModule
                {
                    MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1
                };
            }

            switch (moduleType)
            {
                case "Penetrate":
                    return new PenetrateHitModule
                    {
                        MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1
                    };
                case "Explode":
                    return new ExplodeHitModule() { radius = floatParam1 > 0 ? floatParam1 : 5, loadingtime = floatParam2, };
                case "Rect":
                    return new RectHitModule
                    {
                        MaxCount = intParam >= 0 ? intParam : 3,
                        curCount = 0,
                        rectWidth = floatParam1 > 0 ? floatParam1 : 1f,
                        rectLength = floatParam2 > 0 ? floatParam2 : 1f,
                        _hitTargetDuration = floatParam3
                    };
                case "AOE":
                    return new AoeHitModule
                    {
                        radius = floatParam1 > 0 ? floatParam1 : 3f,
                        checkInterval = 0f,
                        singleTrigger = boolParam,
                        _hitTargetDuration = floatParam2
                    };
                case "Split":
                    return new SplitHitModule
                    {
                        SplitsNum = intParam,
                        SplitsDamage = floatParam1,
                        SplitsDamageAgain = floatParam2,
                        SplitsAgainCount = intParam2
                    };
                case "AttachBuff":
                    int attachBuffId = intParam5 > 0 ? intParam5 : (intParam > 0 ? intParam : 0);
                    BuffApplier.RegisterHitExecuteBuff(skillContext, attachBuffId);
                    return new AttachBuffHitModule
                    {
                        BuffId = attachBuffId,
                        Target = string.IsNullOrEmpty(stringParam4) ? "Enemy" : stringParam4,
                        Probability = buffHitProbability
                    };
                case "Catapult":
                    return new CatapultHitModule
                    {
                        MaxRicochetCount = intParam > 0 ? intParam : 0,
                        CurRicochetCount = 0,
                        SearchRadius = floatParam1 > 0 ? floatParam1 : 0f
                    };
                default:
                    return new PenetrateHitModule
                    {
                        MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1
                    };
            }
        }

        protected static void ApplyBurnAreaAttributeOverrides(
            ISkill skillCard,
            SkillNodeConfig node,
            ref float burnAreaTimeInterval,
            ref float burnAreaDistanceInterval,
            ref float burnAreaDuration,
            ref float burnAreaRadius,
            ref float burnAreaTickInterval,
            ref float burnAreaDamage,
            ref float burnAreaBuffStayTime)
        {
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_time_interval_attr", ref burnAreaTimeInterval);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_distance_interval_attr", ref burnAreaDistanceInterval);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_duration_attr", ref burnAreaDuration);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_radius_attr", ref burnAreaRadius);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_tick_interval_attr", ref burnAreaTickInterval);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_damage_attr", ref burnAreaDamage);
            ApplyFloatFromSkillAttr(skillCard, node, "burn_area_buff_stay_time_attr", ref burnAreaBuffStayTime);
        }

        private static void ApplyFloatFromSkillAttr(ISkill skillCard, SkillNodeConfig node, string attrParamName, ref float value)
        {
            var attrName = node.GetParamString(attrParamName, "");
            if (!string.IsNullOrEmpty(attrName) && Enum.TryParse<AttributeType>(attrName, out var attr))
            {
                value = skillCard.Attributes.GetFinal(attr);
            }
        }

        /// <summary>
        /// 获取技能卡片（统一方法）
        /// </summary>
        protected static ISkill GetSkillCard(SkillContext context)
        {
            if (context?.Caster == null) return null;
            if (context.Caster is SoldierCard soldierCard)
            {
                return soldierCard.GetSkillById(context.SkillId);
            }
            return null;
        }
    }
}
