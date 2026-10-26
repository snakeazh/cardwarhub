using FusionContinent;

public class CritModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        if (ctx.Soldier != null)
        {
            ctx.IsCrit = ctx.Soldier.RollCriticalHit(ctx.CritRate);
            return;
        }

        // 无英灵时回退为独立随机（怪物攻击等 CritRate=0 场景）
        float rate01 = ctx.CritRate > 1f ? ctx.CritRate / 100f : ctx.CritRate;
        ctx.IsCrit = rate01 > 0f && UnityEngine.Random.value <= rate01;
    }
}
