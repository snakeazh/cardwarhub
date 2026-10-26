using System;
using System.Collections.Generic;
using Config;
using FusionContinent;

/// <summary>
/// 士兵伤害计算器（兼容层，委托给 DamageContextBuilder / DamagePipeline / ShieldResolver）。
/// </summary>
public class SoldierDamageCalculator
{
    private readonly SoldierCard _soldier;
    private IDamageCalculationRecorder _recorder;

    public class DamageCalculationResult
    {
        public int RawDamage { get; set; }
        public int FinalDamage { get; set; }
        public bool IsDodged { get; set; }
        public TakeDamageType DamageType { get; set; }
        public MonsterCard Attacker { get; set; }
        public DateTime CalculationTime { get; set; }
        public string SoldierName { get; set; }
    }

    public class DamageShieldResult
    {
        public int OriginalDamage { get; set; }
        public int FinalDamage { get; set; }
        public bool IsDamageAbsorbed { get; set; }
        public bool MoonlightShieldConsumed { get; set; }
        public int ShieldAbsorbed { get; set; }
        public List<DamageCalculationStep> Steps { get; set; }
    }

    public SoldierDamageCalculator(SoldierCard soldier, IDamageCalculationRecorder recorder = null)
    {
        _soldier = soldier;
        _recorder = recorder ?? new NullDamageCalculationRecorder();
    }

    public void SetRecorder(IDamageCalculationRecorder recorder)
    {
        _recorder = recorder ?? new NullDamageCalculationRecorder();
    }

    public DamageCalculationResult CalculateDamage(TakeDamageType damageType, int damageVal, MonsterCard monsterCard)
    {
        var result = DamageContextBuilder.CalculateIncoming(_soldier, monsterCard, damageType, damageVal);

        return new DamageCalculationResult
        {
            RawDamage = damageVal,
            FinalDamage = (int)result.FinalDamage,
            IsDodged = result.IsMiss,
            DamageType = damageType,
            Attacker = monsterCard,
            CalculationTime = DateTime.Now,
            SoldierName = _soldier.name,
        };
    }

    public DamageShieldResult ApplyShields(int damage)
    {
        var shieldResult = new ShieldResolver(_soldier, _recorder).Resolve(damage);
        return new DamageShieldResult
        {
            OriginalDamage = shieldResult.OriginalDamage,
            FinalDamage = shieldResult.FinalDamage,
            IsDamageAbsorbed = shieldResult.IsAbsorbed,
            MoonlightShieldConsumed = shieldResult.IsMoonlightShieldConsumed,
            ShieldAbsorbed = shieldResult.ShieldAbsorbed,
            Steps = shieldResult.Steps,
        };
    }

    public string GetRecordReport()
    {
        return _recorder.GetRecordReport();
    }
}
