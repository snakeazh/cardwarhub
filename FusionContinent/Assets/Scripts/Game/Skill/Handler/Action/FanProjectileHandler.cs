using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 扇形投射物节点处理器 - 支持扇形范围内发射多个投射物
    /// </summary>
    public class FanProjectileHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "FanProjectile";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var prefab = node.GetParamString("prefab", "Prefabs/Skills/Projectile");
            var hitEffect = node.GetParamString("hit_effect", "");
            var endEffect = node.GetParamString("end_effect", "");

            var skillCard = GetSkillCard(context);

            // 判断是否为普通攻击
            bool isNormalAttack = false;
            if (context.Caster is SoldierCard soldierCard)
            {
                isNormalAttack = context.SkillId == soldierCard.NormalAttackSkillId;
            }

            // 扇形配置
            float fanAngle = node.GetParamFloat("fan_angle", 90f);
            int projectileCount = node.GetParamInt("projectile_count", 5);
            var fanAngleAttrName = node.GetParamString("fan_angle_attr", "");
            var projectileCountAttrName = node.GetParamString("projectile_count_attr", "");

            if (skillCard != null)
            {
                if (!string.IsNullOrEmpty(fanAngleAttrName) && Enum.TryParse<AttributeType>(fanAngleAttrName, out var fanAngleAttr))
                    fanAngle = skillCard.Attributes.GetFinal(fanAngleAttr);
                if (!string.IsNullOrEmpty(projectileCountAttrName) && Enum.TryParse<AttributeType>(projectileCountAttrName, out var projectileCountAttr))
                    projectileCount = (int)skillCard.Attributes.GetFinal(projectileCountAttr);
            }

            // 普通攻击时，额外投射物数量叠加
            if (isNormalAttack && context.Caster is SoldierCard normalAttackSoldier)
            {
                projectileCount += (int)normalAttackSoldier.BaseAttributes.GetFinal(AttributeType.NormalAttackNum);

                // 普通攻击时，额外伤害变化
                float extraDamage = normalAttackSoldier.BaseAttributes.GetFinal(AttributeType.NormalAttackDamage);
                if (extraDamage != 0f)
                {
                    context.SetExtraData("ExtraNormalAttackDamage", extraDamage);
                }
            }

            if (projectileCount == 0) projectileCount = 1;

            var aimTarget = node.GetParamString("aim_target", "Target");
            if (!SkillTargetResolver.TryResolveProjectileAim(
                    context, aimTarget, "FanProjectileHandler", out var aimPosition, out var homingTransform))
            {
                return;
            }

            // 投射物组件配置
            float speed = node.GetParamFloat("speed", 10f);
            float maxTime = node.GetParamFloat("max_time", 10f);
            float bulletLength = node.GetParamFloat("bullet_length", 1f);
            float fixedRotation = node.GetParamFloat("fixed_rotation", -1f);
            var speedAttrName = node.GetParamString("speed_attr", "");
            var maxTimeAttrName = node.GetParamString("max_time_attr", "");
            var bulletLengthAttrName = node.GetParamString("bullet_length_attr", "");
            var fixedRotationAttrName = node.GetParamString("fixed_rotation_attr", "");

            if (skillCard != null)
            {
                if (!string.IsNullOrEmpty(speedAttrName) && Enum.TryParse<AttributeType>(speedAttrName, out var speedAttr))
                    speed = skillCard.Attributes.GetFinal(speedAttr);
                if (!string.IsNullOrEmpty(maxTimeAttrName) && Enum.TryParse<AttributeType>(maxTimeAttrName, out var maxTimeAttr))
                    maxTime = skillCard.Attributes.GetFinal(maxTimeAttr);
                if (!string.IsNullOrEmpty(bulletLengthAttrName) && Enum.TryParse<AttributeType>(bulletLengthAttrName, out var bulletLengthAttr))
                    bulletLength = skillCard.Attributes.GetFinal(bulletLengthAttr);
                if (!string.IsNullOrEmpty(fixedRotationAttrName) && Enum.TryParse<AttributeType>(fixedRotationAttrName, out var fixedRotationAttr))
                    fixedRotation = skillCard.Attributes.GetFinal(fixedRotationAttr);
            }

            var moveType = node.GetParamString("move_type", "Straight");
            var hitModulesRaw = node.GetParamString("hit_modules", "Penetrate|3:0");
            var hitModules = ParseHitModules(node, hitModulesRaw, context);

            // 多轮发射配置
            int totalRounds = node.GetParamInt("total_rounds", 1);
            float roundInterval = node.GetParamFloat("round_interval", 0.1f);
            var totalRoundsAttrName = node.GetParamString("total_rounds_attr", "");
            var roundIntervalAttrName = node.GetParamString("round_interval_attr", "");

            if (skillCard != null)
            {
                if (!string.IsNullOrEmpty(totalRoundsAttrName) && Enum.TryParse<AttributeType>(totalRoundsAttrName, out var totalRoundsAttr))
                    totalRounds = (int)skillCard.Attributes.GetFinal(totalRoundsAttr);
                if (!string.IsNullOrEmpty(roundIntervalAttrName) && Enum.TryParse<AttributeType>(roundIntervalAttrName, out var roundIntervalAttr))
                    roundInterval = skillCard.Attributes.GetFinal(roundIntervalAttr);
            }

            // context.TargetPosition 为拖拽方向时，aim_target=Selected 会改为指向已选目标
            Vector3 basePos = context.Caster?.transform.position ?? Vector3.zero;
            Vector3 direction = aimPosition - basePos;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.up;
            }
            else
            {
                direction = direction.normalized;
            }

            // 计算缩放
            float sizeCorrection = context.Caster != null
                ? context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SoldierBulletSizeCorrection)
                : 1f;
            float globalScale = 1f;
            if (context.Caster != null)
            {
                float skillRange = context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SkillRange);
                globalScale = (100f + skillRange) / 100f * sizeCorrection;
            }

            var baseScale = node.GetParamFloat("scale", 100f) / 100f * globalScale;

            // 计算扇形内每个投射物的角度偏移
            float halfFanAngle = fanAngle / 2f;
            float baseAngleRad = Mathf.Atan2(direction.y, direction.x);

            // 多轮发射
            for (int round = 0; round < totalRounds; round++)
            {
                if (round > 0)
                {
                    await UniTask.Delay((int)(roundInterval * 1000));
                }

                for (int i = 0; i < projectileCount; i++)
                {
                    float angleOffset = GetFanAngleOffset(i, projectileCount, halfFanAngle);
                    float angleRad = baseAngleRad + angleOffset * Mathf.Deg2Rad;
                    Vector3 projectileDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);

                    IProjectileMove move = CreateMove(moveType, projectileDirection, speed, node, context, homingTransform);
                    CreateProjectile(prefab, basePos, baseScale,
                        speed, maxTime, bulletLength, fixedRotation, endEffect,
                        hitModules, hitEffect, skillCard, context, projectileDirection, move);
                }
            }
        }

        private static float GetFanAngleOffset(int index, int projectileCount, float halfFanAngle)
        {
            if (projectileCount <= 1 || index <= 0)
            {
                return 0f;
            }

            int sideSlotCount = Mathf.Max(1, Mathf.CeilToInt((projectileCount - 1) / 2f));
            float angleStep = halfFanAngle / sideSlotCount;
            int ringIndex = (index + 1) / 2; // 1,1,2,2,3,3...
            bool isRightSide = index % 2 == 1; // 第2发在右侧，然后左右交替
            float sign = isRightSide ? 1f : -1f;

            return ringIndex * angleStep * sign;
        }

        private List<IProjectileHitModule> ParseHitModules(SkillNodeConfig node, string raw, SkillContext context)
        {
            var modules = new List<IProjectileHitModule>();
            if (string.IsNullOrEmpty(raw))
            {
                modules.Add(new PenetrateHitModule { MaxCount = 3, curCount = 0, _hitTargetDuration = 0f });
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

        private (int intParam1, float floatParam1, float floatParam2, bool boolParam1, int intParam2,
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

        private void ApplySkillAttributeOverrides(ISkill skillCard, ref int intParam1, ref float floatParam1,
            ref float floatParam2, ref int intParam2, ref float floatParam3, string intParam3Attr,
            ref int intParam4, ref float floatParam4, ref float floatParam5,
            string intParam1Attr, string floatParam1Attr, string floatParam2Attr, string intParam2Attr,
            string floatParam3Attr, string intParam4Attr, string floatParam4Attr, string floatParam5Attr)
        {
            if (skillCard == null) return;

            if (!string.IsNullOrEmpty(intParam1Attr) && Enum.TryParse<AttributeType>(intParam1Attr, out var ipAttr))
                intParam1 = (int)skillCard.Attributes.GetFinal(ipAttr);
            if (!string.IsNullOrEmpty(floatParam1Attr) && Enum.TryParse<AttributeType>(floatParam1Attr, out var fp1Attr))
                floatParam1 = skillCard.Attributes.GetFinal(fp1Attr);
            if (!string.IsNullOrEmpty(floatParam2Attr) && Enum.TryParse<AttributeType>(floatParam2Attr, out var fp2Attr))
                floatParam2 = skillCard.Attributes.GetFinal(fp2Attr);
            if (!string.IsNullOrEmpty(intParam2Attr) && Enum.TryParse<AttributeType>(intParam2Attr, out var ip2Attr))
                intParam2 = (int)skillCard.Attributes.GetFinal(ip2Attr);
            if (!string.IsNullOrEmpty(floatParam3Attr) && Enum.TryParse<AttributeType>(floatParam3Attr, out var fp3Attr))
                floatParam3 = skillCard.Attributes.GetFinal(fp3Attr);
            if (!string.IsNullOrEmpty(intParam4Attr) && Enum.TryParse<AttributeType>(intParam4Attr, out var ip4Attr))
                intParam4 = (int)skillCard.Attributes.GetFinal(ip4Attr);
            if (!string.IsNullOrEmpty(floatParam4Attr) && Enum.TryParse<AttributeType>(floatParam4Attr, out var fp4Attr))
                floatParam4 = skillCard.Attributes.GetFinal(fp4Attr);
            if (!string.IsNullOrEmpty(floatParam5Attr) && Enum.TryParse<AttributeType>(floatParam5Attr, out var fp5Attr))
                floatParam5 = skillCard.Attributes.GetFinal(fp5Attr);
        }

        private IProjectileMove CreateMove(string moveType, Vector3 direction, float speed, SkillNodeConfig node, SkillContext context, Transform homingTarget = null)
        {
            return moveType switch
            {
                "Straight" => new StraightMove(direction, speed),
                "Bounce" => new BounceMove(
                    direction,
                    speed,
                    node.GetParamInt("bounce_count", 3),
                    node.GetParamBool("bounce_reset_hit", true)),
                "LaserSweep" => new LaserSweepMove(speed, node.GetParamFloat("sweep_angle", 50f), node.GetParamFloat("sweep_duration", 5f)),
                "Homing" => new HomingMove(
                    node.GetParamFloat("search_speed", 3f), speed,
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
                "Parabola" or "Curve" or "Spiral" => new StraightMove(direction, speed),
                _ => new StraightMove(direction, speed)
            };
        }

        private IProjectileHitModule CreateHitModule(string moduleType, int intParam, float floatParam1,
            float floatParam2, bool boolParam, SkillContext skillContext, int intParam2 = 0,
            float floatParam3 = 0f, int intParam3 = 0, string percentBaseAttrName = null,
            float buffHitProbability = 1f, int intParam4 = 0, float floatParam4 = 1f,
            float floatParam5 = 0f, bool boolParam2 = false, string stringParam1 = null,
            string stringParam2 = null, string stringParam3 = null, string stringParam4 = null,
            int intParam5 = 0, string stringParam5 = null)
        {
            if (skillContext?.Caster is not SoldierCard)
            {
                LogTool.LogWarning("[FanProjectileHandler] 攻击方不是 SoldierCard，无法造成伤害");
                return new PenetrateHitModule { MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1 };
            }

            return moduleType switch
            {
                "Penetrate" => new PenetrateHitModule { MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1 },
                "Explode" => new ExplodeHitModule { radius = floatParam1 > 0 ? floatParam1 : 5, loadingtime = floatParam2 },
                "Rect" => new RectHitModule { MaxCount = intParam >= 0 ? intParam : 3, curCount = 0, rectWidth = floatParam1 > 0 ? floatParam1 : 1f, rectLength = floatParam2 > 0 ? floatParam2 : 1f, _hitTargetDuration = floatParam3 },
                "AOE" => new AoeHitModule { radius = floatParam1 > 0 ? floatParam1 : 3f, checkInterval = floatParam2, singleTrigger = boolParam, _hitTargetDuration = floatParam2 },
                "Split" => new SplitHitModule { SplitsNum = intParam, SplitsDamage = floatParam1, SplitsDamageAgain = floatParam2, SplitsAgainCount = intParam2 },
                "AttachBuff" => CreateAttachBuffHitModule(intParam5, intParam, stringParam4, buffHitProbability, skillContext),
                "Catapult" => new CatapultHitModule
                {
                    MaxRicochetCount = intParam > 0 ? intParam : 0,
                    CurRicochetCount = 0,
                    SearchRadius = floatParam1 > 0 ? floatParam1 : 0f
                },
                _ => new PenetrateHitModule { MaxCount = intParam > 0 ? intParam : 3, curCount = 0, _hitTargetDuration = floatParam1 }
            };
        }

        private static AttachBuffHitModule CreateAttachBuffHitModule(
            int intParam5,
            int intParam,
            string stringParam4,
            float buffHitProbability,
            SkillContext skillContext)
        {
            int attachBuffId = intParam5 > 0 ? intParam5 : (intParam > 0 ? intParam : 0);
            BuffApplier.RegisterHitExecuteBuff(skillContext, attachBuffId);
            return new AttachBuffHitModule
            {
                BuffId = attachBuffId,
                Target = string.IsNullOrEmpty(stringParam4) ? "Enemy" : stringParam4,
                Probability = buffHitProbability
            };
        }

        private void CreateProjectile(string prefab, Vector3 position, float scale,
            float speed, float maxTime, float bulletLength, float fixedRotation,
            string endEffect, List<IProjectileHitModule> hitModules, string hitEffect,
            ISkill skillCard, SkillContext context, Vector3 projectileDirection, IProjectileMove move)
        {
            var effect = EffectManager.Instance.PlayProjectileEffect(prefab, position, maxTime,
                scale: new Vector3(scale, scale, scale),
                parent: GameLand.Default.Bullet);

            if (effect == null) return;

            if (effect.GetComponent<Projectile_new>() == null)
            {
                effect.AddComponent<Projectile_new>();
            }
            Projectile_new projectile = effect.GetComponent<Projectile_new>();

            projectile.Config.speed = speed;
            projectile.Config.checkRadius = 0.1f;
            projectile.Config.lifeTime = maxTime;
            projectile.Config.hitEffect = hitEffect;
            projectile.Config.bulletLength = bulletLength;
            projectile.Config.fixedRotation = fixedRotation;
            projectile.Config.endEffect = endEffect;

            projectile.Init(move, hitModules, context, position, projectileDirection);
            ProjectileManager.Instance.Add(projectile);
        }

        private static ISkill GetSkillCard(SkillContext context)
        {
            if (context?.Caster is SoldierCard soldierCard)
            {
                return soldierCard.GetSkillById(context.SkillId);
            }
            return null;
        }
    }
}
