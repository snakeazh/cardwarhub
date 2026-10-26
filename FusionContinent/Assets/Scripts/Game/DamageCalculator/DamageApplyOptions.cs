using Config;
using FusionContinent.Skill;
using UnityEngine;

/// <summary>
/// 伤害结算阶段的附加参数（不参与管线公式计算）。
/// </summary>
public struct DamageApplyOptions
{
    public Vector3 HitPosition;
    public TakeDamageType DamageType;
    public ISkill Skill;
    public bool IsAgainDamage;
    public IDamageCalculationRecorder Recorder;
}
