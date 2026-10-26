using Config;
using FusionContinent;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// EffectGroupsType.AddBuff 执行器。
    /// BaseValue 为 SkillBuffTemplateLibrary.BuffId，经 BuffApplier 施加。
    /// </summary>
    public static class BattleRuleLearnBuffEffectApplier
    {
        /// <summary>旧版 AttributeStruct 入口（LearnBuffEffect 类型，兼容保留）。</summary>
        public static bool TryApply(AttributeStruct attr, BattleRuleTriggerContext ruleContext)
        {
            if (attr.Type != AttributeType.LearnBuffEffect)
            {
                return false;
            }

            return ApplyBuff(BattleRuleEffectTargets.ResolveBuffId(attr), ruleContext);
        }

        /// <summary>
        /// EffectParamStruct 入口。
        /// BaseValue = SkillBuffTemplateLibrary.BuffId；EffectType 必须为 AddBuff。
        /// TargetUnitType：Self → 英灵；Target → 受击怪物（OnDamageAfter 冻结等）。
        /// </summary>
        public static bool TryApply(EffectParamStruct effect, BattleRuleTriggerContext ruleContext)
        {
            if (effect.EffectType != EffectGroupsType.AddBuff)
            {
                return false;
            }

            return ApplyBuff(BattleRuleEffectTargets.ResolveBuffId(effect), ruleContext, effect.TargetUnitType);
        }

        private static bool ApplyBuff(int buffId, BattleRuleTriggerContext ruleContext)
        {
            return ApplyBuff(buffId, ruleContext, UnitType.Self);
        }

        private static bool ApplyBuff(
            int buffId,
            BattleRuleTriggerContext ruleContext,
            UnitType targetUnitType)
        {
            if (buffId <= 0)
            {
                LogTool.LogError($"[BattleRuleLearnBuffEffectApplier] 无效的 Buff Id: {buffId}");
                return true;
            }

            if (!TryCreateSkillContext(ruleContext, out var skillContext))
            {
                return true;
            }

            var owners = ResolveOwners(ruleContext, targetUnitType);
            if (owners.Count == 0)
            {
                LogTool.LogWarning(
                    $"[BattleRuleLearnBuffEffectApplier] 未找到可施加 Buff 的单位: buffId={buffId}, target={targetUnitType}");
                return true;
            }

            for (int i = 0; i < owners.Count; i++)
            {
                var hero = owners[i] as SoldierCard;
                var monster = owners[i] as MonsterCard;
                string ownerInfo;
                if (hero != null)
                {
                    ownerInfo = $"hero={hero.cardId}, hp={hero.CurrentHp}/{hero.MaxHp}";
                }
                else if (monster != null)
                {
                    ownerInfo = $"monster={monster.name}, hp={monster.hp}";
                }
                else
                {
                    ownerInfo = $"owner={owners[i].GetType().Name}";
                }

                var triggerName = ruleContext != null ? ruleContext.Trigger.ToString() : "null";
                LogTool.Log(
                    $"[BattleRuleLearnBuffEffectApplier] AddBuff: buffId={buffId}, trigger={triggerName}, target={targetUnitType}, skillId={skillContext.SkillId}, {ownerInfo}");

                var buffable = owners[i] as IBuffable;
                if (buffable == null)
                {
                    LogTool.LogWarning(
                        $"[BattleRuleLearnBuffEffectApplier] 目标不是 IBuffable: {owners[i].GetType().Name}");
                    continue;
                }

                if (!BuffApplier.ApplyBuff(buffId, skillContext, buffable))
                {
                    LogTool.LogWarning(
                        $"[BattleRuleLearnBuffEffectApplier] 施加 Buff 失败: buffId={buffId}, {ownerInfo}");
                }
            }

            return true;
        }

        /// <summary>
        /// 构造 BuffApplier 使用的 SkillContext。
        /// 技能来源（Skill / GeneralSkill）会绑定施法者与技能实例，供模板 *AttrName 读取等级属性；
        /// 流派卡/装备等无技能实例的来源回退模板常量。
        /// </summary>
        /// <returns>false 表示技能来源英灵不可用，应跳过整次 AddBuff。</returns>
        private static bool TryCreateSkillContext(BattleRuleTriggerContext ruleContext, out SkillContext skillContext)
        {
            skillContext = null;
            if (ruleContext == null)
            {
                skillContext = new SkillContext();
                return true;
            }

            var damageContext = ruleContext.DamageContext;
            if (damageContext != null && damageContext.SkillId > 0)
            {
                skillContext = CreateSkillContextFromDamage(ruleContext, damageContext);
                return true;
            }

            var sourceSkillId = BattleRuleSkillSourceSupport.ResolveSourceSkillId(ruleContext);
            if (sourceSkillId > 0
                && BattleRuleSkillSourceSupport.UsesSkillInstanceForValues(ruleContext.ModifierSourceType))
            {
                var sourceSoldier = BattleRuleSkillSourceSupport.ResolveSourceSoldier(ruleContext, sourceSkillId);
                if (sourceSoldier == null || sourceSoldier.isDie)
                {
                    LogTool.Log(
                        $"[BattleRuleLearnBuffEffectApplier] 技能来源英灵不可用，跳过 AddBuff: skillId={sourceSkillId}, scopeHero={ruleContext.ScopeHeroCardId}");
                    return false;
                }

                var skillCard = sourceSoldier.GetSkillById(sourceSkillId);
                var skillLevel = skillCard != null ? skillCard.Level : 1;
                skillContext = new SkillContext
                {
                    SkillId = sourceSkillId,
                    Level = skillLevel,
                    Caster = sourceSoldier,
                    Target = ruleContext.Target as Component,
                    TargetPosition = sourceSoldier.transform.position,
                };
                return true;
            }

            skillContext = CreateFallbackSkillContext(ruleContext);
            BattleRuleSourceParamSupport.AttachParamSkillToContext(ruleContext, skillContext);
            return true;
        }

        private static SkillContext CreateSkillContextFromDamage(
            BattleRuleTriggerContext ruleContext,
            DamageContext damageContext)
        {
            var soldierComponent = damageContext.Soldier as Component ?? ruleContext.Source as Component;
            var monsterComponent = damageContext.Monster as Component ?? ruleContext.Target as Component;
            var skillId = damageContext.SkillId;

            return new SkillContext
            {
                SkillId = skillId,
                Level = skillId > 0 && Global.GetCardById(skillId) != null ? Global.GetCardById(skillId).Lv : 1,
                Caster = soldierComponent,
                Target = monsterComponent,
                TargetPosition = monsterComponent != null
                    ? monsterComponent.transform.position
                    : soldierComponent != null
                        ? soldierComponent.transform.position
                        : Vector3.zero,
            };
        }

        private static SkillContext CreateFallbackSkillContext(BattleRuleTriggerContext ruleContext)
        {
            var soldierComponent = ruleContext.Source as Component;
            var monsterComponent = ruleContext.Target as Component;

            return new SkillContext
            {
                SkillId = 0,
                Level = 1,
                Caster = soldierComponent,
                Target = monsterComponent,
                TargetPosition = monsterComponent != null
                    ? monsterComponent.transform.position
                    : soldierComponent != null
                        ? soldierComponent.transform.position
                        : Vector3.zero,
            };
        }

        private static System.Collections.Generic.List<object> ResolveOwners(
            BattleRuleTriggerContext ruleContext,
            UnitType targetUnitType)
        {
            var owners = new System.Collections.Generic.List<object>();

            if (targetUnitType == UnitType.Target)
            {
                // 治疗/护盾后：Target 为受益英灵；命中后冻结等：Target 为怪物
                var treatedSoldier = ResolveTargetSoldier(ruleContext);
                if (treatedSoldier != null)
                {
                    owners.Add(treatedSoldier);
                    return owners;
                }

                var monster = ResolveTargetMonster(ruleContext);
                if (monster != null)
                {
                    owners.Add(monster);
                }

                return owners;
            }

            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            for (int i = 0; i < soldiers.Count; i++)
            {
                owners.Add(soldiers[i]);
            }

            return owners;
        }

        /// <summary>治疗/护盾等对友方：优先 ruleContext.Target，其次 Source。</summary>
        private static SoldierCard ResolveTargetSoldier(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null)
            {
                return null;
            }

            if (ruleContext.Target is SoldierCard targetSoldier && !targetSoldier.isDie)
            {
                return targetSoldier;
            }

            return null;
        }

        /// <summary>命中后 AddBuff 到怪物：优先 DamageContext.Monster，其次 ruleContext.Target。</summary>
        private static MonsterCard ResolveTargetMonster(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null)
            {
                return null;
            }

            if (ruleContext.DamageContext != null && ruleContext.DamageContext.Monster != null)
            {
                return ruleContext.DamageContext.Monster;
            }

            return ruleContext.Target as MonsterCard;
        }
    }
}
