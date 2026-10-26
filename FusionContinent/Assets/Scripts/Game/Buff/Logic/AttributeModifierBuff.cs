using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using Manager;
using UnityEngine;
using Unity.Profiling;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 属性修正 Buff
    /// OnAdd → 添加属性加成（OnRemove 时移除）
    /// 用于 SkillGraph ApplyBuff 节点驱动任意属性增益/减益
    /// </summary>
    public class AttributeModifierBuff : BuffBase
    {
        private readonly AttributeType _attrType;
        private readonly string _sourceKey;
        private readonly AttributeSourceType _sourceType;

        /// <summary>
        /// 是否为百分比加成（true=百分比，false=固定值）
        /// </summary>
        private readonly bool _isPercent;

        /// <summary>
        /// 效果系数（当 ValueType=Percentage 时表示倍率，当 ValueType=Number 时表示固定值）
        /// </summary>
        private readonly float _effectCoeff;

        /// <summary>
        /// 冷却时间（秒），用于注册 / 移除技能冷却
        /// </summary>
        private readonly float _cooldownTime;

        /// <summary>
        /// 施法者技能ID，用于冷却回调
        /// </summary>
        private readonly int _skillId;

        public AttributeModifierBuff(BuffType type, AttributeType attrType, float duration,
            float effectValue, ValueType valueType, float cooldownTime = 0f, int skillId = 0,
            AttributeSourceType sourceType = AttributeSourceType.Skill, string sourceKey = null, int maxStack = 1)
            : base(type, duration, effectValue, valueType)
        {
            _attrType = attrType;
            _isPercent = valueType == ValueType.Percentage;
            _effectCoeff = effectValue;
            _cooldownTime = cooldownTime;
            _skillId = skillId;
            _sourceType = sourceType;
            _sourceKey = !string.IsNullOrEmpty(sourceKey)
                ? sourceKey
                : (skillId > 0 ? skillId.ToString() : "AttributeModifierBuff");
            MaxStack = maxStack;
        }

        /// <summary>
        /// 属性 Modifier 的 SourceKey：与 OnAdd/OnRemove 保持一致，优先外部 BuffId。
        /// </summary>
        private string AttributeSourceKey => BuffId > 0 ? BuffId.ToString() : _sourceKey;

        public override void OnAdd(object owner)
        {
           
            if (owner == null) return;
                base.OnAdd(owner);
            Dictionary<AttributeType, (float flat, float percent)> effects;
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTempList))
            {
                effects = new Dictionary<AttributeType, (float flat, float percent)>
                {
                    [_attrType] = (_isPercent ? 0f : _effectCoeff, _isPercent ? _effectCoeff : 0f)
                };
            }

            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerAttributeModify))
            {
            if (owner is SoldierCard soldier)
            {
                soldier.BaseAttributes.ApplyBuff(AttributeSourceKey, effects, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Add属性 → type={_attrType}, value={_effectCoeff}, ownerId={soldier.cardId}, sourceType={_sourceType}, sourceKey={_sourceKey}");

                if (_skillId > 0 && _cooldownTime > 0)
                {
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerCounterUpdate))
                    {
                        GameLand.Default?.ReleaseSkillCallBack(_skillId, _cooldownTime);
                    }
                }
            }
            else if (owner is MonsterCard monster)
            {
                monster.Attributes.ApplyBuff(BuffId.ToString(), effects, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Add属性 → type={_attrType}, value={_effectCoeff}, monster={monster.uniqueId}, sourceType={_sourceType}, sourceKey={_sourceKey}");
            }
            }
        }

        public override void OnStackUp(object owner)
        {
            base.OnStackUp(owner);
            //又叠加了
            var effects = new System.Collections.Generic.Dictionary<AttributeType, (float flat, float percent)>
            {
                [_attrType] = (_isPercent ? 0f : _effectCoeff, _isPercent ? _effectCoeff : 0f)
            };

            if (owner is SoldierCard soldier)
            {
                soldier.BaseAttributes.ApplyBuff(AttributeSourceKey, effects, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Add属性 → type={_attrType}, value={_effectCoeff}, ownerId={soldier.cardId}, sourceType={_sourceType}, sourceKey={AttributeSourceKey}");
                LogTool.L($"当前这个最终的属性值为 {_attrType}: {soldier.BaseAttributes.GetFinal(_attrType)}");  //当前这个最终的属性值为
                if (_skillId > 0 && _cooldownTime > 0)
                    GameLand.Default?.ReleaseSkillCallBack(_skillId, _cooldownTime);
            }
            else if (owner is MonsterCard monster)
            {
                monster.Attributes.ApplyBuff(AttributeSourceKey, effects, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Add属性 → type={_attrType}, value={_effectCoeff}, monster={monster.uniqueId}, sourceType={_sourceType}, sourceKey={AttributeSourceKey}");
            }
            
            // 特殊处理 层数满的时候
            if (BuffId == 10001 && Stack == MaxStack)
            {
                ISkill skillCard = null;
                if (SkillContext.Caster != null)
                {
                    var soldierCard = SkillContext.Caster.GetComponent<SoldierCard>();
                    if (soldierCard != null)
                    {
                        skillCard = soldierCard.GetSkillById(SkillContext.SkillId);
                    }
                }

                if (skillCard != null)
                {
                    if (skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierAttackPower) != 0)
                    {
                        Dictionary<AttributeType, (float flat, float percent)> specialEffects = new Dictionary<AttributeType, (float flat, float percent)>();
                        specialEffects.Add(AttributeType.SoldierAttackPower,(100000,0));
                        skillCard.Parent.BaseAttributes.ApplyBuff(BuffId.ToString(), specialEffects, AttributeSourceType.Skill);
                    }
                }
            }

            LogTool.L($"[BuffController] Buff添加 → type={Type}, buffId={BuffId}, stack={Stack}");
        }

        public override void OnRemove(object owner)
        {
            if (owner == null) return;
            base.OnRemove(owner);
            if (owner is SoldierCard soldier)
            {
                soldier.BaseAttributes.RemoveBuff(AttributeSourceKey, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Remove属性 → type={_attrType}, ownerId={soldier.cardId}, sourceType={_sourceType}, sourceKey={AttributeSourceKey}");
            }
            else if (owner is MonsterCard monster)
            {
                monster.Attributes.RemoveBuff(AttributeSourceKey, _sourceType);
                LogTool.L($"[AttributeModifierBuff] Remove属性 → type={_attrType}, monster={monster.uniqueId}, sourceType={_sourceType}, sourceKey={AttributeSourceKey}");
            }
        }

    }
}
