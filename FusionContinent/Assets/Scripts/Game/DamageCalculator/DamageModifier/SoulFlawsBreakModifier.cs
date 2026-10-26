using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulFlawsBreakModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if(ctx.IsMiss) return;
        if (ctx.IsWeak)
            ctx.FinalDamage *= 3;
    }
}
