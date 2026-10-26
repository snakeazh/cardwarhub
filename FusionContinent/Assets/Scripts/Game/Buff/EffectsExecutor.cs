using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using Manager;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 效果执行器
    /// 根据 EffectsConfig 的 EffectsType 执行不同的效果操作
    /// </summary>
    public static class EffectsExecutor
    {
        /// <summary>
        /// 执行效果列表
        /// </summary>
        public static void ExecuteEffects(
            List<int> effectIds,
            SkillContext context,
            object owner,
            Vector3 direction,
            object target = null)
        {
            if (effectIds == null || effectIds.Count == 0)
                return;

            var tables = DataConfigManager.Tables;
            if (tables == null)
            {
                LogTool.E("[EffectsExecutor] DataConfigManager.Tables is null");
                return;
            }

            foreach (var effectId in effectIds)
            {
                var effectConfig = tables.TbEffectsConfig.GetOrDefault(effectId);
                if (effectConfig == null)
                {
                    LogTool.W($"[EffectsExecutor] EffectsConfig not found: id={effectId}");
                    continue;
                }

                ExecuteEffect(effectConfig, context, owner, direction,target);
            }
        }

        /// <summary>
        /// 执行单个效果
        /// </summary>
        public static void ExecuteEffect(
            EffectsConfig effectConfig,
            SkillContext context,
            object owner,
            Vector3 direction,
            object target = null)
        {
            if (effectConfig == null)
            {
                LogTool.E("[EffectsExecutor] ExecuteEffect: effectConfig is null");
                return;
            }

            switch (effectConfig.Type)
            {
                case EffectsType.TakeSkill:
                    ExecuteTakeSkill(effectConfig, context, owner,direction, target);
                    break;

                case EffectsType.TakeBuff:
                    ExecuteTakeBuff(effectConfig.EffectsId, context, owner, target);
                    break;
                case EffectsType.TakeSpecial:
                    ExecuteAttribute(effectConfig,context, owner,direction, target);
                    break;
                case EffectsType.DelBuff:
                    ExecuteRemoveBuff(effectConfig, context, owner, target);
                    break;
                // case EffectsType.MaxHPDamage:
                //     ExecuteMaxHPDamage(effectConfig, context, owner, target);
                //     break;

                default:
                    LogTool.W($"[EffectsExecutor] Unsupported EffectsType: {effectConfig.Type}");
                    break;
            }
        }

        /// <summary>
        /// 释放技能
        /// </summary>
        private static void ExecuteTakeSkill(
            EffectsConfig effectConfig,
            SkillContext context,
            object owner,
            Vector3 direction,
            object target)
        {
            var skillId = effectConfig.EffectsId;
            var casterComponent = owner as Component;

            if (casterComponent != null && SkillManager.instance != null)
            {
                var targetPoint = target is Component targetComponent
                    ? targetComponent.transform.position
                    : casterComponent.transform.position + casterComponent.transform.forward * 5f;

                (casterComponent as SoldierCard).UseSkill(skillId, targetPoint).Forget();
                // SkillManager.instance.CastSkill(skillId, casterComponent, targetPoint).Forget();
                LogTool.L($"[EffectsExecutor] TakeSkill: Released skill {skillId}");
            }
            else
            {
                LogTool.W($"[EffectsExecutor] TakeSkill: Invalid caster for skill {skillId}");
            }
        }

        /// <summary>
        /// 属性的特殊处理
        /// 根据 Attributes 中的配置执行特殊操作（如减少冷却时间、造成最大生命值伤害）
        /// </summary>
        private static void ExecuteAttribute(
            EffectsConfig effectConfig,
            SkillContext context,
            object owner,
            Vector3 direction,
            object target)
        {
            if (effectConfig.Attributes == null || effectConfig.Attributes.Count == 0)
            {
                LogTool.W("[EffectsExecutor] ExecuteAttribute: No attributes defined");
                return;
            }

            var soldierCard = owner as SoldierCard;
            if (soldierCard == null)
            {
                LogTool.W("[EffectsExecutor] ExecuteAttribute: Owner is not SoldierCard");
                return;
            }

            foreach (var attr in effectConfig.Attributes)
            {
                // 处理 MonsterContinuousDamage3CauseTrueDamage：对目标最大生命值造成百分比伤害
                if (attr.Type == AttributeType.MonsterContinuousDamage3CauseTrueDamage)
                {
                    ExecuteMonsterContinuousDamage(soldierCard, target, attr.Value);
                    continue;
                }

                // 处理 DoubleAttack：触发时使用普通攻击（延迟0.5秒）
                if (attr.Type == AttributeType.DoubleAttack)
                {
                    var attackDir = direction;
                    var normalSkillId = soldierCard.NormalAttackSkillId;
                    var attacker = soldierCard;
                    DelayUseSkill(attacker, normalSkillId, attackDir).Forget();
                    continue;
                }

                // 从 Attributes 中获取冷却缩减时间
                if (attr.Type == AttributeType.SkillCdTime || attr.ValueType == ValueType.Number)
                {
                    float reduceSeconds = attr.Value;

                    // 获取当前角色所有技能卡
                    var allSkills = soldierCard.GetSkillAll();
                    foreach (var skillPair in allSkills)
                    {
                        var skillId = skillPair.Key;
                        
                        // 调用 GameLand.Default.ReduceSkillCooldown 减少冷却时间
                        if (GamePlayManager.Default != null && GameLand.Default != null)
                        {
                            var success = GameLand.Default.ReduceSkillCooldown(skillId, reduceSeconds);
                            if (success)
                            {
                                LogTool.L($"[EffectsExecutor] ExecuteAttribute: Reduced cooldown for skill {skillId} by {reduceSeconds}s");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 延迟使用技能（用于 DoubleAttack 等需要延迟触发的效果）
        /// </summary>
        private static async UniTaskVoid DelayUseSkill(SoldierCard soldierCard, int skillId, Vector3 direction)
        {
            await UniTask.Delay((int)(0.1f * 1000));
            if (soldierCard != null)
            {
                soldierCard.UseSkill(skillId, direction, skipBeforeTriggerEvent: true);
                LogTool.L($"[EffectsExecutor] DelayUseSkill: delayed attack executed without Before trigger event, skill {skillId}");
            }
        }

        /// <summary>
        /// 怪物连续受击造成最大生命值真实伤害
        /// 读取 MonsterContinuousDamage3CauseTrueDamage 的值，对目标怪物的最大生命值造成百分比伤害
        /// </summary>
        private static void ExecuteMonsterContinuousDamage(SoldierCard caster, object target, float percent)
        {
            if (target == null)
            {
                LogTool.W("[EffectsExecutor] ExecuteMonsterContinuousDamage: target is null");
                return;
            }

            if (percent <= 0)
            {
                LogTool.W($"[EffectsExecutor] ExecuteMonsterContinuousDamage: Invalid percent value: {percent}");
                return;
            }

            var monsterCard = target as MonsterCard;
            if (monsterCard == null)
            {
                LogTool.W($"[EffectsExecutor] ExecuteMonsterContinuousDamage: target is not MonsterCard, target={target?.GetType().Name}");
                return;
            }

            // 获取目标的最大生命值
            int maxHp = (int)monsterCard.Attributes.GetFinal(AttributeType.MonsterMaxHealth);
            if (maxHp <= 0)
            {
                LogTool.W($"[EffectsExecutor] ExecuteMonsterContinuousDamage: Monster has no max HP, target={monsterCard}");
                return;
            }

            // 计算伤害
            var damage = Mathf.CeilToInt(maxHp * percent);

            // 对目标造成真实伤害（使用不需要skillCard的重载方法）
            monsterCard.ReceiveDamage(MonsterDamageRequest.Single(
                monsterCard, TakeDamageType.RealDamage, damage, null, monsterCard.transform.position,
                pipelineType: DamagePipelineType.BuffSkillDamage));
            LogTool.L($"[EffectsExecutor] ExecuteMonsterContinuousDamage: Dealt {damage} real damage ({percent * 100}% of maxHP={maxHp}) to monster");
        }

        /// <summary>
        /// 施加 Buff（buffId 为 SkillBuff 模板 Id）。
        /// </summary>
        public static void ExecuteTakeBuff(
            int buffId,
            SkillContext context,
            object owner,
            object target = null)
        {
            if (buffId <= 0)
            {
                LogTool.LogWarning("[EffectsExecutor] TakeBuff: Invalid buffId");
                return;
            }

            var buffable = owner as IBuffable;
            if (buffable != null)
            {
                if (BuffApplier.ApplyBuff(buffId, context, buffable))
                {
                    LogTool.Log($"[EffectsExecutor] TakeBuff: Applied buff {buffId} to owner");
                }
                else
                {
                    LogTool.LogWarning($"[EffectsExecutor] TakeBuff: Failed to apply buff {buffId} to owner");
                }
            }
            else
            {
                LogTool.LogWarning($"[EffectsExecutor] TakeBuff: Owner is not IBuffable, owner={owner?.GetType().Name}");
            }
        }
        
        /// <summary>
        /// 移除Buff
        /// </summary>
        private static void ExecuteRemoveBuff(
            EffectsConfig effectConfig,
            SkillContext context,
            object owner,
            object target)
        {
            if (effectConfig.EffectsId <= 0)
            {
                LogTool.W("[EffectsExecutor] RemoveBuff: Invalid EffectsId");
                return;
            }

            var buffIdToRemove = effectConfig.EffectsId;

            // 尝试获取 BuffController
            BuffController buffController = null;
            if (owner is SoldierCard soldierCard)
            {
                buffController = soldierCard.BuffController;
            }
            else if (owner is MonsterCard monsterCard)
            {
                buffController = monsterCard.BuffController;
            }

            if (buffController == null)
            {
                LogTool.W("[EffectsExecutor] RemoveBuff: Owner has no BuffController");
                return;
            }

            // 查找并移除匹配的 Buff
            var allBuffs = buffController.GetAllBuffs();
            foreach (var buff in allBuffs)
            {
                if (buff.BuffId == buffIdToRemove)
                {
                    buffController.RemoveBuff(buff);
                    LogTool.L($"[EffectsExecutor] RemoveBuff: Removed buff {buffIdToRemove} from owner");
                    return;
                }
            }

            LogTool.W($"[EffectsExecutor] RemoveBuff: Buff {buffIdToRemove} not found on owner");
        }
    }
}
