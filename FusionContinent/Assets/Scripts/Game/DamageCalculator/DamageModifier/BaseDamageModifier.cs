public class BaseDamageModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        if (ctx.IsMiss) return;

        ctx.FinalDamage = ctx.BaseDamage;
    }
}