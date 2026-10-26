using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecutionThresholdModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsExecution)
        {
            ctx.TrueDamage = 999999;
        }
    }
}
