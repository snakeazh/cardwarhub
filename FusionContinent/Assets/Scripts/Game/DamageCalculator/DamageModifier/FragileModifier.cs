using UnityEngine;

public class FragileModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        if (ctx.FragileBonus > 0)
        {
            ctx.FinalDamage += Mathf.CeilToInt(
                ctx.FinalDamage * ctx.FragileBonus
            );
        }
    }
}