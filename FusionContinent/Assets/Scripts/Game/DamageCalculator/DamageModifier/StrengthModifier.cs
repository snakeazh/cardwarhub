using Config;
using UnityEngine;

public class StrengthModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;
        if(ctx.Soldier == null) return;
        if (ctx.Soldier.MainAttrType != MainAttributeType.Strength)
            return;

        int extra = Mathf.CeilToInt(ctx.RawDamage * 0.2f);

        ctx.RawDamage -= extra;
        ctx.FinalDamage += extra;
    }
}