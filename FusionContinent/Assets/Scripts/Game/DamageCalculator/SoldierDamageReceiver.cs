using System;
using Config;
using FusionContinent;
using Game.Battle;
using QFramework;
using UnityEngine;

/// <summary>
/// 英灵受击结算：护盾、扣血、飘字、受击事件。
/// </summary>
public static class SoldierDamageReceiver
{
    public static int Apply(SoldierCard soldier, DamageResult result, in DamageApplyOptions options)
    {
        var recorder = options.Recorder ?? new NullDamageCalculationRecorder();
        soldier.SetLastDamageRecorder(recorder);

        if (result.IsMiss)
        {
            ShowFloatingText(soldier, 0, "MISS");
            return 0;
        }

        var shieldResult = new ShieldResolver(soldier, recorder).Resolve((int)result.FinalDamage);

        if (!shieldResult.IsAbsorbed && shieldResult.FinalDamage > 0)
        {
            soldier.BaseAttributes.SetAttr(AttributeType.SoldierHealth, -shieldResult.FinalDamage);
        }

        soldier.RoundBeHitCount++;
        ShowFloatingText(soldier, -shieldResult.FinalDamage);

        EventBus.Publish(new ConditionsTypeEvent
        {
            coditionsType = ConditionsType.HeroAfterHarmed,
            caster = soldier,
            target = soldier,
            direction = default,
            heroAfterHarmedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        });

        BattleRuleComputerSupport.OnBeHit(soldier, result);

        return shieldResult.FinalDamage;
    }

    private static void ShowFloatingText(SoldierCard soldier, int damageVal, string strVal = "")
    {
        var damageObj = GamePlayManager.Default._resLoader.Get<GameObject>("CityDamage").Instantiate();
        damageObj.transform.position = soldier.HealthBarController.GetDamageTextOffset();
        damageObj.transform.SetParent(GameLand.Default.DamageParent);

        if (strVal != "")
        {
            damageObj.LocalScale(new Vector3(1f, 1f, 1f));
            damageObj.GetComponent<CityDamageFun>().SetData(strVal);
        }
        else
        {
            damageObj.LocalScale(new Vector3(0.6f, 0.6f, 0.6f));
            damageObj.GetComponent<CityDamageFun>().SetData(damageVal);
        }
    }
}
