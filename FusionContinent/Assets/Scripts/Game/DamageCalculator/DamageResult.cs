using FusionContinent;

/// <summary>
/// 伤害管线结算结果，从 DamageContext 快照构造。
/// </summary>
public struct DamageResult
{
    public float FinalDamage;
    public bool IsCrit;
    public bool IsMiss;
    public object Attacker;
    public object Target;
    public int SkillId;

    /// <summary>管线执行后的完整上下文，供结算阶段读取 TrueDamage / IsExecution 等扩展字段。</summary>
    public DamageContext Context;

    public DamageResult(DamageContext ctx)
    {
        IsMiss = ctx.IsMiss;
        IsCrit = ctx.IsCrit;
        FinalDamage = ctx.IsMiss ? 0f : ctx.FinalDamage;
        SkillId = ctx.SkillId;
        Context = ctx;

        if (ctx.PipelineType == DamagePipelineType.MonsterDamage)
        {
            Attacker = ctx.Monster;
            Target = ctx.Soldier;
        }
        else
        {
            Attacker = ctx.Soldier;
            Target = ctx.Monster;
        }
    }
}
