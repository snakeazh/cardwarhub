using UnityEngine;

public class TakeDamageModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        float before = ctx.FinalDamage;
        float after = before + Mathf.FloorToInt(before * ctx.SoldierTakeDamage);

        ctx.FinalDamage = after;
    }
}