using Config;
using FusionContinent;
using UnityEngine;

public static class Formula
{
    // 攻速 → 攻击间隔
    public static float AttackInterval(
        float attackSpeed,
        float baseValue,
        float maxLimit)
    {
        float scaled = (attackSpeed * GlobalConst.ATTACK_SPEED_FACTOR) / (attackSpeed + baseValue);

        float clamped = Mathf.Min(scaled,maxLimit);

        return clamped;
    }

    /// <summary>
    /// 获取当前的闪避值
    /// </summary>
    /// <param name="missRateSumBase"></param>
    /// <param name="missNum"></param>
    /// <returns></returns>
    public static float GetMissValue(int missRateSumBase,float missNum)
    {
        return 1 - (float)missRateSumBase / (missRateSumBase + missNum);
    }

    /// <summary>
    /// 获取暴击率
    /// </summary>
    /// <param name="baseRate">基础暴击率</param>
    /// <param name="maxRate">最大值</param>
    /// <param name="baseValue">暴击值</param>
    /// <param name="criticalNumBase">最大暴击值</param>
    /// <returns></returns>
    public static float GetCritRate(float baseRate, float maxRate,float baseValue,float criticalNumBase,bool isPre = true)
    {
        float newRate = baseValue / (baseValue + criticalNumBase);
        float value= Mathf.Min(baseRate+newRate,maxRate);
        return isPre?value*100:value;
    }

    /// <summary>
    /// 获取暴击伤害
    /// </summary>
    /// <param name="baseRate">人物暴击率</param>
    /// <param name="criticalNumBase">基础暴击伤害</param>
    /// <returns></returns>
    public static float GetCriticalDamage(float criticalRate,float criticalDamage,bool isPre = true)
    {
        float value= criticalRate > (isPre?100:1) ? (criticalRate-(isPre?100:1))+criticalDamage : criticalDamage;
        return isPre?value*100:value;
    }
}