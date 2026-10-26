using System;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Utils;
using UnityEngine;

/// <summary>
/// 怪物伤害计算器 - 处理MonsterCard的受击计算逻辑（仿照SoldierDamageCalculator）
/// </summary>
public class MonsterDamageCalculator
{
    private readonly MonsterCard _monster;
    private IDamageCalculationRecorder _recorder;

    public MonsterDamageCalculator(MonsterCard monster, IDamageCalculationRecorder recorder = null)
    {
        _monster = monster;
        _recorder = recorder ?? new NullDamageCalculationRecorder();
    }
    /// <summary>
    /// 获取记录报告
    /// </summary>
    public string GetRecordReport()
    {
        return _recorder.GetRecordReport();
    }
    public void SetRecorder(IDamageCalculationRecorder recorder)
    {
        _recorder = recorder ?? new NullDamageCalculationRecorder();
    }

    public class DamageCalculationResult
    {
        public int RawDamage { get; set; }
        public int FinalDamage { get; set; }
        public bool IsDodged { get; set; }
        public bool IsInvincible { get; set; }
        public bool IsExecution { get; set; }
        public bool IsSoulFlawsBreak { get; set; }
        public bool IsCritical { get; set; }
        public int CriticalDamage { get; set; }
        public int ThunderboltDamage { get; set; }
        public float ToughnessDamage { get; set; }
        public bool ShouldApplyToughnessDamage { get; set; }
        public TakeDamageType DamageType { get; set; }
        public float ExecutionThreshold { get; set; }
    }

    /// <summary>
    /// 根据Combo层数获取削韧倍率
    /// </summary>
    private float GetToughnessComboMultiplier(int combo)
    {
        if (combo >= 6) return 5f;
        if (combo >= 1) return 1f + 0.5f * (combo - 1);
        return 1f;
    }

