using System.Collections.Generic;
using Config;
using FusionContinent.Utils;
using UnityEngine;

/// <summary>
/// 属性类伤害修正。
/// 条件触发型增伤（原 atkCarryings → skillCarrying.attributeStructs）已迁移至
/// BattleRuleDamageModifier + EffectGroup，此处保留 AllDamageBonus 百分比；EveryDamage 见 <see cref="EveryDamageModifier"/>。
/// </summary>
public class AttributesModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.AllDamageBonus > 0f)
        {
            ctx.FinalDamage += ctx.FinalDamage * ctx.AllDamageBonus;
            ctx.TrueDamage += ctx.TrueDamage * ctx.AllDamageBonus;
        }
    }
}
