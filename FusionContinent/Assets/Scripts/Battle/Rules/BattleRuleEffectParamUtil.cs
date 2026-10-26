using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// EffectParamStruct 参数解析工具。
    /// <para>
    /// 配置约定：
    /// - BaseValue：固定加值，映射到 AttributeModifier.FlatAdd；
    /// - PreValue：百分比加值，映射到 AttributeModifier.PercentAdd；
    /// - 二者可同时非零，最终公式：(基础值 × (1 + PercentAdd)) + FlatAdd。
    /// </para>
    /// <para>
    /// 当 PreValue 非零且 SourceAttrType != Null(201) 时，
    /// 额外将「来源属性 × PreValue」累加到 FlatAdd（系数型固定值，可与 BaseValue 叠加）。
    /// </para>
    /// </summary>
    public static class BattleRuleEffectParamUtil
    {
        /// <summary>BaseValue 是否有效（非零固定值）。</summary>
        public static bool HasFlatValue(EffectParamStruct effect)
        {
            return effect != null && !Mathf.Approximately(effect.BaseValue, 0f);
        }

        /// <summary>PreValue 是否有效（非零百分比或系数）。</summary>
        public static bool HasPercentValue(EffectParamStruct effect)
        {
            return effect != null && !Mathf.Approximately(effect.PreValue, 0f);
        }

        /// <summary>
        /// 是否配置了来源属性。
        /// Null(201) 为配表占位，表示「未配置来源」；此时 PreValue 走百分比分支。
        /// </summary>
        public static bool HasSourceAttribute(EffectParamStruct effect)
        {
            return effect != null && effect.SourceAttrType != AttributeType.Null;
        }

        /// <summary>
        /// 来源属性是否为规则参数（Category=Add / Skill）：从技能实例或链式学习来源配置读取。
        /// 同属性 PercentAdd 分支（SourceAttrType == AttributeType）不读来源值，不计入。
        /// </summary>
        public static bool UsesSkillSourceAttribute(EffectParamStruct effect)
        {
            if (!HasPercentValue(effect) || !HasSourceAttribute(effect))
            {
                return false;
            }

            if (effect.EffectType == EffectGroupsType.ModifyAttr
                && effect.SourceAttrType == effect.AttributeType)
            {
                return false;
            }

            return BattleRuleSourceParamSupport.IsRuleSourceParamAttribute(effect.SourceAttrType);
        }

        /// <summary>
        /// 将 EffectParamStruct 转为 AttributeModifier，供英灵/模块持久属性使用。
        /// </summary>
        /// <param name="effect">效果参数</param>
        /// <param name="sourceKey">修饰来源 Key（如 BattleRuleAcquire_{ruleId}）</param>
        /// <param name="sourceType">修饰来源类型</param>
        /// <param name="soldier">Self 来源时的英灵；模块属性或全场逻辑可为 null</param>
        /// <param name="sourceSkill">技能来源规则的技能实例；SourceAttrType 为技能类属性时从此读取按等级数值</param>
        /// <param name="ruleContext">链式学习来源上下文（装备/流派等配置型参数）</param>
        public static AttributeModifier BuildModifier(
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard soldier,
            ISkill sourceSkill = null,
            BattleRuleTriggerContext ruleContext = null)
        {
            var modifier = new AttributeModifier
            {
                SourceKey = sourceKey,
                SourceType = sourceType,
                FlatAdd = HasFlatValue(effect) ? effect.BaseValue : 0f,
                PercentAdd = 0f,
            };

            if (!HasPercentValue(effect))
            {
                return modifier;
            }

            if (HasSourceAttribute(effect))
            {
                // 同属性 PreValue：表示对该属性 BaseValue 的百分比加成（PercentAdd），
                // 而非「当前 FinalValue × PreValue」追加 FlatAdd（否则会随叠层指数放大）。
                if (effect.EffectType == EffectGroupsType.ModifyAttr
                    && effect.SourceAttrType == effect.AttributeType)
                {
                    modifier.PercentAdd = effect.PreValue;
                }
                else
                {
                    modifier.FlatAdd += ResolveSourceAttributeValue(effect, soldier, sourceSkill, ruleContext)
                        * effect.PreValue;
                }
            }
            else
            {
                modifier.PercentAdd = effect.PreValue;
            }

            return modifier;
        }

        /// <summary>
        /// 解析持久属性场景下的来源属性值（英灵/模块/技能）。
        /// 规则参数（Category=Skill）→ 技能实例或链式学习来源配置；
        /// SourceUnitType.Target → 模块属性；Self → 指定英灵或代表英灵。
        /// </summary>
        public static float ResolveSourceAttributeValue(
            EffectParamStruct effect,
            SoldierCard soldier,
            ISkill sourceSkill = null,
            BattleRuleTriggerContext ruleContext = null)
        {
            if (UsesSkillSourceAttribute(effect))
            {
                if (sourceSkill != null)
                {
                    return sourceSkill.Attributes.GetFinal(effect.SourceAttrType);
                }

                if (ruleContext != null)
                {
                    return BattleRuleSourceParamSupport.ResolveParamValue(
                        ruleContext, effect.SourceAttrType);
                }

                LogTool.LogWarning(
                    $"[BattleRuleEffectParamUtil] 规则参数未解析到来源，取值 0: SourceAttrType={effect.SourceAttrType}");
                return 0f;
            }

            if (effect.SourceUnitType == UnitType.Target)
            {
                return GamePlayManager.Default.ModuleAttributes.GetFinal(effect.SourceAttrType);
            }

            if (soldier != null)
            {
                return soldier.BaseAttributes.GetFinal(effect.SourceAttrType);
            }

            // 全场逻辑无指定英灵时，取代表英灵读 Self 来源
            var representative = BattleRuleSoldierHealthConditionValues.ResolveRepresentativeSoldier();
            return representative != null
                ? representative.BaseAttributes.GetFinal(effect.SourceAttrType)
                : 0f;
        }

        /// <summary>
        /// 解析单次伤害场景下的来源属性值。
        /// Self → 攻击英灵；Target → 受击怪物。
        /// MonsterMaxHealth / MonsterHealth 作为伤害缩放来源时，固定读本次受击怪物（最大生命 / 当前生命），与 SourceUnitType 无关。
        /// </summary>
        public static float ResolveSourceAttributeValue(DamageContext ctx, EffectParamStruct effect)
        {
            if (ctx != null && ctx.Monster != null)
            {
                if (effect.SourceAttrType == AttributeType.MonsterMaxHealth)
                {
                    var maxHp = ctx.Monster.Attributes.GetFinal(AttributeType.MonsterMaxHealth);
                    if (maxHp > 0f)
                    {
                        return maxHp;
                    }

                    if (ctx.Monster.maxHp > 0)
                    {
                        return ctx.Monster.maxHp;
                    }

                    return 0f;
                }

                if (effect.SourceAttrType == AttributeType.MonsterHealth)
                {
                    return ctx.Monster.hp;
                }
            }

            if (effect.SourceUnitType == UnitType.Self)
            {
                return ctx.Soldier != null
                    ? ctx.Soldier.BaseAttributes.GetFinal(effect.SourceAttrType)
                    : 0f;
            }

            return ctx.Monster != null
                ? ctx.Monster.Attributes.GetFinal(effect.SourceAttrType)
                : 0f;
        }
    }
}
