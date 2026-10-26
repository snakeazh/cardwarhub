using Config;
using FusionContinent;

/// <summary>
/// 职业系英灵伤害修正：攻击方英灵职业与属性匹配时，对 FinalDamage / TrueDamage 按百分比增伤。
/// 属性来源为英灵 BaseAttributes（战斗规则 ModifyAttr 持久写入后可被 GetFinal 读取）。
/// </summary>
public class CareerDamageModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.Soldier == null)
        {
            return;
        }

        var bonus = CareerDamageSupport.ResolveDamageBonus(ctx.Soldier);
        if (bonus <= 0f)
        {
            return;
        }

        ctx.FinalDamage += ctx.FinalDamage * bonus;
        ctx.TrueDamage += ctx.TrueDamage * bonus;
    }
}

internal static class CareerDamageSupport
{
    internal static float ResolveDamageBonus(SoldierCard soldier)
    {
        if (!TryGetCareerDamageAttribute(soldier.soldierBattle.CarType, out var attrType))
        {
            return 0f;
        }

        return soldier.BaseAttributes.GetFinal(attrType);
    }

    private static bool TryGetCareerDamageAttribute(CareerType carType, out AttributeType attrType)
    {
        switch (carType)
        {
            case CareerType.Warrior:
                attrType = AttributeType.SoldierDamageChange;
                return true;
            case CareerType.Archer:
                attrType = AttributeType.ArcherDamageChange;
                return true;
            case CareerType.Wizard:
                attrType = AttributeType.MageDamageChange;
                return true;
            case CareerType.Assassin:
                attrType = AttributeType.AssassinDamageChange;
                return true;
            case CareerType.Assistant:
                attrType = AttributeType.AssistantDamageChange;
                return true;
            default:
                attrType = default;
                return false;
        }
    }
}
