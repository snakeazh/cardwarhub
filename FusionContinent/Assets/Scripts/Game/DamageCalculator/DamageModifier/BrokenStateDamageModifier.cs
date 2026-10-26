using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;

public class BrokenStateDamageModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        //虚弱增伤
        if (ctx.IsWeak)
        {
            ctx.FinalDamage *= (1 + ctx.BrokenStateDamageCorrection + ctx.MonsterToughnessBreakDamageBonus);
        }

        if (ctx.quality == MonsterQuality.Boss)
        {
            ctx.FinalDamage *= (1+ctx.CorrectionOfDamageSufferedByTheLeaderEnemies);
        }
        
        if (ctx.quality == MonsterQuality.Elite)
        {
            ctx.FinalDamage *= (1+ctx.CorrectionOfDamageSufferedByEliteEnemies);
        }
    }
}