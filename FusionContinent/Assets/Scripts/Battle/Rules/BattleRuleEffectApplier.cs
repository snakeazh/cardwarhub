using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Utils;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 伤害管线中的效果数值执行器。
    /// <para>
    /// 处理 EffectGroupsType.AddADDamage / AddAPDamage / AddRealDamage（追加伤害）与 ModifyAttr（单次命中即时属性）。
    /// BaseValue / PreValue 分别独立结算，可同时生效。
    /// </para>
    /// </summary>
    public static class BattleRuleEffectApplier
    {
        /// <summary>
        /// EffectGroupsType.AddADDamage / AddAPDamage / AddRealDamage：向本次 DamageContext 追加伤害。
        /// BaseValue → 固定追加；PreValue → 比例追加或「来源属性 × PreValue」。
        /// </summary>
        public static void ApplyAddDamageEffect(DamageContext ctx, EffectParamStruct effect)
        {
            if (ctx == null || effect == null)
            {
                return;
            }

            var sourceValue = ResolveSourceValue(ctx, effect);

            // 固定伤害/数值
            if (BattleRuleEffectParamUtil.HasFlatValue(effect))
            {
                ApplyAddDamageMagnitudeByEffectType(ctx, effect, sourceValue, effect.BaseValue, usePercent: false);
            }

            // 比例伤害，或来源属性 × 系数
            if (BattleRuleEffectParamUtil.HasPercentValue(effect))
            {
                if (BattleRuleEffectParamUtil.HasSourceAttribute(effect))
                {
                    var scaledFlat = sourceValue * effect.PreValue;
                    ApplyAddDamageMagnitudeByEffectType(ctx, effect, sourceValue, scaledFlat, usePercent: false);
                }
                else
                {
                    ApplyAddDamageMagnitudeByEffectType(ctx, effect, sourceValue, effect.PreValue, usePercent: true);
                }
            }
        }

        public static void ApplyModifyAttrToDamageContext(DamageContext ctx, EffectParamStruct effect)
        {
            ApplyModifyAttrToDamageContext(ctx, effect, null);
        }

        /// <summary>
        /// EffectGroupsType.ModifyAttr 在伤害管线中的即时结算。
        /// 仅改动本次 DamageContext，不写入英灵 BaseAttributes。
        /// </summary>
        public static void ApplyModifyAttrToDamageContext(
            DamageContext ctx,
            EffectParamStruct effect,
            BattleRuleTriggerContext ruleContext)
        {
            if (ctx == null || effect == null)
            {
                return;
            }

            if (effect.AttributeType == AttributeType.AddMagicDamage
                && !BattleRuleDamageSupport.IsNormalAttack(ctx))
            {
                return;
            }

            if (BattleRuleEffectParamUtil.HasFlatValue(effect))
            {
                ApplyDamageAttributeData(ctx, new AttributeData
                {
                    Type = effect.AttributeType,
                    Value = effect.BaseValue,
                    ValueType = ValueType.Number,
                });
            }

            if (!BattleRuleEffectParamUtil.HasPercentValue(effect))
            {
                return;
            }

            // 链式学习来源参数：BaseValue + SourceAttr × PreValue → 按目标属性百分比写入当次伤害
            if (BattleRuleEffectParamUtil.UsesSkillSourceAttribute(effect))
            {
                var sourceSkill = BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext);
                var paramValue = BattleRuleEffectParamUtil.ResolveSourceAttributeValue(
                    effect, ctx.Soldier, sourceSkill, ruleContext);
                var bonus = effect.BaseValue + paramValue * effect.PreValue;
                ApplyDamageAttributeData(ctx, new AttributeData
                {
                    Type = effect.AttributeType,
                    Value = bonus,
                    ValueType = ValueType.Percentage,
                });
                return;
            }

            if (BattleRuleEffectParamUtil.HasSourceAttribute(effect))
            {
                // 来源属性 × 系数 → 按 AddDamage 逻辑追加到伤害字段
                var sourceValue = BattleRuleEffectParamUtil.ResolveSourceAttributeValue(ctx, effect);
                ApplyAddDamageMagnitude(ctx, effect, sourceValue, sourceValue * effect.PreValue, usePercent: false);
                return;
            }

            ApplyDamageAttributeData(ctx, new AttributeData
            {
                Type = effect.AttributeType,
                Value = effect.PreValue,
                ValueType = ValueType.Percentage,
            });
        }

        /// <summary>
        /// 按 EffectGroupsType 将 magnitude 写入 DamageContext 对应伤害字段，保证飘字颜色/样式正确。
        /// <para>
        /// AddADDamage → 物理（白字）；AddAPDamage → 魔法（蓝字）；AddRealDamage → 真实（白字+流派底图）。
        /// 与主伤害同类型时合并进 FinalDamage；否则写入 Attached* / TrueDamage 独立结算。
        /// </para>
        /// </summary>
        private static void ApplyAddDamageMagnitudeByEffectType(
            DamageContext ctx,
            EffectParamStruct effect,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            switch (effect.EffectType)
            {
                case EffectGroupsType.AddADDamage:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        ApplyDamageDelta(ctx, ref ctx.FinalDamage, sourceValue, magnitude, usePercent);
                    }
                    else
                    {
                        ApplyAttachedPhysicalDamage(ctx, sourceValue, magnitude, usePercent);
                    }
                    break;

                case EffectGroupsType.AddAPDamage:
                    if (ctx.DamageType == TakeDamageType.MagicDamage)
                    {
                        ApplyDamageDelta(ctx, ref ctx.FinalDamage, sourceValue, magnitude, usePercent);
                    }
                    else
                    {
                        ApplyAttachedMagicDamage(ctx, sourceValue, magnitude, usePercent);
                    }
                    break;

                case EffectGroupsType.AddRealDamage:
                    ApplyDamageDelta(ctx, ref ctx.TrueDamage, sourceValue, magnitude, usePercent);
                    break;
            }
        }

        /// <summary>非魔法主伤害时追加独立魔法伤害（结算后单独走 MagicDamage 管线，蓝字飘字）。</summary>
        private static void ApplyAttachedMagicDamage(
            DamageContext ctx,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            var bonus = usePercent ? sourceValue * magnitude : magnitude;
            ctx.AttachedMagicDamage += bonus;
        }

        /// <summary>非物理主伤害时追加独立物理伤害（结算后单独走 PhysicalDamage 管线，白字飘字）。</summary>
        private static void ApplyAttachedPhysicalDamage(
            DamageContext ctx,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            var bonus = usePercent ? sourceValue * magnitude : magnitude;
            ctx.AttachedPhysicalDamage += bonus;
        }

        /// <summary>
        /// 按 AttributeType 将 magnitude 写入 DamageContext 对应字段（ModifyAttr 即时分支使用）。
        /// </summary>
        /// <param name="usePercent">true 时 magnitude 为比例（乘以 sourceValue 或当前伤害）</param>
        private static void ApplyAddDamageMagnitude(
            DamageContext ctx,
            EffectParamStruct effect,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            switch (effect.AttributeType)
            {
                case AttributeType.AddRealDamage:
                    ApplyDamageDelta(ctx, ref ctx.TrueDamage, sourceValue, magnitude, usePercent);
                    break;

                case AttributeType.AddPhysicalDamage:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        ApplyDamageDelta(ctx, ref ctx.FinalDamage, sourceValue, magnitude, usePercent);
                    }
                    break;

                case AttributeType.AddMagicDamage:
                    ApplyNormalAttackAttachedMagicDamage(ctx, sourceValue, magnitude, usePercent);
                    break;

                case AttributeType.AllDamageBonuses:
                    ApplyDamageDelta(ctx, ref ctx.FinalDamage, sourceValue, magnitude, usePercent);
                    ApplyDamageDelta(ctx, ref ctx.TrueDamage, sourceValue, magnitude, usePercent);
                    break;

                case AttributeType.PhysicalBloodSucking:
                    ApplyBloodSucking(ctx, ref ctx.PhysicalBloodSucking, magnitude, usePercent);
                    break;

                case AttributeType.MagicBloodSucking:
                    ApplyBloodSucking(ctx, ref ctx.MagicBloodSucking, magnitude, usePercent);
                    break;

                case AttributeType.SoldierArmorPenetration:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        ApplyPenetration(ctx, magnitude, usePercent, isPercentPen: false);
                    }
                    break;

                case AttributeType.SoldierMagicPenetration:
                    if (ctx.DamageType == TakeDamageType.MagicDamage)
                    {
                        ApplyPenetration(ctx, magnitude, usePercent, isPercentPen: false);
                    }
                    break;

                case AttributeType.SoldierArmorPenetrationPer:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        ApplyPenetration(ctx, magnitude, usePercent, isPercentPen: true);
                    }
                    break;

                case AttributeType.SoldierMagicPenetrationPer:
                    if (ctx.DamageType == TakeDamageType.MagicDamage)
                    {
                        ApplyPenetration(ctx, magnitude, usePercent, isPercentPen: true);
                    }
                    break;

                case AttributeType.NormalAttackDamage:
                    if (BattleRuleDamageSupport.IsNormalAttack(ctx))
                    {
                        ApplyDamageDelta(ctx, ref ctx.FinalDamage, sourceValue, magnitude, usePercent);
                    }
                    break;

                default:
                    // 未列出的 AttributeType：默认追加到 FinalDamage
                    if (!Mathf.Approximately(magnitude, 0f))
                    {
                        var bonus = usePercent ? sourceValue * magnitude : magnitude;
                        ctx.FinalDamage += bonus;
                    }
                    break;
            }
        }

        /// <summary>
        /// 判断 ModifyAttr 是否应直接改本次 <see cref="DamageContext"/>（单次命中即时，不写 AttributeSet）。
        /// <para>
        /// 其余英灵/技能属性（含 PreValue 百分比）应写入 AttributeSet，
        /// 由 <see cref="FusionContinent.Attribute.AttributeValue.FinalValue"/> 统一结算，
        /// 避免在伤害管线中重复对 FinalDamage 叠乘百分比。
        /// </para>
        /// </summary>
        public static bool IsImmediateDamageAttribute(AttributeCategoryType category, AttributeType type)
        {
            if (category == AttributeCategoryType.Module)
            {
                return false;
            }

            return type switch
            {
                AttributeType.AddMagicDamage or
                AttributeType.PerMaxHpRealDamage or
                AttributeType.SoldierArmorPenetration or
                AttributeType.SoldierMagicPenetration or
                AttributeType.SoldierArmorPenetrationPer or
                AttributeType.SoldierMagicPenetrationPer or
                AttributeType.AllDamageBonuses => true,
                _ => false
            };
        }

        /// <summary>
        /// 将单条 AttributeData 应用到 DamageContext（含概率判定）。
        /// 供 ModifyAttr 即时分支使用。
        /// </summary>
        public static void ApplyDamageAttributeData(DamageContext ctx, AttributeData attr)
        {
            if (ctx == null || attr == null)
            {
                return;
            }

            var probabilityTrigger = attr.ProbabilityTrigger == 0 ? 1f : attr.ProbabilityTrigger;
            if (RandomTool.RandomRangeTarget(0, 100) >= (int)(probabilityTrigger * 100))
            {
                return;
            }

            switch (attr.Type)
            {
                case AttributeType.AddRealDamage:
                    if (attr.ValueType == ValueType.Number)
                    {
                        ctx.TrueDamage += attr.Value;
                    }
                    else
                    {
                        ctx.TrueDamage += ctx.TrueDamage * attr.Value;
                    }
                    break;

                case AttributeType.PerMaxHpRealDamage:
                    if (attr.ValueType == ValueType.Number && ctx.Soldier != null)
                    {
                        ctx.TrueDamage += attr.Value * ctx.Soldier.BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
                    }
                    break;

                case AttributeType.AllDamageBonuses:
                    ctx.FinalDamage += ctx.FinalDamage * attr.Value;
                    ctx.TrueDamage += ctx.TrueDamage * attr.Value;
                    break;

                case AttributeType.PhysicalBloodSucking:
                    if (attr.ValueType == ValueType.Number)
                    {
                        ctx.PhysicalBloodSucking += attr.Value;
                    }
                    else
                    {
                        ctx.PhysicalBloodSucking *= 1f + attr.Value;
                    }
                    break;

                case AttributeType.MagicBloodSucking:
                    if (attr.ValueType == ValueType.Number)
                    {
                        ctx.MagicBloodSucking += attr.Value;
                    }
                    else
                    {
                        ctx.MagicBloodSucking *= 1f + attr.Value;
                    }
                    break;

                case AttributeType.SoldierArmorPenetration:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        if (attr.ValueType == ValueType.Number)
                        {
                            ctx.resistanceIgnoreValue += attr.Value;
                        }
                        else
                        {
                            ctx.resistanceIgnorePenre *= 1f + attr.Value;
                        }
                    }
                    break;

                case AttributeType.SoldierMagicPenetration:
                    if (ctx.DamageType == TakeDamageType.MagicDamage)
                    {
                        if (attr.ValueType == ValueType.Number)
                        {
                            ctx.resistanceIgnoreValue += attr.Value;
                        }
                        else
                        {
                            ctx.resistanceIgnorePenre *= 1f + attr.Value;
                        }
                    }
                    break;

                case AttributeType.SoldierArmorPenetrationPer:
                    if (ctx.DamageType == TakeDamageType.PhysicalDamage)
                    {
                        ApplyPercentPenetration(ctx, attr.Value);
                    }
                    break;

                case AttributeType.SoldierMagicPenetrationPer:
                    if (ctx.DamageType == TakeDamageType.MagicDamage)
                    {
                        ApplyPercentPenetration(ctx, attr.Value);
                    }
                    break;

                case AttributeType.NormalAttackDamage:
                    if (!BattleRuleDamageSupport.IsNormalAttack(ctx))
                    {
                        break;
                    }

                    if (attr.ValueType == ValueType.Percentage)
                    {
                        ctx.FinalDamage *= 1f + attr.Value;
                    }
                    else
                    {
                        // 与 MonsterManager.damageInheritance += extraDamage 一致：0.5 → ×1.5
                        ctx.FinalDamage *= 1f + attr.Value;
                    }
                    break;
            }
        }

        private static void ApplyNormalAttackAttachedMagicDamage(
            DamageContext ctx,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            if (!BattleRuleDamageSupport.IsNormalAttack(ctx)
                || ctx.DamageType != TakeDamageType.PhysicalDamage)
            {
                return;
            }

            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            var bonus = usePercent ? sourceValue * magnitude : magnitude;
            ctx.AttachedMagicDamage += bonus;
        }

        /// <summary>追加伤害到 targetDamage：固定值直接加，比例值乘以 sourceValue 或当前伤害。</summary>
        private static void ApplyDamageDelta(
            DamageContext ctx,
            ref float targetDamage,
            float sourceValue,
            float magnitude,
            bool usePercent)
        {
            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            if (usePercent)
            {
                var baseValue = sourceValue > 0f ? sourceValue : targetDamage;
                targetDamage += baseValue * magnitude;
            }
            else
            {
                targetDamage += magnitude;
            }
        }

        private static void ApplyBloodSucking(DamageContext ctx, ref float bloodSucking, float magnitude, bool usePercent)
        {
            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            if (usePercent)
            {
                bloodSucking *= 1f + magnitude;
            }
            else
            {
                bloodSucking += magnitude;
            }
        }

        /// <summary>
        /// 穿透比例：配置 1 表示 100% 穿透，0.5 表示 50%；大于 1 时按百分数（如 100）解析。
        /// </summary>
        private static float NormalizePenetrationRatio(float value)
        {
            if (value > 1f)
            {
                return Mathf.Clamp01(value / 100f);
            }

            return Mathf.Clamp01(value);
        }

        private static void ApplyPercentPenetration(DamageContext ctx, float ratio)
        {
            ctx.resistanceIgnorePreValueArmor += NormalizePenetrationRatio(ratio);
        }

        private static void ApplyPenetration(DamageContext ctx, float magnitude, bool usePercent, bool isPercentPen)
        {
            if (Mathf.Approximately(magnitude, 0f))
            {
                return;
            }

            if (isPercentPen)
            {
                ApplyPercentPenetration(ctx, magnitude);
                return;
            }

            if (usePercent)
            {
                ctx.resistanceIgnorePenre *= 1f + magnitude;
            }
            else
            {
                ctx.resistanceIgnoreValue += magnitude;
            }
        }

        /// <summary>
        /// 解析追加伤害的来源基准值。
        /// 未配置 SourceAttrType 时用当前 FinalDamage；否则读英灵/怪物属性。
        /// </summary>
        private static float ResolveSourceValue(DamageContext ctx, EffectParamStruct effect)
        {
            if (!BattleRuleEffectParamUtil.HasSourceAttribute(effect))
            {
                return ctx.FinalDamage;
            }

            return BattleRuleEffectParamUtil.ResolveSourceAttributeValue(ctx, effect);
        }
    }
}