    /// <summary>
    /// 计算怪物最终伤害（不含护盾/血量扣除）
    /// </summary>
    public DamageCalculationResult CalculateDamage(TakeDamageType damageType, int damage, ISkill skillCard, bool isAgainDamage)
    {
        var combo = Global.ConsecutiveSkillReleaseCount.Value;
        combo = Mathf.Clamp(combo, 0, 6);
        var result = new DamageCalculationResult
        {
            RawDamage = damage,
            FinalDamage = damage,
            DamageType = damageType
        };

        _recorder.BeginRecord(_monster?.name ?? "Monster", damageType, damage);

        // 无敌状态直接免伤
        var abnormal = _monster.GetComponent<MonsterAbnormal>();
        if (abnormal != null && abnormal.invincible)
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "无敌状态",
                InputValue = damage,
                OutputValue = 0,
                IsApplied = true,
                AttributeDescription = "Invincible",
                UsedValue = 1,
                Description = "怪物处于无敌状态"
            });

            result.IsInvincible = true;
            result.FinalDamage = 0;
            _recorder.EndRecord(0, false);
            return result;
        }

        // 1. 易伤倍率（vulnerabilitydamage）
        if (abnormal != null && Math.Abs(abnormal.vulnerabilitydamage - 1f) > 0.0001f)
        {
            var newDamage = Mathf.CeilToInt(damage * abnormal.vulnerabilitydamage);
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "易伤倍率",
                InputValue = damage,
                OutputValue = newDamage,
                IsApplied = true,
                AttributeDescription = "vulnerabilitydamage",
                UsedValue = abnormal.vulnerabilitydamage,
                Description = "怪物易伤倍率修正"
            });
            damage = newDamage;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "易伤倍率",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "vulnerabilitydamage",
                UsedValue = abnormal == null ? 1 : abnormal.vulnerabilitydamage,
                Description = "无易伤修正"
            });
        }

        // 2. 斩杀判定（普通怪）
        var executionThreshold = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.MonsterExecutionThreshold);
        var residueHpPer = _monster.maxHp > 0 ? (float)_monster.hp / _monster.maxHp : 0f;
        if (_monster._monsterConfig.MonsterQuality == MonsterQuality.Ordinary
            && _monster.hp > 0 && executionThreshold > 0 && executionThreshold >= residueHpPer)
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "斩杀判定",
                InputValue = damage,
                OutputValue = 0,
                IsApplied = true,
                AttributeDescription = "MonsterExecutionThreshold",
                UsedValue = executionThreshold,
                Description = "满足斩杀阈值"
            });

            result.IsExecution = true;
            result.ExecutionThreshold = executionThreshold;
            result.FinalDamage = 0;
            _recorder.EndRecord(0, false);
            return result;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "斩杀判定",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "MonsterExecutionThreshold",
                UsedValue = executionThreshold,
                Description = "未触发斩杀"
            });
        }

        // 3. 敌我血量百分比额外伤害
        var enemyHpPer = skillCard?.Parent != null && skillCard.Parent.CurrentHp > 0
            ? (float)_monster.hp / skillCard.Parent.CurrentHp
            : 0f;

        var extraDamagePer = skillCard?.Parent?.BaseAttributes.GetFinal(AttributeType.EnemyHealthAbovePerExtraAddDamage,
            (effectParam) => enemyHpPer >= effectParam) ?? 0f;
        if (extraDamagePer > 0f)
        {
            var newDamage = Mathf.CeilToInt(damage * extraDamagePer);
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "敌方高血量增伤",
                InputValue = damage,
                OutputValue = newDamage,
                IsApplied = true,
                AttributeDescription = "EnemyHealthAbovePerExtraAddDamage",
                UsedValue = extraDamagePer,
                Description = $"敌方血量占比{enemyHpPer:F2}触发"
            });
            damage = newDamage;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "敌方高血量增伤",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "EnemyHealthAbovePerExtraAddDamage",
                UsedValue = extraDamagePer,
                Description = "未触发"
            });
        }

        var extraBelowDamagePer = skillCard?.Parent?.BaseAttributes.GetFinal(AttributeType.EnemyHealthBelowPerExtraAddDamage,
            (effectParam) => enemyHpPer <= effectParam) ?? 0f;
        if (extraBelowDamagePer > 0f)
        {
            var newDamage = Mathf.CeilToInt(damage * extraBelowDamagePer);
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "敌方低血量增伤",
                InputValue = damage,
                OutputValue = newDamage,
                IsApplied = true,
                AttributeDescription = "EnemyHealthBelowPerExtraAddDamage",
                UsedValue = extraBelowDamagePer,
                Description = $"敌方血量占比{enemyHpPer:F2}触发"
            });
            damage = newDamage;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "敌方低血量增伤",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "EnemyHealthBelowPerExtraAddDamage",
                UsedValue = extraBelowDamagePer,
                Description = "未触发"
            });
        }

        // 4. 暴击伤害
        var criticalDamage = 0;
        var criticalDamagePercentage = skillCard?.GetCriticalDamage() ?? 0f;
        if (criticalDamagePercentage > 0f)
        {
            criticalDamage = Mathf.CeilToInt(damage * criticalDamagePercentage);
            var newDamage = damage + criticalDamage;
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "暴击伤害",
                InputValue = damage,
                OutputValue = newDamage,
                IsApplied = true,
                AttributeDescription = "CriticalDamage",
                UsedValue = criticalDamagePercentage,
                Description = "暴击伤害加成"
            });
            damage = newDamage;
            result.IsCritical = true;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "暴击伤害",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "CriticalDamage",
                UsedValue = 0,
                Description = "未触发暴击"
            });
        }

        // 技能暴击伤害（直接覆盖）
        var skillCriticalDamage = skillCard?.Attributes.GetFinal(AttributeType.SkillCriticalDamage) ?? 0f;
        if (skillCriticalDamage > 0f)
        {
            var newDamage = (int)skillCriticalDamage;
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "技能暴击伤害",
                InputValue = damage,
                OutputValue = newDamage,
                IsApplied = true,
                AttributeDescription = "SkillCriticalDamage",
                UsedValue = skillCriticalDamage,
                Description = "技能暴击伤害覆盖"
            });
            damage = newDamage;
            criticalDamage = newDamage;
            result.IsCritical = true;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "技能暴击伤害",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "SkillCriticalDamage",
                UsedValue = skillCriticalDamage,
                Description = "无技能暴击覆盖"
            });
        }

        result.CriticalDamage = criticalDamage;

        // 5. 破绽判定
        var soulFlaws = skillCard?.Attributes.GetFinal(AttributeType.SoldierSoulFlaws) ?? 0f;
        if (soulFlaws > 0f)
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "灵魂破绽",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = true,
                AttributeDescription = "SoldierSoulFlaws",
                UsedValue = soulFlaws,
                Description = "触发破绽，直接破盾"
            });
            result.IsSoulFlawsBreak = true;
            result.FinalDamage = damage;
            _recorder.EndRecord(damage, false);
            return result;
        }
        else
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "灵魂破绽",
                InputValue = damage,
                OutputValue = damage,
                IsApplied = false,
                AttributeDescription = "SoldierSoulFlaws",
                UsedValue = soulFlaws,
                Description = "未触发破绽"
            });
        }

        // 6. 闪避判定
        var missBase = DataConfigManager.Tables.TbGameBattleConst.MissRateSumBase;
        var missRate = 1 - ((float)missBase / (missBase + _monster.Attributes.GetFinal(AttributeType.MonsterMissNum)));
        if (RandomTool.CheckRandomByTarget((int)(missRate * 100)))
        {
            _recorder.RecordStep(new DamageCalculationStep
            {
                StepName = "闪避判定",
                InputValue = damage,
                OutputValue = 0,
                IsApplied = true,
                AttributeDescription = "MonsterMissNum",
                UsedValue = _monster.Attributes.GetFinal(AttributeType.MonsterMissNum),
                Description = "怪物闪避成功"
            });

            result.IsDodged = true;
            result.FinalDamage = 0;
            _recorder.EndRecord(0, true);
            return result;
        }

        _recorder.RecordStep(new DamageCalculationStep
        {
            StepName = "闪避判定",
            InputValue = damage,
            OutputValue = damage,
            IsApplied = false,
            AttributeDescription = "MonsterMissNum",
            UsedValue = _monster.Attributes.GetFinal(AttributeType.MonsterMissNum),
            Description = "未触发闪避"
        });

        // 7. 抗性与穿透
        var monsterAttribute = _monster.MonsterAttribute;
        var damageAfterResistance = monsterAttribute.TakeDamage(damageType, damage, _monster, skillCard);
        _recorder.RecordStep(new DamageCalculationStep
        {
            StepName = "抗性减伤",
            InputValue = damage,
            OutputValue = damageAfterResistance,
            IsApplied = true,
            AttributeDescription = "Resistance",
            UsedValue = 1,
            Description = "怪物抗性与穿透计算"
        });
        damage = damageAfterResistance;

        // 8. 主属性易伤
        var mainAttrType = skillCard?.Parent?.MainAttrType ?? MainAttributeType.Strength;
        var mainAttrDamage = monsterAttribute.TakeMainAttributeDamage(mainAttrType, damage, _monster, skillCard);
        _recorder.RecordStep(new DamageCalculationStep
        {
            StepName = "主属性易伤",
            InputValue = damage,
            OutputValue = mainAttrDamage,
            IsApplied = true,
            AttributeDescription = "MainAttribute",
            UsedValue = (float)mainAttrType,
            Description = "主属性修正"
        });
        damage = mainAttrDamage;

        {
            //连击伤害计算
            if (combo > 1)
            {
                damage = Mathf.CeilToInt(
                    (1 + (DataConfigManager.Tables.TbGameBattleConst.ComboDamageRatio) * (combo - 1)) * damage);
            }
        }
        
        // 9. 怪物类型易伤
        var typeDamage = monsterAttribute.TakeMonsterTypeDamage(_monster._monsterConfig.MonsterQuality, damage, skillCard.Parent);
        _recorder.RecordStep(new DamageCalculationStep
        {
            StepName = "怪物类型修正",
            InputValue = damage,
            OutputValue = typeDamage,
            IsApplied = true,
            AttributeDescription = "MonsterQuality",
            UsedValue = (float)_monster._monsterConfig.MonsterQuality,
            Description = "普通/精英/Boss伤害修正"
        });
        damage = typeDamage;

        // 10. 技能削韧
        if (_monster.monsterData.monsterState != MonsterState.weak)
        {
            var baseToughnessDamage = (!isAgainDamage && skillCard != null)
                ? skillCard.Attributes.GetFinal(AttributeType.SkillToughnessDamage)
                : 0f;
            var toughnessComboMultiplier = GetToughnessComboMultiplier(combo);
            var toughnessDamage = baseToughnessDamage > 0f ? baseToughnessDamage * toughnessComboMultiplier : 0f;

            result.ToughnessDamage = toughnessDamage;
            result.ShouldApplyToughnessDamage = toughnessDamage > 0f;
            _recorder.RecordStep(new DamageCalculationStep

            {
                StepName = "技能削韧",
                InputValue = Mathf.CeilToInt(baseToughnessDamage),
                OutputValue = Mathf.CeilToInt(toughnessDamage),
                IsApplied = result.ShouldApplyToughnessDamage,
                AttributeDescription = "SkillToughnessDamage",
                UsedValue = toughnessDamage,
                Description = result.ShouldApplyToughnessDamage
                    ? $"Combo倍率: {toughnessComboMultiplier:F1}x (Combo={combo}), 怪物当前韧性值: {_monster.monsterData.CurrentResilience}"
                    : "未触发削韧"
            });
        }

        // 11. 暴击落雷
        if (result.IsCritical && !isAgainDamage && skillCard != null && skillCard.IsNormalAttack() && skillCriticalDamage == 0)
        {
            var thunderDamageRate = skillCard.Attributes.GetFinal(AttributeType.SkillCriticalThunderbolt);
            if (thunderDamageRate > 0f)
            {
                var thunderDamage = Mathf.CeilToInt(result.CriticalDamage * thunderDamageRate);
                result.ThunderboltDamage = thunderDamage;
            }
        }

        result.FinalDamage = damage;
        _recorder.EndRecord(damage, false);
        return result;
    }
}