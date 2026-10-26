using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Buff;
using UnityEngine;

/// <summary>
/// 护盾结算器（伤害计算管线的后处理阶段）。
/// 
/// 职责：
/// - 在 DamagePipeline 完成伤害计算后，对结算伤害执行护盾吸收逻辑
/// - 月影盾优先：完全吸收本次伤害，消耗 1 层
/// - 普通护盾其次：全部吸收或破碎后穿透剩余伤害
/// 
/// 使用方式：
/// ```csharp
/// var resolver = new ShieldResolver(soldier, recorder);
/// var result = resolver.Resolve(damageAfterPipeline); // damageAfterPipeline 为 int
/// // result.FinalDamage 为穿透护盾后需要扣血的伤害
/// ```
/// </summary>
public class ShieldResolver
{
    private readonly SoldierCard _soldier;
    private readonly IDamageCalculationRecorder _recorder;

    /// <summary>
    /// 护盾结算结果
    /// </summary>
    public class ShieldResult
    {
        /// <summary>原始伤害（进入护盾结算前）</summary>
        public int OriginalDamage;

        /// <summary>穿透护盾后的最终伤害（需扣血）</summary>
        public int FinalDamage;

        /// <summary>护盾是否完全吸收了本次伤害（true 时 FinalDamage == 0）</summary>
        public bool IsAbsorbed;

        /// <summary>月影盾是否消耗了本次伤害</summary>
        public bool IsMoonlightShieldConsumed;

        /// <summary>普通护盾吸收的伤害量（0 表示无护盾或完全穿透）</summary>
        public int ShieldAbsorbed;

        /// <summary>用于调试日志的步骤记录</summary>
        public List<DamageCalculationStep> Steps = new List<DamageCalculationStep>();

        public override string ToString()
        {
            if (IsAbsorbed && FinalDamage == 0)
                return $"护盾完全吸收 | 原始:{OriginalDamage} → 最终:0";
            if (ShieldAbsorbed > 0)
                return $"护盾部分吸收 | 原始:{OriginalDamage} → 最终:{FinalDamage} (吸收:{ShieldAbsorbed})";
            if (IsMoonlightShieldConsumed)
                return $"月影盾抵挡 | 原始:{OriginalDamage} → 最终:{FinalDamage}";
            return $"无护盾 | 原始:{OriginalDamage} → 最终:{FinalDamage}";
        }
    }

    public ShieldResolver(SoldierCard soldier, IDamageCalculationRecorder recorder = null)
    {
        _soldier = soldier;
        _recorder = recorder ?? new NullDamageCalculationRecorder();
    }

    /// <summary>
    /// 结算护盾（月影盾优先完全吸收，否则普通护盾）
    /// </summary>
    /// <param name="damage">经过 DamagePipeline 计算后的伤害值（int）</param>
    /// <returns>护盾结算结果</returns>
    public ShieldResult Resolve(int damage)
    {
        var result = new ShieldResult
        {
            OriginalDamage = damage,
            FinalDamage = damage
        };

        int currentDamage = damage;

        // ── 月影盾（Buff 框架，优先）─────────────────────────────────────────────
        MoonlightShieldBuff moonlightBuff = null;
        if (_soldier.BuffController != null)
        {
            foreach (var b in _soldier.BuffController.GetBuffs(BuffType.MoonlightShield))
            {
                if (b is MoonlightShieldBuff msb)
                {
                    moonlightBuff = msb;
                    break;
                }
            }
        }

        if (moonlightBuff != null && moonlightBuff.ShieldNum > 0)
        {
            var shieldBefore = moonlightBuff.ShieldNum;
            moonlightBuff.OnHitConsume(_soldier);

            var step = new DamageCalculationStep
            {
                StepName = "月影盾",
                InputValue = currentDamage,
                OutputValue = 0,
                IsApplied = true,
                AttributeDescription = "月影盾层数",
                UsedValue = shieldBefore,
                Description = moonlightBuff.ShieldNum > 0
                    ? $"消耗1层月影盾抵挡伤害，剩余{moonlightBuff.ShieldNum}层"
                    : "消耗最后1层月影盾抵挡伤害"
            };
            result.Steps.Add(step);
            _recorder.RecordStep(step);

            result.IsMoonlightShieldConsumed = true;
            result.FinalDamage = 0;
            result.IsAbsorbed = true;
            return result;
        }

        // 月影盾记录（不生效）
        int legacyMoonlightNum =0;
        result.Steps.Add(new DamageCalculationStep
        {
            StepName = "月影盾",
            InputValue = currentDamage,
            OutputValue = currentDamage,
            IsApplied = false,
            AttributeDescription = "月影盾层数",
            UsedValue = legacyMoonlightNum,
            Description = moonlightBuff != null ? "Buff层数已归0" : "无月影盾"
        });
        _recorder.RecordStep(result.Steps[^1]);

        // ── 普通护盾 ──────────────────────────────────────────────────────────────
        if (_soldier.Shield > 0)
        {
            int shieldBefore = _soldier.Shield;
            _soldier.Shield -= currentDamage;

            if (_soldier.Shield < 0)
            {
                // 护盾破碎，穿透剩余伤害
                currentDamage = Math.Abs(_soldier.Shield);
                _soldier.Shield = 0;

                result.Steps.Add(new DamageCalculationStep
                {
                    StepName = "普通护盾",
                    InputValue = damage,
                    OutputValue = currentDamage,
                    IsApplied = true,
                    AttributeDescription = "护盾值",
                    UsedValue = shieldBefore,
                    Description = $"护盾破碎，抵挡{shieldBefore}伤害，剩余{currentDamage}伤害"
                });
                result.ShieldAbsorbed = shieldBefore;
                result.IsAbsorbed = false;
            }
            else
            {
                // 护盾完全吸收
                result.Steps.Add(new DamageCalculationStep
                {
                    StepName = "普通护盾",
                    InputValue = damage,
                    OutputValue = 0,
                    IsApplied = true,
                    AttributeDescription = "护盾值",
                    UsedValue = shieldBefore,
                    Description = $"护盾完全吸收{damage}伤害，剩余护盾{_soldier.Shield}"
                });
                result.ShieldAbsorbed = damage;
                result.IsAbsorbed = true;
                currentDamage = 0;
            }

            _recorder.RecordStep(result.Steps[^1]);
        }
        else
        {
            result.Steps.Add(new DamageCalculationStep
            {
                StepName = "普通护盾",
                InputValue = currentDamage,
                OutputValue = currentDamage,
                IsApplied = false,
                AttributeDescription = "护盾值",
                UsedValue = 0,
                Description = "无护盾"
            });
            _recorder.RecordStep(result.Steps[^1]);
        }

        result.FinalDamage = currentDamage;
        return result;
    }
}
