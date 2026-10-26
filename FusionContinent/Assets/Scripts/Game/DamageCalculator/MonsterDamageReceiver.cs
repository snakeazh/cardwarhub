using Config;
using FusionContinent;
using FusionContinent.Skill;
using Game.Battle;
using UnityEngine;
using ValueType = Config.ValueType;

/// <summary>
/// 怪物受击结算：护盾、扣血、死亡、飘字、受击事件。
/// </summary>
public static class MonsterDamageReceiver
{
    public static int Apply(MonsterCard monster, DamageResult result, in DamageApplyOptions options)
    {
        var ctx = result.Context;
        monster.damageContext = ctx;

        var skillCard = options.Skill;
        var pos = options.HitPosition;
        var damageType = options.DamageType;
        var attacker = ctx.Soldier;

        ApplyHpShieldAndLifesteal(monster, skillCard, attacker);
        monster.OnHit();

        var soulFlaws = skillCard?.Attributes.GetFinal(AttributeType.SoldierSoulFlaws) ?? 0f;
        if (soulFlaws > 0f)
        {
            monster.MonsterAttribute.Armor = 0;
            monster.ShowShieldBreakIconInternal(1f);
        }

        HpDamageEvent hpDamageEvent = new HpDamageEvent
        {
            Position = pos,
            Damage = (int)result.FinalDamage,
            IsCrit = result.IsCrit,
            DamageType = damageType,
            Target = monster.transform,
            TargetCard = monster,
            Caster = attacker,
            IsExecution = false,
            IsMiss = result.IsMiss
        };

        if (ctx.IsExecution)
        {
            hpDamageEvent.Damage = 99999;
            hpDamageEvent.IsExecution = true;
        }

        if (!result.IsCrit)
        {
            hpDamageEvent.DamageType = skillCard != null && skillCard.Config.DamageEffectWords != 0
                ? skillCard.Config.DamageEffectWords
                : damageType;
        }

        EventBus.Publish(hpDamageEvent);

        if (ctx.TrueDamage > 0)
        {
            HpDamageEvent trueDamageEvent = new HpDamageEvent
            {
                Position = pos,
                Damage = (int)ctx.TrueDamage,
                IsCrit = ctx.IsCrit,
                DamageType = TakeDamageType.RealDamage,
                Target = monster.transform,
                TargetCard = monster,
                Caster = attacker,
                IsExecution = false,
                IsMiss = false
            };
            EventBus.Publish(trueDamageEvent);
        }

        if (attacker != null && skillCard != null)
        {
            BattleRuleHitTrigger.TriggerAttackCriticalAfter(ctx, skillCard, attacker, pos);
            BattleRuleHitTrigger.TriggerSkillCriticalAfter(ctx, skillCard, attacker);

            if (skillCard.Id == attacker.NormalAttackSkillId && ctx.IsWeak)
            {
                EventBus.Publish(new ConditionsTypeEvent
                {
                    coditionsType = ConditionsType.SpecialOnAttackAfter,
                    caster = attacker,
                    target = monster
                });
            }
        }

        BattleRuleComputerSupport.OnOutgoingHit(ctx, skillCard);

        BattleRuleHitTrigger.TriggerSkillAfter(ctx, skillCard, attacker);

        if (monster.hp <= 0)
        {
            BattleRuleComputerSupport.OnKill(ctx);
            BattleRuleHitTrigger.TriggerDamageAfter(ctx, attacker);
            monster.Die();
            return (int)result.FinalDamage;
        }

        BattleRuleHitTrigger.TriggerDamageAfter(ctx, attacker);

        if (!ctx.IsWeak && monster.monsterData.MaxResilience > 0f)
        {
            monster.ApplyToughnessDamageInternal(ctx.ToughnessDamage);
            DamageUIManager.Instance.ShowToughnessDamage(ctx.ToughnessDamage, pos);
        }

        monster.TriggerHitEffectInternal();

        if (monster.hp <= 0)
        {
            monster.Die();
            return (int)result.FinalDamage;
        }

        float maxHealthPercentage = (float)monster.maxHp / monster.hp;
        monster.ContinuousDamageNumInternal(skillCard, maxHealthPercentage);
        return (int)result.FinalDamage;
    }

    private static void ApplyHpShieldAndLifesteal(MonsterCard monster, ISkill skillCard, SoldierCard soldierCard)
    {
        var ctx = monster.damageContext;

        if (monster.MonsterAttribute.Armor > 0)
        {
            monster.MonsterAttribute.Armor -= ctx.FinalDamage + ctx.TrueDamage;
        }

        if (monster.MonsterAttribute.Armor < 0)
        {
            ctx.FinalDamage += Mathf.Abs(monster.MonsterAttribute.Armor);
            monster.MonsterAttribute.Armor = 0;
        }

        if (monster.MonsterAttribute.Armor == 0)
        {
            monster.hp -= (int)ctx.FinalDamage + (int)ctx.TrueDamage;

            var residueHpPer = (float)monster.hp / monster.maxHp;
            monster.NotifyMechanicsHpChanged(residueHpPer);
            var executionThreshold = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.MonsterExecutionThreshold);
            if (monster._monsterConfig.MonsterQuality == MonsterQuality.Ordinary
                && monster.hp > 0
                && executionThreshold > 0
                && executionThreshold >= residueHpPer)
            {
                monster.ExecutionIcon.gameObject.SetActive(true);
            }

            var invincibleMoveSpeed = monster.Attributes.GetFinal(
                AttributeType.MonsterHealthBelowTriggerInvincibleMoveSpeed,
                per => residueHpPer <= per);
            if (invincibleMoveSpeed > 0f
                && !monster.HandlingTagsContains("MonsterHealthBelowTriggerInvincibleMoveSpeed"))
            {
                monster.AddHandlingTag("MonsterHealthBelowTriggerInvincibleMoveSpeed");
                monster.GetComponent<MonsterAbnormal>().SetAbnormal(
                    UnusualStatusType.Invincible, 3f, 1, ValueType.Number);
                monster.GetComponent<MonsterAbnormal>().SetAbnormal(
                    UnusualStatusType.Accelerate, 3f, invincibleMoveSpeed, ValueType.Percentage);
            }
        }

        if (soldierCard != null
            && ctx.PhysicalBloodSucking > 0
            && ctx.DamageType == TakeDamageType.PhysicalDamage)
        {
            soldierCard.SetSoldierHp((int)(ctx.FinalDamage * ctx.PhysicalBloodSucking));
        }

        if (soldierCard != null
            && ctx.MagicBloodSucking > 0
            && ctx.DamageType == TakeDamageType.MagicDamage)
        {
            soldierCard.SetSoldierHp((int)(ctx.FinalDamage * ctx.MagicBloodSucking));
        }

        if (soldierCard != null
            && ctx.Soldier.BaseAttributes.GetFinal(AttributeType.BrokenStateNAtkReduceCooling) > 0)
        {
            float reduceCoolingValue = ctx.Soldier.BaseAttributes.GetFinal(AttributeType.BrokenStateNAtkReduceCooling);
            GameLand.Default.ReduceSoldierAllSkillsCooling(ctx.Soldier.cardId, reduceCoolingValue);
        }

        if (ctx.IsCrit)
        {
            if (ctx.ThunderDamageRate > 0)
            {
                monster.PlayEffectCriticalThunderboltInternal(
                    skillCard, monster.transform.position, ctx.ThunderboltDamage);
            }

            BattleRuleHitTrigger.TriggerCriticalAfter(ctx);
        }

        BattleRuleHitTrigger.TriggerNormalAttackAfter(ctx, skillCard, soldierCard);
    }
}
