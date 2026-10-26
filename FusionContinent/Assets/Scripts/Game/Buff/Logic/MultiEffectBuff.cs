using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Skill.Handler;
using Manager;
using Unity.Profiling;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 聚合型 Buff：一个 Buff 实例管理多个效果
    /// 例如：战神祝福 = [攻击+10%, 血量+20%]
    /// 直接从 BuffEffectsConfig 读取多条效果记录并统一应用
    /// </summary>
    public class MultiEffectBuff : BuffBase
    {
        /// <summary>效果数据列表</summary>
        private readonly List<BuffEffectData> _effectDataList = new List<BuffEffectData>();

        /// <summary>源技能 ID（用于 SourceKey 判重）</summary>
        private readonly int _skillId;

        /// <summary>施法者（用于 SourceKey 判重）</summary>
        private readonly object _caster;

        /// <summary>SourceKey（BuffId + SkillId + CasterHash）</summary>
        private readonly string _sourceKey;

        /// <summary>获取唯一标识</summary>
        public string SourceKey => _sourceKey;

        /// <summary>
        /// 创建聚合 Buff
        /// </summary>
        /// <param name="buffType">Buff 类型</param>
        /// <param name="buffId">Buff 配置 ID</param>
        /// <param name="duration">持续时间</param>
        /// <param name="maxStack">最大层数</param>
        /// <param name="effects">效果列表（BuffEffectData）</param>
        /// <param name="skillId">技能 ID（用于判重）</param>
        /// <param name="caster">施法者（用于判重）</param>
        /// <param name="tickInterval">触发间隔</param>
        /// <param name="context">技能上下文</param>
        /// <param name="sourceType">来源类型</param>
        /// <param name="sourceKey">自定义来源键</param>
        public MultiEffectBuff(
            BuffType buffType,
            int buffId,
            float duration,
            int maxStack,
            List<BuffEffectData> effects,
            int skillId = 0,
            object caster = null,
            float tickInterval = 0f,
            SkillContext context = null,
            AttributeSourceType sourceType = AttributeSourceType.Skill,
            string sourceKey = null)
            : base(buffType, duration, 0f, ValueType.Number)
        {
            BuffId = buffId;
            MaxStack = maxStack;
            TickInterval = tickInterval;
            SkillContext = context;
            Caster = caster;
            _skillId = skillId;
            _caster = caster;

            // 生成 SourceKey：BuffId_SkillId_CasterHash
            _sourceKey = !string.IsNullOrEmpty(sourceKey)
                ? sourceKey
                : $"{buffId}_{skillId}_{caster?.GetHashCode() ?? 0}";

            // 构建效果数据列表
            if (effects != null)
            {
                _effectDataList.AddRange(effects);
            }
        }

        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            ApplyEffects(owner, apply: true);
            LogTool.L($"[MultiEffectBuff] OnAdd: buffId={BuffId}, sourceKey={_sourceKey}, effects={_effectDataList.Count}, stack={Stack}");
        }

        public override void OnStackUp(object owner)
        {
            base.OnStackUp(owner);
            ApplyEffects(owner, apply: true);
            LogTool.L($"[MultiEffectBuff] OnStackUp: buffId={BuffId}, stack={Stack}");
        }

        public override void OnRefreshDuration(object owner)
        {
            base.OnRefreshDuration(owner);
            ResetTimer();
            LogTool.L($"[MultiEffectBuff] OnRefreshDuration: buffId={BuffId}");
        }

        public override void OnRemove(object owner)
        {
            base.OnRemove(owner);
            ApplyEffects(owner, apply: false);
            _effectDataList.Clear();
            LogTool.L($"[MultiEffectBuff] OnRemove: buffId={BuffId}, sourceKey={_sourceKey}");
        }

        /// <summary>
        /// 应用或移除所有效果
        /// </summary>
        private void ApplyEffects(object owner, bool apply)
        {
            foreach (var effect in _effectDataList)
            {
                var flatValue = effect.Value;
                var percentValue = effect.PercentValue;

                Dictionary<AttributeType, (float flat, float percent)> effects;
                using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTempList))
                {
                    effects = new Dictionary<AttributeType, (float flat, float percent)>
                    {
                        [effect.AttributeType] = (flatValue, percentValue)
                    };
                }

                if (owner is SoldierCard soldier)
                {
                    bool applyToBase;
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerRuleCheck))
                    {
                        applyToBase = ShouldApplyToSoldierBaseAttributes();
                    }

                    if (applyToBase)
                    {
                        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerAttributeModify))
                        {
                            if (apply)
                            {
                                soldier.BaseAttributes.ApplyBuff(_sourceKey, effects, AttributeSourceType.Skill);
                                LogSoldierAttributeEffect(soldier, effect.AttributeType, apply);
                            }
                            else
                            {
                                soldier.BaseAttributes.RemoveBuff(_sourceKey, AttributeSourceType.Skill);
                            }
                        }

                        continue;
                    }

                    Config.SpecialEffectsConfig specialCfg;
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerRuleCheck))
                    {
                        specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(_skillId);
                    }
                    if (specialCfg == null)
                    {
                        LogTool.LogWarning(
                            $"[MultiEffectBuff] 技能配置不存在 skillId={_skillId}, buffId={BuffId}, 跳过被动卡牌分支");
                        continue;
                    }

                    // 被动生效的卡牌
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerAttributeModify))
                    {
                    foreach (var cardId in specialCfg.PassiveActsOnCard)
                    {
                        var skillCard = soldier.GetSkillById(cardId);
                        if (skillCard == null) continue;
                        if (apply)
                            skillCard.Attributes.ApplyBuff(_sourceKey, effects, AttributeSourceType.Skill);
                        else
                            skillCard.Attributes.RemoveBuff(_sourceKey, AttributeSourceType.Skill);
                    }
                    }
                }
                else if (owner is MonsterCard monster)
                {
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerAttributeModify))
                    {
                        if (apply)
                            monster.Attributes.ApplyBuff(_sourceKey, effects, AttributeSourceType.Skill);
                        else
                            monster.Attributes.RemoveBuff(_sourceKey, AttributeSourceType.Skill);
                    }
                }
            }
        }

        /// <summary>
        /// 规则/非技能来源 Buff（skillId=0）或无 PassiveActsOnCard 时，直接改英灵 BaseAttributes。
        /// </summary>
        private bool ShouldApplyToSoldierBaseAttributes()
        {
            if (_skillId <= 0)
            {
                return true;
            }

            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(_skillId);
            if (specialCfg == null)
            {
                return true;
            }

            return specialCfg.PassiveActsOnCard.Count == 0;
        }

        private void LogSoldierAttributeEffect(SoldierCard soldier, AttributeType attrType, bool apply)
        {
            if (attrType != AttributeType.SoldierNumOfAttackSpeed
                && attrType != AttributeType.SoldierNumOfCritical)
            {
                return;
            }

            var numOfAtkSpeed = soldier.BaseAttributes.GetAttr(AttributeType.SoldierNumOfAttackSpeed).SumFinalValue();
            var atkSpeed = soldier.BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
            LogTool.Log(
                $"[MultiEffectBuff] {(apply ? "Apply" : "Remove")} buffId={BuffId}, hero={soldier.cardId}, attr={attrType}, NumOfAttackSpeed={numOfAtkSpeed:F3}, AttackSpeed={atkSpeed:F3}, sourceKey={_sourceKey}");
        }
    }
}
