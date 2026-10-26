using Config;
using UnityEngine;

public class ResistanceModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        float res = ctx.Resistance;
        float preValue;
        float prePenre;
        if (ctx.DamageType == TakeDamageType.MagicDamage)
        {
            preValue = ctx.resistanceIgnorePreValueMagic;
            prePenre = ctx.resistanceIgnorePreValuePenreMagic;
        }
        else
        {
            preValue = ctx.resistanceIgnorePreValueArmor;
            prePenre = ctx.resistanceIgnorePreValuePenreArmor;
        }

        res *= (1 - (ctx.resistanceIgnorePenetraValue + preValue) * (1 + prePenre));
        res -= (ctx.resistanceIgnore+ctx.resistanceIgnoreValue)*(1+ctx.resistanceIgnorePenre);
        res = res < 0 ?0:res;
        
        if (res > 0)
        {
            ctx.FinalDamage = Mathf.CeilToInt(
                ctx.FinalDamage * 500f / (res + 500f)
            );
        }
    }
}