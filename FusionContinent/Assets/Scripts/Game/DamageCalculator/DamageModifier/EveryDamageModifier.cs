using UnityEngine;

/// <summary>
/// 所有伤害固定加值（AttributeType.EveryDamage）。
/// 物理 / 魔法 FinalDamage 与真实伤害 TrueDamage 在非 0 时各叠加一次。
/// 仅用于 SoldierDamage / SceneSkillDamage 管线；BuffSkillDamage（灼烧/DOT 等）不叠加。
/// </summary>
public class EveryDamageModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        EveryDamageSupport.ApplyBonus(ctx);
    }
}

internal static class EveryDamageSupport
{
    public static void ApplyBonus(DamageContext ctx)
    {
        if (ctx.EveryDamage <= 0f)
        {
            return;
        }

        if (!Mathf.Approximately(ctx.FinalDamage, 0f))
        {
            ctx.FinalDamage += ctx.EveryDamage;
        }

        if (!Mathf.Approximately(ctx.TrueDamage, 0f))
        {
            ctx.TrueDamage += ctx.EveryDamage;
        }
    }
}
