using Config;

using FusionContinent;

using FusionContinent.Skill;

using Manager;

using UnityEngine;



namespace Game.Battle

{

    /// <summary>

    /// ComputerGroup 计数事件门面。Receiver / 伤害管线只调用此类，不写业务判断。

    /// <para>配表与 12111 示例见 <c>BattleRule_ComputerGroup_12111_Context.md</c>。</para>

    /// </summary>

    public static class BattleRuleComputerSupport

    {

        public static void OnBeHit(SoldierCard soldier, DamageResult result)

        {

            if (soldier == null || result.IsMiss || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var attacker = result.Context != null ? result.Context.Monster : null;

            var context = new BattleRuleTriggerContext(TriggerType.HeroAfterHarmed, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                Source = attacker,

                Target = soldier,

                DamageContext = result.Context,

                CurrentTime = Time.time,

            };



            DispatchCounterEvent(CounterTriggerType.BeHitCount, context);

        }



        /// <summary>

        /// 对目标造成伤害时累加 <see cref="CounterTriggerType.AttackCount"/>（普攻与技能均计入）。

        /// <para>

        /// 须在 <see cref="TriggerType.OnDamageBefore"/> 规则判定之前调用（见 <see cref="BattleRuleDamageModifier"/>）。

        /// 12111/300001 使用 <see cref="CounterOwnerType.Pair"/> 表示「同一英灵-怪物」独立计数。

        /// </para>

        /// </summary>

        public static void AccumulateOutgoingDamageCount(DamageContext damageContext)

        {

            if (damageContext == null || damageContext.Soldier == null || damageContext.Monster == null

                || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var context = new BattleRuleTriggerContext(TriggerType.OnDamageBefore, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                Source = damageContext.Soldier,

                Target = damageContext.Monster,

                DamageContext = damageContext,

                CurrentTime = Time.time,

            };



            DispatchCounterEvent(CounterTriggerType.AttackCount, context);

        }



        /// <summary>

        /// 怪物受击结算后：仅处理暴击类计数；AttackCount 已在伤害管线前累加，此处不重复。

        /// </summary>

        public static void OnOutgoingHit(DamageContext damageContext, ISkill skillCard)

        {

            if (damageContext == null || damageContext.Soldier == null || damageContext.Monster == null

                || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var context = new BattleRuleTriggerContext(TriggerType.OnDamageAfter, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                Source = damageContext.Soldier,

                Target = damageContext.Monster,

                DamageContext = damageContext,

                CurrentTime = Time.time,

            };



            if (damageContext.IsCrit)

            {

                DispatchCounterEvent(CounterTriggerType.CriticalCount, context);

            }

        }



        public static void OnKill(DamageContext damageContext)

        {

            if (damageContext == null || damageContext.Soldier == null || damageContext.Monster == null

                || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var context = new BattleRuleTriggerContext(TriggerType.OnDamageAfter, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                Source = damageContext.Soldier,

                Target = damageContext.Monster,

                DamageContext = damageContext,

                CurrentTime = Time.time,

            };



            DispatchCounterEvent(CounterTriggerType.KillCount, context);

        }



        /// <summary>

        /// 怪物离场后清理 Target / Pair 关联计数。在 <see cref="MonsterCard.Die"/> 调用，避免同帧 OnKill 累加被误清。

        /// </summary>

        public static void OnMonsterRemoved(MonsterCard monster)

        {

            if (monster == null)

            {

                return;

            }



            var manager = GetComputerManager();

            if (manager == null)

            {

                return;

            }



            manager.RemoveCountersByOwnerInstanceId(monster.GetInstanceID());

        }



        public static void OnSkillCast(SoldierCard soldier, int skillId)

        {

            if (soldier == null || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }

            if (skillId == soldier.NormalAttackSkillId)

            {

                return;

            }



            var context = new BattleRuleTriggerContext(TriggerType.OnSkillBefore, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                Source = soldier,

                Target = soldier,

                DamageContext = new DamageContext

                {

                    Soldier = soldier,

                    SkillId = skillId,

                    PipelineType = DamagePipelineType.SoldierDamage,

                },

                CurrentTime = Time.time,

            };



            DispatchCounterEvent(CounterTriggerType.SkillCastCount, context);

            BattleRuleMustCriticalSupport.OnSkillReleased(soldier, skillId);



            if (skillId != soldier.NormalAttackSkillId)

            {

                // PVP：AI（对侧）放技能走独立连击计数，不推进玩家 ComboCount 条件

                if (soldier.BattleSide != BattleSide.Opponent)

                {

                    var manager = GetComputerManager();

                    if (manager != null)

                    {

                        manager.SyncComboCount(Global.ConsecutiveSkillReleaseCount.Value, context);

                    }

                }

            }

        }



        /// <summary>

        /// 自定义计数：customEventId 对应 ComputerGroup.ResetParam（TriggerType/CounterType=Custom）。

        /// </summary>

        public static void OnCustomCounter(int customEventId, BattleRuleTriggerContext context)

        {

            if (context == null || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var manager = GetComputerManager();

            if (manager == null)

            {

                return;

            }



            manager.OnCustomCounterEvent(customEventId, context);

        }



        /// <summary>

        /// Buff 移除时重置 ResetType=BuffRemove 的计数（ResetParam 为 BuffId，0 表示任意 Buff）。

        /// </summary>

        public static void OnBuffRemoved(Component owner, int buffId)

        {

            if (owner == null || AttributeManager.BattleLearnedSkillRules.Count == 0)

            {

                return;

            }



            var manager = GetComputerManager();

            if (manager == null)

            {

                return;

            }



            manager.OnBuffRemoved(owner.GetInstanceID(), buffId);

        }



        /// <summary>

        /// 英灵复活：清除该英灵关联计数，避免死亡前进度残留。

        /// </summary>

        public static void OnOwnerRevived(SoldierCard soldier)

        {

            if (soldier == null)

            {

                return;

            }



            var manager = GetComputerManager();

            if (manager == null)

            {

                return;

            }



            manager.RemoveCountersByOwnerInstanceId(soldier.GetInstanceID());

        }



        /// <summary>
        /// 波次/回合开始时累加 <see cref="CounterTriggerType.round"/> 计数。
        /// 须在 <see cref="TriggerType.RoundStart"/> 规则判定之前调用。
        /// </summary>
        public static void OnRoundStart(BattleRuleTriggerContext context)
        {
            if (context == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            DispatchCounterEvent(CounterTriggerType.round, context);
        }

        /// <summary>
        /// 购买强化（三选一选卡）：累加 PurchaseEffect；专属卡（IsAddHero）额外累加 PurchaseExclusiveEffect。
        /// </summary>
        /// <param name="exclusiveHeroCardId">专属强化对应英灵 cardId；0 表示非专属。</param>
        public static void OnPurchaseEffect(int exclusiveHeroCardId = 0)
        {
            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var manager = GetComputerManager();
            if (manager == null)
            {
                return;
            }

            var context = CreateGlobalNoConditionContext();
            manager.OnCounterEvent(CounterTriggerType.PurchaseEffect, context);
            TryTriggerNoConditionRulesForComputer(CounterTriggerType.PurchaseEffect, context);

            if (exclusiveHeroCardId <= 0)
            {
                return;
            }

            var exclusiveHero = FindAliveHeroByCardId(exclusiveHeroCardId);
            if (exclusiveHero == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleComputerSupport] 专属强化未找到场上英灵: heroCardId={exclusiveHeroCardId}");
                return;
            }

            var exclusiveContext = CreateGlobalNoConditionContext();
            exclusiveContext.Source = exclusiveHero;
            exclusiveContext.Target = exclusiveHero;
            exclusiveContext.ValueProvider = BattleRuleAcquireValueProvider.Instance;
            manager.OnCounterEvent(CounterTriggerType.PurchaseExclusiveEffect, exclusiveContext);
            TryTriggerNoConditionRulesForComputer(
                CounterTriggerType.PurchaseExclusiveEffect, exclusiveContext);
        }

        private static SoldierCard FindAliveHeroByCardId(int heroCardId)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return null;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero != null
                    && !hero.isDie
                    && hero.RoleType != SoldierRoleType.Mirror
                    && hero.cardId == heroCardId)
                {
                    return hero;
                }
            }

            return null;
        }

        /// <summary>
        /// 消耗战斗币：累加 <see cref="CounterTriggerType.BattleGoldNum"/>，并尝试触发挂载该 Computer 的 NoCondition 规则。
        /// </summary>
        public static void OnBattleGoldSpent(int goldAmount)
        {
            if (goldAmount <= 0 || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var context = CreateGlobalNoConditionContext();
            var manager = GetComputerManager();
            if (manager == null)
            {
                return;
            }

            manager.OnCounterEvent(CounterTriggerType.BattleGoldNum, context, goldAmount);
            TryTriggerNoConditionRulesForComputer(CounterTriggerType.BattleGoldNum, context);
        }

        /// <summary>
        /// 每秒累加 <see cref="CounterTriggerType.EverySecond"/>，并尝试触发挂载该 Computer 的 NoCondition 规则。
        /// </summary>
        public static void OnEverySecond()
        {
            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var context = CreateGlobalNoConditionContext();
            DispatchCounterEvent(CounterTriggerType.EverySecond, context);
            TryTriggerNoConditionRulesForComputer(CounterTriggerType.EverySecond, context);
        }

        private static BattleRuleTriggerContext CreateGlobalNoConditionContext()
        {
            return new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                ValueProvider = BattleRuleLineupValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
            };
        }

        private static void TryTriggerNoConditionRulesForComputer(
            CounterTriggerType counterTrigger,
            BattleRuleTriggerContext context)
        {
            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return;
            }

            for (int i = 0; i < 32; i++)
            {
                var triggered = ruleService.TryTriggerNoConditionRulesForComputerTrigger(counterTrigger, context);
                if (triggered <= 0)
                {
                    break;
                }
            }
        }

        private static void DispatchCounterEvent(CounterTriggerType triggerType, BattleRuleTriggerContext context)
        {
            var manager = GetComputerManager();
            if (manager == null)
            {
                return;
            }

            manager.OnCounterEvent(triggerType, context);
        }

        private static BattleRuleComputerManager GetComputerManager()
        {
            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return null;
            }

            return ruleService.ComputerManager;
        }
    }
}


