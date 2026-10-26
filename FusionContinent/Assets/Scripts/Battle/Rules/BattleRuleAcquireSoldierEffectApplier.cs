using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using Manager;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// EffectGroupsType.ModifyAttr 的英灵持久属性执行器。
    /// <para>
    /// 适用场景：NoCondition / HpChange 规则获得时，或新召唤英灵补同步。
    /// 将 BaseValue/PreValue 转为 AttributeModifier 写入 SoldierCard.BaseAttributes 或技能 AttributeSet。
    /// </para>
    /// </summary>
    public static class BattleRuleAcquireSoldierEffectApplier
    {
        /// <summary>将效果列表应用到全场存活英灵。</summary>
        public static void ApplyEffects(
            IReadOnlyList<EffectParamStruct> effects,
            string sourceKey,
            AttributeSourceType sourceType)
        {
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                LogTool.LogWarning("[BattleRuleAcquireSoldierEffectApplier] GamePlayManager 不可用");
                return;
            }

            gamePlay.TraverseVillageSoldier(hero =>
            {
                if (!hero.isDie)
                {
                    ApplyEffectsToSoldier(effects, sourceKey, sourceType, hero);
                }
            });
        }

        /// <summary>将效果列表应用到单个英灵。</summary>
        public static void ApplyEffectsToSoldier(
            IReadOnlyList<EffectParamStruct> effects,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard soldier)
        {
            if (effects == null || effects.Count == 0 || soldier == null || soldier.isDie)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                ApplyEffectToSoldier(effects[i], sourceKey, sourceType, soldier);
            }
        }

        /// <summary>
        /// 对单个英灵施加一条 ModifyAttr 效果。
        /// 非 ModifyAttr 类型直接跳过（由 BattleRuleEffectGroupsApplier 负责分发）。
        /// </summary>
        public static void ApplyEffectToSoldier(
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard soldier)
        {
            ApplyEffectToSoldier(effect, sourceKey, sourceType, soldier, false);
        }

        public static void ApplyEffectToSoldier(
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard soldier,
            bool useAdditiveModifierStack,
            ISkill sourceSkill = null,
            BattleRuleTriggerContext ruleContext = null)
        {
            if (effect == null || soldier == null || soldier.isDie)
            {
                return;
            }

            if (effect.EffectType != EffectGroupsType.ModifyAttr)
            {
                return;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(effect.AttributeType);
            if (attrCfg == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleAcquireSoldierEffectApplier] 属性配置不存在: {effect.AttributeType}");
                return;
            }

            if (attrCfg.Category == AttributeCategoryType.Skill)
            {
                foreach (var skillPair in soldier.GetSkillAll())
                {
                    var skill = skillPair.Value;
                    if (skill == null || skill.Attributes == null)
                    {
                        continue;
                    }

                    ApplyEffectToAttributeSet(
                        skill.Attributes,
                        effect,
                        sourceKey,
                        sourceType,
                        soldier,
                        useAdditiveModifierStack,
                        sourceSkill,
                        ruleContext);
                }

                return;
            }

            ApplyEffectToAttributeSet(
                soldier.BaseAttributes,
                effect,
                sourceKey,
                sourceType,
                soldier,
                useAdditiveModifierStack,
                sourceSkill,
                ruleContext);
        }

        /// <summary>
        /// 将一条 ModifyAttr 写入指定 AttributeSet（英灵 / 技能属性）。
        /// </summary>
        public static void ApplyEffectToAttributeSet(
            AttributeSet attributeSet,
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard sourceSoldier,
            bool useAdditiveModifierStack,
            ISkill sourceSkill = null,
            BattleRuleTriggerContext ruleContext = null)
        {
            if (attributeSet == null || effect == null || effect.EffectType != EffectGroupsType.ModifyAttr)
            {
                return;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(effect.AttributeType);
            if (attrCfg == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleAcquireSoldierEffectApplier] 属性配置不存在: {effect.AttributeType}");
                return;
            }

            if (attrCfg.Category != AttributeCategoryType.Soldier
                && attrCfg.Category != AttributeCategoryType.Skill)
            {
                LogTool.LogWarning(
                    $"[BattleRuleAcquireSoldierEffectApplier] 仅支持英灵/技能属性 ModifyAttr，已跳过: {effect.AttributeType}");
                return;
            }

            var modifier = BattleRuleEffectParamUtil.BuildModifier(
                effect, sourceKey, sourceType, sourceSoldier, sourceSkill, ruleContext);

            var attrFinalBefore = attributeSet.GetFinal(effect.AttributeType);
            attributeSet.GetAttr(effect.AttributeType).AddModifier(modifier, useAdditiveModifierStack);
            var attrFinalAfter = attributeSet.GetFinal(effect.AttributeType);

            var ruleId = TryParseRuleIdFromSourceKey(sourceKey);
            DamagePipelineFlowLogger.LogAcquireAttribute(
                ruleId, effect, modifier, sourceKey, attrFinalBefore, attrFinalAfter);

            if (!DamagePipelineFlowLogger.IsEnabled)
            {
                LogTool.Log(
                    $"[BattleRuleAcquireSoldierEffectApplier] 已施加属性 {effect.AttributeType}, Flat={modifier.FlatAdd}, Percent={modifier.PercentAdd}, key={sourceKey}");
            }
        }

        private static int TryParseRuleIdFromSourceKey(string sourceKey)
        {
            if (string.IsNullOrEmpty(sourceKey))
            {
                return 0;
            }

            const string acquirePrefix = "BattleRuleAcquire_";
            const string damagePrefix = "BattleRuleDamage_";
            string rest = null;
            if (sourceKey.StartsWith(acquirePrefix))
            {
                rest = sourceKey.Substring(acquirePrefix.Length);
            }
            else if (sourceKey.StartsWith(damagePrefix))
            {
                rest = sourceKey.Substring(damagePrefix.Length);
            }

            if (rest == null)
            {
                return 0;
            }

            var underscore = rest.IndexOf('_');
            var idPart = underscore >= 0 ? rest.Substring(0, underscore) : rest;
            if (int.TryParse(idPart, out var ruleId))
            {
                return ruleId;
            }

            return 0;
        }

        /// <summary>移除指定来源 Key 的 ModifyAttr 修饰（施法会话结束等场景）。</summary>
        public static void RemoveEffectFromAttributeSet(
            AttributeSet attributeSet,
            AttributeType attributeType,
            AttributeSourceType sourceType,
            string sourceKey)
        {
            if (attributeSet == null || string.IsNullOrEmpty(sourceKey))
            {
                return;
            }

            attributeSet.GetAttr(attributeType).RemoveModifier(sourceType, sourceKey);
        }
    }
}
