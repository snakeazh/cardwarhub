using FusionContinent;

/// <summary>
/// 伤害结算入口，按目标类型分发到具体 Receiver。
/// </summary>
public static class DamageReceiver
{
    public static int ApplyDamage(DamageResult result, in DamageApplyOptions options = default)
    {
        if (result.Target is SoldierCard soldier)
        {
            return SoldierDamageReceiver.Apply(soldier, result, options);
        }

        if (result.Target is MonsterCard monster)
        {
            return MonsterDamageReceiver.Apply(monster, result, options);
        }

        return 0;
    }
}
