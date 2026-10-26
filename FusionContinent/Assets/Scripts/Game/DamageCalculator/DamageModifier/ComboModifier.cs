using FusionContinent;
using FusionContinent.Utils;
using UnityEngine;

public class ComboModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.comboNum == 0)
        {
            return;
        }
        
        if(ctx.comboNum > 6 )
            ctx.comboNum = 6;
        
        
        ctx.FinalDamage = Mathf.CeilToInt(
            (1 + (ctx.ComboDamageRatio) * (ctx.comboNum - 1)) * ctx.FinalDamage);
        
        ctx.ToughnessDamage = Mathf.CeilToInt(
            (1 + (ctx.ComboResilienceRatio) * (ctx.comboNum - 1)) * ctx.ToughnessDamage);
    }
}