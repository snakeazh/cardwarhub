using System;
using Config;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 治疗/伤害 Buff
    /// - Soldier: 按 TickInterval 回复血量（TickInterval &lt;= 0 时每帧回复）
    /// - Monster: 每次触发时造成伤害（扣血）
    /// 治疗/伤害量 = Value（编辑器传入的 EffectValue）
    /// </summary>
    public class HealBuff : BuffBase
    {
        private MonsterCard _monster;

        public HealBuff(float duration, float baseValue, ValueType valueType, object caster = null)
            : base(BuffType.Health, duration, baseValue, valueType)
        {
            Caster = caster;
        }

        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            _monster = owner as MonsterCard;
        }

        public override void OnTick(object owner, float dt)
        {
            base.OnTick(owner, dt);

            if (TickInterval > 0f)
            {
                return;
            }

            ApplyHealOrDamage(owner);
        }

        public override void OnIntervalTick(object owner, float intervalAccumulator)
        {
            base.OnIntervalTick(owner, intervalAccumulator);
            ApplyHealOrDamage(owner);
        }

        private void ApplyHealOrDamage(object owner)
        {
            if (_monster != null)
            {
                OnMonsterTick();
            }
            else if (owner is SoldierCard soldier)
            {
                OnSoldierTick(soldier);
            }
        }

        private void OnMonsterTick()
        {
            if (_monster == null || _monster.hp <= 0) return;

            int damage = ResolveEffectAmount();
            if (damage <= 0) return;

            _monster.ReceiveDamage(MonsterDamageRequest.Single(
                _monster, DamageType, damage, null, _monster.transform.position,
                pipelineType: DamagePipelineType.BuffSkillDamage));
            LogTool.L($"[HealBuff] Monster dealt {damage} damage (Value={Value})");
        }

        private void OnSoldierTick(SoldierCard soldier)
        {
            int healVal = ResolveEffectAmount();
            if (healVal <= 0) return;

            soldier.SetSoldierHp(healVal);
            LogTool.L($"[HealBuff] Soldier healed +{healVal} HP (Value={Value})");
        }

        private int ResolveEffectAmount()
        {
            return Mathf.Max(0, Mathf.RoundToInt(Value));
        }

        public override void OnRemove(object owner)
        {
            base.OnRemove(owner);
            _monster = null;
        }
    }
}
