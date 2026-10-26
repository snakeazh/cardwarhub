using System;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 灼烧 Buff（Soldier 目标专用）
    /// 单层伤害量 = Value + (施法者.SoldierSpellPower * 施法者.SkillSpellInheritance)
    /// 最终伤害量 = 单层伤害量 * Stack
    /// </summary>
    public class SoldierBurnBuff : BuffBase
    {
        /// <summary>每 tick 额外获得的法术威力加成（默认 0）</summary>
        protected float SpellPowerBonus { get; set; }

        public SoldierBurnBuff(float duration, float baseValue, ValueType valueType,BuffDurationType durationType = BuffDurationType.Duration,float tickInterval = 0f, int maxStack = 1)
            : base(BuffType.Burn, duration, baseValue, valueType,durationType,tickInterval)
        {
            MaxStack = maxStack;
            SpellPowerBonus = 0f;
        }

        public override void OnStackUp(object owner)
        {
            ResetTimer();
        }

        public override void OnTick(object owner, float dt)
        {
            base.OnTick(owner, dt);
        }

        public override void OnIntervalTick(object owner, float intervalAccumulator)
        {
            if (owner == null || IsFinished) return;

            // 单层基础伤害
            float singleLayerDamage = Value;

            // 施法者法术威力加成
            if (Caster is SoldierCard casterSoldier)
            {
                var spellPower = casterSoldier.BaseAttributes.GetFinal(AttributeType.SoldierSpellPower) *
                                 casterSoldier.BaseAttributes.GetFinal(AttributeType.SkillSpellInheritance);
                singleLayerDamage = Value + spellPower + SpellPowerBonus;
            }

            int dmg = Mathf.CeilToInt(singleLayerDamage * Stack);

            if (dmg <= 0) return;

            if (owner is MonsterCard monster)
            {
                monster.ReceiveDamage(MonsterDamageRequest.Single(
                    monster, DamageType, dmg, null, monster.transform.position,
                    pipelineType: DamagePipelineType.BuffSkillDamage));
                LogTool.L($"[SoldierBurnBuff] 灼烧伤害 → Monster: -{dmg} HP ({DamageType})");
            }
            else if (owner is SoldierCard soldier)
            {
                int dmgVal = dmg;
                if (soldier.CanReceiveIncomingDamage(dmgVal))
                {
                    var result = DamageContextBuilder.CalculateIncoming(soldier, null, DamageType, dmgVal);
                    dmgVal = soldier.TakeDamage(result);
                }
                LogTool.L($"[SoldierBurnBuff] 灼烧伤害 → Soldier: -{dmgVal} HP ({DamageType})");
            }
        }
    }
}
