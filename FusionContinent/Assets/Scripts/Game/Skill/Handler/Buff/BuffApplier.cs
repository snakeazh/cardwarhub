using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Buff;
using FusionContinent.Skill;
using FusionContinent.Skill.Config;
using Game.Battle;
using UnityEngine;
using Unity.Profiling;
using AttributeType = Config.AttributeType;
using ValueType = Config.ValueType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// Buff应用工具类 - 统一处理所有Buff相关的操作
    /// 包括：Buff创建、效果值计算、属性获取、目标解析等
    /// </summary>
    public static class BuffApplier
    {
        #region Buff施加（主要API）

        /// <summary>
        /// 向目标施加Buff（通过BuffId获取配置）
        /// </summary>
        public static bool ApplyBuff(int buffId, SkillContext context, IBuffable target, int stack = 1, Vector3 hitPosition = default)
        {
            if (context != null && context.SkipBuffOnCast)
            {
                return false;
            }

            var effectConfigs = SkillBuffTemplateProvider.GetEffects(buffId);
            if (effectConfigs.Count == 0)
            {
                LogTool.LogWarning($"[BuffApplier] 未找到 Buff 模板: buffId={buffId}");
                return false;
            }

            bool applied = false;

            // 月影庇护等「护盾 + 属性」组合 Buff 由 ApplyBuffHandler 统一施加，避免 MultiEffectBuff 重复叠属性
            if (effectConfigs.Count > 1 && effectConfigs[0].BuffTypeId == (int)BuffType.MoonlightShield)
            {
                return false;
            }

            // 聚合模式检测：同一 buffId 配置了多个效果
            bool isMultiEffect = effectConfigs.Count > 1;

            if (isMultiEffect)
            {
                int resolvedSkillId;
                int maxStack;
                List<BuffEffectData> effects;
                using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerCreateBuff))
                {
                    effects = ConvertToBuffEffectData(effectConfigs, context, target, out resolvedSkillId, out maxStack);
                }
                var buffType = (BuffType)effectConfigs[0].BuffTypeId;

                var skillCard = GetSkillCard(context);
                float duration = ResolveFloat(skillCard, effectConfigs[0].DurationAttrName, effectConfigs[0].Duration);

                IBuff multiBuff;
                using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerCreateContext))
                {
                    multiBuff = BuffFactory.CreateMultiEffect(
                        buffId: buffId,
                        buffType: buffType,
                        duration: duration,
                        maxStack: maxStack,
                        effects: effects,
                        skillId: resolvedSkillId,
                        tickInterval: 0f,
                        caster: context?.Caster,
                        target: target,
                        context: context,
                        sourceKey: buffId > 0 ? buffId.ToString() : null);
                }

                if (multiBuff != null)
                {
                    multiBuff.BuffId = buffId;
                    if (!string.IsNullOrEmpty(effectConfigs[0].BuffVfx))
                        multiBuff.BuffVfx = effectConfigs[0].BuffVfx;
                    multiBuff.DamageType = ResolveDamageType(skillCard, effectConfigs[0].DamageTypeAttrName, effectConfigs[0].DamageType);

                    target.AddBuff(multiBuff);
                    NotifyTreatmentApplied(context, target, multiBuff.DamageType);
                    applied = true;
                }
            }
            else
            {
                // 单效果模式：原有逻辑
                foreach (var effectConfig in effectConfigs)
                {
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTriggerCondition))
                    {
                        if (UnityEngine.Random.value > Mathf.Clamp01(effectConfig.Probability))
                            continue;
                    }

                    if ((BuffType)effectConfig.BuffTypeId == BuffType.Execute)
                    {
                        // 斩杀由同次命中的伤害管线升格为 99999，不在此处单独结算
                        applied = true;
                        continue;
                    }

                    IBuff buff;
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerCreateBuff))
                    {
                        buff = CreateBuff(buffId, effectConfig, context, target);
                    }
                    if (buff == null) continue;

                    target.AddBuff(buff);
                    NotifyTreatmentApplied(context, target, buff.DamageType);
                    applied = true;
                }
            }

            return applied;
        }

        /// <summary>
        /// 以 <see cref="TakeDamageType.Treatment"/> 标识的治疗/护盾 Buff 施加成功后，触发 AfterTreatmentWasApplied。
        /// </summary>
        public static void NotifyTreatmentApplied(
            SkillContext context,
            IBuffable target,
            TakeDamageType damageType)
        {
            if (damageType != TakeDamageType.Treatment)
            {
                return;
            }

            var treatedSoldier = target as SoldierCard;
            if (treatedSoldier == null)
            {
                return;
            }

            SoldierCard caster = null;
            if (context != null && context.Caster != null)
            {
                caster = context.Caster.GetComponent<SoldierCard>();
            }

            BattleRuleTreatmentTrigger.Trigger(caster, treatedSoldier, damageType);
        }

        /// <summary>
        /// 将 SkillBuffEffectData 列表转换为 BuffEffectData 列表
        /// </summary>
        private static List<BuffEffectData> ConvertToBuffEffectData(
            List<SkillBuffEffectData> effectConfigs,
            SkillContext context,
            IBuffable buffTarget,
            out int resolvedSkillId,
            out int maxStack)
        {
            List<BuffEffectData> effects;
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTempList))
            {
                effects = new List<BuffEffectData>();
            }
            resolvedSkillId = context?.SkillId ?? 0;
            maxStack = 1;
            var skillCard = GetSkillCard(context);

            foreach (var ec in effectConfigs)
            {
                if (!string.IsNullOrEmpty(ec.AttrTypeName))
                {
                    if (TryResolveAttributeType(ec.AttrTypeName, out var attrType))
                    {
                        // 解析属性值：有AttrName读属性，没AttrName读常量
                        float effectValue = ResolveFloat(skillCard, ec.EffectValueAttrName, ec.EffectValue);
                        float percentEffectValue = ResolveFloat(skillCard, ec.PercentEffectValueAttrName, ec.PercentEffectValue);
                        bool useTargetAsValueSource = ec.BuffValueSource == "Target";
                        var damageType = ResolveDamageType(skillCard, ec.DamageTypeAttrName, ec.DamageType);

                        float finalValue = effectValue;

                        if (percentEffectValue != 0f)
                        {
                            finalValue += CalculatePercentValue(context, buffTarget, percentEffectValue,
                                useTargetAsValueSource, damageType, ec.PercentValueSourceAttrName);
                        }

                        effects.Add(new BuffEffectData
                        {
                            AttributeType = attrType,
                            Value = finalValue,
                            ValueType = ec.AttrValueType,
                            PercentValue = percentEffectValue
                        });
                    }
                }

                // 取最大层数
                maxStack = Mathf.Max(maxStack, Mathf.Max(1, ec.Stack));
            }

            return effects;
        }

        /// <summary>
        /// 向目标列表批量施加Buff
        /// </summary>
        public static void ApplyBuffToTargets(int buffId, SkillContext context, IEnumerable<IBuffable> targets)
        {
            foreach (var target in targets)
            {
                if (target != null)
                    ApplyBuff(buffId, context, target);
            }
        }

        #endregion

        #region 斩杀 Buff（命中时判定，不创建常驻 Buff）

        private const int ExecuteDamageValue = 99999;

        /// <summary>
        /// 命中模块 / ApplyBuff 节点注册斩杀 Buff，供伤害管线在受击前血量 ≤ 斩杀线时将本段伤害升格为 99999。
        /// </summary>
        public static void RegisterHitExecuteBuff(SkillContext context, int buffId)
        {
            if (context == null || buffId <= 0)
            {
                return;
            }

            var effectConfigs = SkillBuffTemplateProvider.GetEffects(buffId);
            for (int i = 0; i < effectConfigs.Count; i++)
            {
                if ((BuffType)effectConfigs[i].BuffTypeId != BuffType.Execute)
                {
                    continue;
                }

                if (!context.HitExecuteBuffIds.Contains(buffId))
                {
                    context.HitExecuteBuffIds.Add(buffId);
                }

                return;
            }
        }

        /// <summary>
        /// 伤害结算前写入模板斩杀线（绝对血量）。
        /// </summary>
        public static void PrepareTemplateExecuteThreshold(
            SkillContext context,
            MonsterCard monster,
            DamageContext damageContext)
        {
            damageContext.TemplateExecuteThreshold = 0f;
            damageContext.TemplateExecuteBuffId = 0;
            if (context == null || monster == null || context.HitExecuteBuffIds.Count == 0)
            {
                return;
            }

            if (monster._monsterConfig.MonsterQuality == MonsterQuality.Boss)
            {
                return;
            }

            float maxThreshold = 0f;
            int selectedBuffId = 0;

            for (int i = 0; i < context.HitExecuteBuffIds.Count; i++)
            {
                int buffId = context.HitExecuteBuffIds[i];
                var effectConfigs = SkillBuffTemplateProvider.GetEffects(buffId);
                for (int j = 0; j < effectConfigs.Count; j++)
                {
                    var effectConfig = effectConfigs[j];
                    if ((BuffType)effectConfig.BuffTypeId != BuffType.Execute)
                    {
                        continue;
                    }

                    if (UnityEngine.Random.value > Mathf.Clamp01(effectConfig.Probability))
                    {
                        continue;
                    }

                    float threshold = GetExecuteThreshold(effectConfig, context, monster);
                    if (threshold > maxThreshold)
                    {
                        maxThreshold = threshold;
                        selectedBuffId = buffId;
                    }
                }
            }

            damageContext.TemplateExecuteThreshold = maxThreshold;
            damageContext.TemplateExecuteBuffId = selectedBuffId;
        }

        public static void LogExecuteDamageJudgment(
            string result,
            DamageContext damageContext,
            MonsterCard monster,
            int hpForThreshold,
            bool moduleExecute,
            bool templateExecute)
        {
            int maxHp = monster.maxHp;
            float hpPercent = maxHp > 0 ? hpForThreshold / (float)maxHp : 0f;
            float moduleThresholdPercent = maxHp > 0
                ? GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.MonsterExecutionThreshold)
                : 0f;
            float templateThreshold = damageContext.TemplateExecuteThreshold;
            float templateThresholdPercent = maxHp > 0 ? templateThreshold / maxHp : 0f;
            string hpSource = damageContext.PreHitMonsterHp > 0 ? "PreHitMonsterHp" : "CurrentHp";

            LogTool.Log(
                $"[BuffApplier][Execute判定] {result} buffId={damageContext.TemplateExecuteBuffId}, " +
                $"skillId={damageContext.SkillId}, target={monster.name}({monster.uniqueId}), maxHp={maxHp}, " +
                $"hpForThreshold={hpForThreshold}({hpPercent * 100f:F1}%), hpSource={hpSource}, " +
                $"preHitHp={damageContext.PreHitMonsterHp}, currentHp={monster.hp}, " +
                $"templateThreshold={templateThreshold:F1}({templateThresholdPercent * 100f:F1}%), " +
                $"moduleThreshold={moduleThresholdPercent * 100f:F1}%, moduleExecute={moduleExecute}, templateExecute={templateExecute}, " +
                $"finalDamage={(damageContext.IsExecution ? ExecuteDamageValue : damageContext.FinalDamage)}");
        }

        /// <summary>
        /// 计算斩杀血量阈值：固定值，或来源属性 × 百分比（|v|&gt;1 为百分数，否则为小数比例）。
        /// </summary>
        public static float GetExecuteThreshold(
            SkillBuffEffectData effectConfig,
            SkillContext context,
            IBuffable target)
        {
            var skillCard = GetSkillCard(context);
            float threshold = ResolveFloat(skillCard, effectConfig.EffectValueAttrName, effectConfig.EffectValue);

            if (string.IsNullOrEmpty(effectConfig.PercentValueSourceAttrName))
            {
                return threshold;
            }

            if (!Enum.TryParse<AttributeType>(effectConfig.PercentValueSourceAttrName, out var attrType))
            {
                LogTool.LogWarning(
                    $"[BuffApplier] Execute 未知的阈值属性名: {effectConfig.PercentValueSourceAttrName}");
                return threshold;
            }

            bool useTargetAsValueSource = effectConfig.BuffValueSource == "Target";
            float attributeValue = ReadAttributeValue(context, target, attrType, useTargetAsValueSource);
            if (attributeValue <= 0f)
            {
                return threshold;
            }

            float percentEffectValue = ResolveFloat(
                skillCard, effectConfig.PercentEffectValueAttrName, effectConfig.PercentEffectValue);
            if (percentEffectValue <= 0f)
            {
                return threshold;
            }

            float percentMultiplier = Mathf.Abs(percentEffectValue) > 1f
                ? percentEffectValue / 100f
                : percentEffectValue;
            return attributeValue * percentMultiplier;
        }

        #endregion

        #region Buff创建

        /// <summary>
        /// 创建Buff实例
        /// </summary>
        public static IBuff CreateBuff(int buffId, SkillBuffEffectData effectConfig, SkillContext context, IBuffable target)
        {
            var buffType = (BuffType)effectConfig.BuffTypeId;
            var skillCard = GetSkillCard(context);

            // 解析属性值：有AttrName读属性，没AttrName读常量
            float duration = ResolveFloat(skillCard, effectConfig.DurationAttrName, effectConfig.Duration);
            float effectValue = ResolveFloat(skillCard, effectConfig.EffectValueAttrName, effectConfig.EffectValue);
            float tickInterval = ResolveFloat(skillCard, effectConfig.TickIntervalAttrName, effectConfig.TickInterval);
            float probability = ResolveFloat(skillCard, effectConfig.ProbabilityAttrName, effectConfig.Probability);
            int maxStack = Mathf.Max(1, ResolveInt(skillCard, effectConfig.StackAttrName, effectConfig.Stack));

            var useTargetAsValueSource = effectConfig.BuffValueSource == "Target";
            var damageType = ResolveDamageType(skillCard, effectConfig.DamageTypeAttrName, effectConfig.DamageType);

            // 百分比效果值计算
            float percentEffectValue = ResolveFloat(skillCard, effectConfig.PercentEffectValueAttrName, effectConfig.PercentEffectValue);
            float finalEffectValue = 0;

            if (percentEffectValue != 0f)
            {
                finalEffectValue += CalculatePercentValue(context, target, percentEffectValue,
                    useTargetAsValueSource, damageType, effectConfig.PercentValueSourceAttrName);
            }

            finalEffectValue += effectValue;

            IBuff buff;

            if (buffType == BuffType.MoonlightShield)
            {
                var shieldVfx = string.IsNullOrEmpty(effectConfig.ShieldVfx) ? "EliaSkillMoonshadowWard01" : effectConfig.ShieldVfx;
                var breakVfx = string.IsNullOrEmpty(effectConfig.BreakVfx) ? "EliaSkillMoonshadowWard02" : effectConfig.BreakVfx;
                int shieldNum = Mathf.RoundToInt(finalEffectValue);
                if (shieldNum <= 0) shieldNum = 1;

                buff = BuffFactory.CreateMoonlightShield(duration, shieldNum, finalEffectValue,
                    shieldVfx, breakVfx,
                    caster: context?.Caster, target: target as object, context: context);
            }
            else if (buffType == BuffType.RequiemChant)
            {
                buff = BuffFactory.CreateRequiemChant(duration, finalEffectValue,
                    "EliaSkillSoulComfort03",
                    caster: context?.Caster, target: target as object, context: context);
            }
            else if (buffType == BuffType.Attrbute || !string.IsNullOrEmpty(effectConfig.AttrTypeName))
            {
                if (!Mathf.Approximately(finalEffectValue, 0f)
                    && TryResolveAttributeType(effectConfig.AttrTypeName, out var attrType))
                {
                    var resolvedSkillId = skillCard != null ? context.SkillId : 0;
                    var attributeSourceKey = buffId > 0
                        ? buffId.ToString()
                        : (resolvedSkillId > 0 ? resolvedSkillId.ToString() : ((int)buffType).ToString());
                    buff = BuffFactory.CreateAttribute(buffType, attrType, duration,
                        finalEffectValue, effectConfig.AttrValueType, 0f, resolvedSkillId, tickInterval,
                        caster: context?.Caster, target: target as object, context: context,
                        sourceType: AttributeSourceType.Skill,
                        sourceKey: attributeSourceKey,
                        maxStack: Mathf.Max(1, effectConfig.Stack));
                }
                else if (Mathf.Approximately(finalEffectValue, 0f))
                {
                    buff = new VisualMarkerBuff(buffType, duration);
                }
                else
                {
                    LogTool.LogError(
                        "[BuffApplier] Attrbute Buff 缺少有效 AttrTypeName"
                        + ", buffId=" + buffId
                        + ", AttrTypeName=" + effectConfig.AttrTypeName);
                    return null;
                }
            }
            else
            {
                buff = BuffFactory.Create(buffType, duration, finalEffectValue,
                    tickInterval,
                    caster: context?.Caster, target: target as object, context: context,
                    maxStack: maxStack);
            }

            if (buff == null) return null;

            buff.BuffId = buffId > 0 ? buffId : effectConfig.BuffId;
            if (buff.Caster == null) buff.Caster = context?.Caster;
            if (buff.SkillContext == null) buff.SkillContext = context;
            if (!string.IsNullOrEmpty(effectConfig.BuffVfx)) buff.BuffVfx = effectConfig.BuffVfx;
            if (buff is BuffBase buffBase)
            {
                buffBase.DurationType = BuffDurationType.Duration;
                buffBase.DamageType = damageType;
                if (probability > 0f)
                    buffBase.Probability = probability;
            }

            return buff;
        }

        /// <summary>
        /// 创建AttachBuffHitModule专用的Buff（使用 effectConfig 中的配置）
        /// </summary>
        // public static IBuff CreateBuffForHitModule(
        //     BuffType buffType,
        //     SkillContext skillContext,
        //     MonsterCard hitTarget,
        //     IBuffable buffOwner,
        //     SkillBuffEffectData effectConfig,
        //     int skillId = 0,
        //     float cooldownTime = 0f)
        // {
        //     var skillCard = GetSkillCard(skillContext);
        //
        //     // 解析属性值：有AttrName读属性，没AttrName读常量
        //     float duration = ResolveFloat(skillCard, effectConfig.DurationAttrName, effectConfig.Duration);
        //     float effectValue = ResolveFloat(skillCard, effectConfig.EffectValueAttrName, effectConfig.EffectValue);
        //     float tickInterval = ResolveFloat(skillCard, effectConfig.TickIntervalAttrName, effectConfig.TickInterval);
        //     float probability = ResolveFloat(skillCard, effectConfig.ProbabilityAttrName, effectConfig.Probability);
        //
        //     var useTargetAsValueSource = effectConfig.BuffValueSource == "Target";
        //     var damageType = Enum.IsDefined(typeof(TakeDamageType), effectConfig.DamageType)
        //         ? (TakeDamageType)effectConfig.DamageType
        //         : TakeDamageType.General;
        //
        //     // 百分比效果值计算
        //     float percentEffectValue = ResolveFloat(skillCard, effectConfig.PercentEffectValueAttrName, effectConfig.PercentEffectValue);
        //     float finalEffectValue = effectValue;
        //
        //     if (percentEffectValue != 0f)
        //     {
        //         finalEffectValue += CalculatePercentValue(skillContext, hitTarget, percentEffectValue,
        //             useTargetAsValueSource, damageType, effectConfig.PercentValueSourceAttrName);
        //     }
        //
        //     if (!string.IsNullOrEmpty(effectConfig.PercentValueSourceAttrName))
        //     {
        //         finalEffectValue *= GetPercentValueSource(skillContext, hitTarget,
        //             effectConfig.PercentValueSourceAttrName, useTargetAsValueSource);
        //     }
        //
        //     IBuff buff;
        //
        //     if (buffType == BuffType.MoonlightShield)
        //     {
        //         var shieldVfx = string.IsNullOrEmpty(effectConfig.ShieldVfx) ? "EliaSkillMoonshadowWard01" : effectConfig.ShieldVfx;
        //         var breakVfx = string.IsNullOrEmpty(effectConfig.BreakVfx) ? "EliaSkillMoonshadowWard02" : effectConfig.BreakVfx;
        //         buff = BuffFactory.CreateMoonlightShield(duration,
        //             Mathf.Max(1, effectConfig.Stack), finalEffectValue,
        //             shieldVfx, breakVfx,
        //             caster: skillContext?.Caster, target: buffOwner as object, context: skillContext);
        //     }
        //     else if (buffType == BuffType.RequiemChant)
        //     {
        //         buff = BuffFactory.CreateRequiemChant(duration, finalEffectValue, "EliaSkillSoulComfort03",
        //             caster: skillContext?.Caster, target: buffOwner as object, context: skillContext);
        //     }
        //     else if (!string.IsNullOrEmpty(effectConfig.AttrTypeName))
        //     {
        //         var resolvedSkillId = skillId > 0 ? skillId : (skillContext?.SkillId ?? 0);
        //         if (Enum.TryParse<AttributeType>(effectConfig.AttrTypeName, out var attrType))
        //         {
        //             buff = BuffFactory.CreateAttribute(buffType, attrType,
        //                 duration, finalEffectValue, ValueType.Number, cooldownTime,
        //                 resolvedSkillId, tickInterval,
        //                 caster: skillContext?.Caster, target: buffOwner as object, context: skillContext,
        //                 sourceType: AttributeSourceType.Skill,
        //                 sourceKey: resolvedSkillId > 0 ? resolvedSkillId.ToString() : ((int)buffType).ToString(),
        //                 maxStack: Mathf.Max(1, effectConfig.Stack));
        //         }
        //         else
        //         {
        //             buff = BuffFactory.Create(buffType, duration, finalEffectValue, tickInterval,
        //                 caster: skillContext?.Caster, target: buffOwner as object, context: skillContext);
        //         }
        //     }
        //     else
        //     {
        //         buff = BuffFactory.Create(buffType, duration, finalEffectValue, tickInterval,
        //             caster: skillContext?.Caster, target: buffOwner as object, context: skillContext);
        //     }
        //
        //     if (buff == null) return null;
        //
        //     buff.BuffId = effectConfig.BuffId > 0 ? effectConfig.BuffId : (int)buffType;
        //     if (!string.IsNullOrEmpty(effectConfig.BuffVfx)) buff.BuffVfx = effectConfig.BuffVfx;
        //     if (buff is BuffBase buffBase)
        //     {
        //         buffBase.DurationType = BuffDurationType.Duration;
        //         if (probability > 0f)
        //             buffBase.Probability = probability;
        //     }
        //
        //     return buff;
        // }

        #endregion

        #region 属性解析（遵循：有AttrName读属性，没AttrName读常量）

        /// <summary>
        /// 解析伤害类型：优先从技能属性读取，其次使用编辑器常量
        /// </summary>
        public static TakeDamageType ResolveDamageType(ISkill skillCard, string attrName, int constantValue)
        {
            if (!string.IsNullOrEmpty(attrName) && skillCard != null &&
                Enum.TryParse<AttributeType>(attrName, out var attrType))
            {
                var resolved = (int)skillCard.Attributes.GetFinal(attrType);
                if (Enum.IsDefined(typeof(TakeDamageType), resolved))
                {
                    return (TakeDamageType)resolved;
                }
            }

            return Enum.IsDefined(typeof(TakeDamageType), constantValue)
                ? (TakeDamageType)constantValue
                : TakeDamageType.General;
        }

        /// <summary>
        /// 解析浮点型属性值：优先从技能属性读取，其次使用常量
        /// </summary>
        public static float ResolveFloat(ISkill skillCard, string attrName, float constantValue)
        {
            if (!string.IsNullOrEmpty(attrName) && skillCard != null &&
                Enum.TryParse<AttributeType>(attrName, out var attrType))
            {
                return skillCard.Attributes.GetFinal(attrType);
            }
            return constantValue;
        }

        /// <summary>
        /// 解析整型属性值：优先从技能属性读取，其次使用常量
        /// </summary>
        public static int ResolveInt(ISkill skillCard, string attrName, int constantValue)
        {
            if (!string.IsNullOrEmpty(attrName) && skillCard != null &&
                Enum.TryParse<AttributeType>(attrName, out var attrType))
            {
                return (int)skillCard.Attributes.GetFinal(attrType);
            }
            return constantValue;
        }

        /// <summary>
        /// 获取当前技能的 ISkill 实例；链式学习规则来源参数代理次之。
        /// </summary>
        public static ISkill GetSkillCard(SkillContext context)
        {
            var soldierSkill = context?.Caster?.GetComponent<SoldierCard>()?.GetSkillById(context.SkillId);
            if (soldierSkill != null)
            {
                return soldierSkill;
            }

            return context?.GetExtraData<ISkill>(SkillContextKeys.RuleSourceParamSkill);
        }

        #endregion

        #region 效果值计算

        /// <summary>
        /// 解析单条 Buff 模板效果的最终数值（固定值 + 百分比来源属性）。
        /// </summary>
        public static float ResolveEffectFinalValue(
            SkillBuffEffectData effectConfig,
            SkillContext context,
            IBuffable target)
        {
            if (effectConfig == null)
            {
                return 0f;
            }

            var skillCard = GetSkillCard(context);
            float effectValue = ResolveFloat(skillCard, effectConfig.EffectValueAttrName, effectConfig.EffectValue);
            float percentEffectValue = ResolveFloat(skillCard, effectConfig.PercentEffectValueAttrName, effectConfig.PercentEffectValue);
            var useTargetAsValueSource = effectConfig.BuffValueSource == "Target";
            var damageType = ResolveDamageType(skillCard, effectConfig.DamageTypeAttrName, effectConfig.DamageType);

            float finalEffectValue = effectValue;
            if (percentEffectValue != 0f)
            {
                finalEffectValue += CalculatePercentValue(
                    context,
                    target,
                    percentEffectValue,
                    useTargetAsValueSource,
                    damageType,
                    effectConfig.PercentValueSourceAttrName);
            }

            return finalEffectValue;
        }

        /// <summary>
        /// 计算百分比效果值：基础属性 × 百分比系数 × 伤害系数。
        /// 配置了 PercentValueSourceAttrName 时直接读取该属性；否则按伤害类型推导属性。
        /// 系数语义：|percentValue| &gt;= 1 视为百分数（÷100），否则视为小数比例（如 0.05 = 5%）。
        /// </summary>
        public static float CalculatePercentValue(
            SkillContext context,
            IBuffable buffTarget,
            float percentValue,
            bool useTargetAsValueSource,
            TakeDamageType damageType,
            string baseAttrName = null)
        {
            if (percentValue == 0f) return 0f;

            float baseValue;
            float damageRatio = 1f;

            if (!string.IsNullOrEmpty(baseAttrName))
            {
                if (!Enum.TryParse<AttributeType>(baseAttrName, out var attrType))
                {
                    LogTool.LogWarning($"[BuffApplier] 未知的属性名: {baseAttrName}");
                    return 0f;
                }

                baseValue = ReadAttributeValue(context, buffTarget, attrType, useTargetAsValueSource);
            }
            else
            {
                if (useTargetAsValueSource && buffTarget != null)
                {
                    baseValue = GetBuffTargetAttributeValue(buffTarget, GetDamageAttrType(damageType, true));
                }
                else if (context?.Caster != null)
                {
                    var soldierCard = context.Caster.GetComponent<SoldierCard>();
                    if (soldierCard != null)
                    {
                        baseValue = soldierCard.BaseAttributes.GetFinal(GetDamageAttrType(damageType, false));
                    }
                    else
                    {
                        baseValue = 0f;
                    }
                }
                else
                {
                    baseValue = 0f;
                }

                damageRatio = damageType == TakeDamageType.CriticalDamage
                    ? (useTargetAsValueSource ? 1f : 1.5f)
                    : 1f;
            }

            if (baseValue <= 0f) return 0f;

            // |v|>1 视为百分数（100=100%）；(0,1] 视为小数比例（0.5=50%，1=100%）
            float percentMultiplier = Mathf.Abs(percentValue) > 1f ? percentValue / 100f : percentValue;
            return baseValue * percentMultiplier * damageRatio;
        }

        /// <summary>
        /// 获取百分比数值来源属性值
        /// </summary>
        public static float GetPercentValueSource(
            SkillContext context,
            IBuffable buffTarget,
            string attrName,
            bool useTargetAsValueSource)
        {
            if (!Enum.TryParse<AttributeType>(attrName, out var attrType))
            {
                LogTool.LogWarning($"[BuffApplier] 未知的百分比数值来源属性名: {attrName}");
                return 0f;
            }

            return ReadAttributeValue(context, buffTarget, attrType, useTargetAsValueSource);
        }

        private static float ReadAttributeValue(
            SkillContext context,
            IBuffable buffTarget,
            AttributeType attrType,
            bool useTargetAsValueSource)
        {
            if (useTargetAsValueSource && buffTarget != null)
            {
                return GetBuffTargetAttributeValue(buffTarget, attrType);
            }

            if (context?.Caster != null)
            {
                var soldierCard = context.Caster.GetComponent<SoldierCard>();
                if (soldierCard != null)
                {
                    return soldierCard.BaseAttributes.GetFinal(attrType);
                }
            }

            if (buffTarget != null)
            {
                return GetBuffTargetAttributeValue(buffTarget, attrType);
            }

            return 0f;
        }

        /// <summary>
        /// 获取Buff目标属性值
        /// </summary>
        public static float GetBuffTargetAttributeValue(IBuffable buffTarget, AttributeType attrType)
        {
            if (buffTarget is MonsterCard monsterCard)
                return monsterCard.Attributes.GetFinal(attrType);

            if (buffTarget is SoldierCard soldierCard)
                return soldierCard.BaseAttributes.GetFinal(attrType);

            return 0f;
        }

        private static AttributeType GetDamageAttrType(TakeDamageType damageType, bool targetSide)
        {
            return damageType switch
            {
                TakeDamageType.PhysicalDamage => targetSide ? AttributeType.MonsterAttackPower : AttributeType.SoldierAttackPower,
                TakeDamageType.MagicDamage or TakeDamageType.MagicDamageThunder => targetSide ? AttributeType.MonsterSpellPower : AttributeType.SoldierSpellPower,
                TakeDamageType.CriticalDamage => targetSide ? AttributeType.MonsterAttackPower : AttributeType.SoldierCriticalDamage,
                TakeDamageType.RealDamage => AttributeType.SoldierAttackPower,
                _ => AttributeType.SoldierAttackPower,
            };
        }

        #endregion

        #region 命中概率

        /// <summary>
        /// 解析Buff命中概率（可从节点参数或技能属性读取）
        /// </summary>
        public static float ResolveBuffHitProbability(SkillNodeConfig node, ISkill skillCard)
        {
            var probability = Mathf.Clamp01(node.GetParamFloat("buff_hit_probability", 1f));
            var probabilityAttrName = node.GetParamString("buff_hit_probability_attr", "");

            if (!string.IsNullOrEmpty(probabilityAttrName) && skillCard != null &&
                Enum.TryParse<AttributeType>(probabilityAttrName, out var probAttr))
            {
                probability = Mathf.Clamp01(skillCard.Attributes.GetFinal(probAttr));
            }

            return probability;
        }

        #endregion

        #region 属性名标准化

        /// <summary>
        /// 标准化命中模块属性名（与编辑器逻辑一致）
        /// </summary>
        public static string NormalizeHitModuleAttrName(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            if (token == "0") return "";
            return token;
        }

        private static bool TryResolveAttributeType(string attrTypeName, out AttributeType attrType)
        {
            attrType = default;
            if (string.IsNullOrEmpty(attrTypeName))
            {
                return false;
            }

            if (Enum.TryParse(attrTypeName, out attrType))
            {
                return true;
            }

            switch (attrTypeName)
            {
                case "AttackSpeedNum":
                    attrType = AttributeType.SoldierNumOfAttackSpeed;
                    return true;
                case "AttackRealDagame":
                    attrType = AttributeType.AttackRealDamage;
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region 技能属性覆盖

        /// <summary>
        /// 从技能卡片属性覆盖命中模块参数
        /// </summary>
        public static void ApplySkillAttributeOverrides(ISkill skillCard,
            ref int intParam1, ref float floatParam1, ref float floatParam2,
            ref int intParam2, ref float floatParam3, string intParam3Attr,
            ref int intParam4, ref float floatParam4, ref float floatParam5,
            string intParam1Attr, string floatParam1Attr, string floatParam2Attr,
            string intParam2Attr, string floatParam3Attr, string intParam4Attr,
            string floatParam4Attr, string floatParam5Attr)
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

        #endregion
    }
}