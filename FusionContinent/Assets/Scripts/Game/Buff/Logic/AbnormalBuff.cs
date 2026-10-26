using System;
using Config;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 怪物异常状态 Buff 基类
    /// OnAdd → MonsterAbnormal.Apply() 应用状态
    /// OnTick → 累加计时器
    /// OnRemove → MonsterAbnormal.Remove() 清理状态
    /// </summary>
    public abstract class AbnormalBuff : BuffBase
    {
        /// <summary>对应的异常状态类型</summary>
        protected UnusualStatusType AbnormalType { get; }

        protected AbnormalBuff(BuffType type, UnusualStatusType abnormalType, float duration, float value, ValueType valueType,BuffDurationType durationType = BuffDurationType.Duration,float tickInterval = 0f)
            : base(type, duration, value, valueType,durationType,tickInterval)
        {
            AbnormalType = abnormalType;
        }

        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            if (owner is not MonsterCard monster)
            {
                return;
            }

            var abnormal = monster.monsterData != null
                ? monster.monsterData.monsterAbnormal
                : null;
            if (abnormal == null)
            {
                abnormal = monster.GetComponent<MonsterAbnormal>();
            }

            if (abnormal == null)
            {
                LogTool.LogError(
                    "[AbnormalBuff] MonsterAbnormal missing, cannot apply "
                    + AbnormalType
                    + ", BuffType=" + Type
                    + ", MonsterId=" + monster._cardId);
                return;
            }

            abnormal.Apply(AbnormalType, Value, ValueType);
        }

        public override void OnRemove(object owner)
        {
            if (owner is MonsterCard monster)
            {
                var abnormal = monster.GetComponent<MonsterAbnormal>();
                abnormal?.Remove(AbnormalType);
            }

            // 回收 BuffVfx（如 buff_zhuoshao_01），AbnormalBuff 原先未调用导致特效残留
            base.OnRemove(owner);
        }
    }

    /// <summary>眩晕（冻结）</summary>
    public class VertigoAbnormalBuff : AbnormalBuff
    {
        public VertigoAbnormalBuff(float duration)
            : base(BuffType.Vertigo, UnusualStatusType.Vertigo, duration, 0, ValueType.Number) { }
    }

    /// <summary>减速</summary>
    public class SlowAbnormalBuff : AbnormalBuff
    {
        public SlowAbnormalBuff(float duration, float value, ValueType valueType)
            : base(BuffType.Moderate, UnusualStatusType.Moderate, duration, value, valueType) { }
    }

    /// <summary>灼烧（间隔触发）</summary>
    public class BurnAbnormalBuff : AbnormalBuff
    {
        /// <summary>灼烧触发间隔（秒）</summary>
        private MonsterCard m_Monster;
        private int m_FireDamage;

        public BurnAbnormalBuff(float duration, float value, ValueType valueType, float tickInterval, int maxStack = 1)
            : base(BuffType.Burn, UnusualStatusType.Burn, duration, value, valueType,tickInterval:tickInterval)
        {
            MaxStack = maxStack;
        }

        public override void OnStackUp(object owner)
        {
            ResetTimer();
        }

        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            m_Monster = owner as MonsterCard;
            if (m_Monster != null)
            {
                m_FireDamage = (int)Value;
            }
        }

        public override void OnIntervalTick(object owner, float intervalAccumulator)
        {
            if (m_Monster == null || m_FireDamage <= 0 || m_Monster.hp <= 0)
                return;

            if (m_Monster.monsterData.monsterState == MonsterState.die || !m_Monster.isActiveAndEnabled)
                return;

            int damage = m_FireDamage * Stack;
            m_Monster.ReceiveDamage(MonsterDamageRequest.Single(
                m_Monster, DamageType, damage, null, m_Monster.transform.position,
                pipelineType: DamagePipelineType.BuffSkillDamage));
        }

        public override void OnRemove(object owner)
        {
            base.OnRemove(owner);
            m_Monster = null;
            m_FireDamage = 0;
        }
    }

    /// <summary>易损（受伤加深）</summary>
    public class VulnerableAbnormalBuff : AbnormalBuff
    {
        public VulnerableAbnormalBuff(float duration, float value, ValueType valueType)
            : base(BuffType.Vulnerable, UnusualStatusType.Vulnerable, duration, value, valueType) { }
    }

    /// <summary>无敌</summary>
    public class InvincibleAbnormalBuff : AbnormalBuff
    {
        public InvincibleAbnormalBuff(float duration)
            : base(BuffType.Invincible, UnusualStatusType.Invincible, duration, 0, ValueType.Number) { }
    }

    /// <summary>缴械（禁止普攻）</summary>
    public class DisarmAbnormalBuff : AbnormalBuff
    {
        public DisarmAbnormalBuff(float duration)
            : base(BuffType.Disarm, UnusualStatusType.Disarm, duration, 0, ValueType.Number) { }
    }

    /// <summary>加速</summary>
    public class AccelerateAbnormalBuff : AbnormalBuff
    {
        public AccelerateAbnormalBuff(float duration, float value, ValueType valueType)
            : base(BuffType.Accelerate, UnusualStatusType.Accelerate, duration, value, valueType) { }
    }

    /// <summary>击退</summary>
    public class KnockBackAbnormalBuff : AbnormalBuff
    {
        public KnockBackAbnormalBuff(float duration, float value, ValueType valueType)
            : base(BuffType.KnockBack, UnusualStatusType.KnockBack, duration, value, valueType) { }
    }
}
