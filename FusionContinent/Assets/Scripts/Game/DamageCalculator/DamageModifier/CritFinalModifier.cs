using UnityEngine;

public class CritFinalModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        if (ctx.IsCrit)
        {
            ctx.FinalDamage *= ctx.CritMultiplier/100;
            ctx.ThunderboltDamage = Mathf.CeilToInt(ctx.FinalDamage * ctx.ThunderDamageRate);
        }
    }
}
