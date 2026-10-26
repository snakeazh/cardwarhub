using FusionContinent.Utils;

public class DodgeModifier : IDamageModifier
{
    public void Apply(DamageContext ctx)
    {
        float missRate = ctx.missRate;

        if (RandomTool.CheckRandomByTarget((int)(missRate * 100)))
        {
            ctx.IsMiss = true;
            ctx.FinalDamage = 0;
        }
    }
}