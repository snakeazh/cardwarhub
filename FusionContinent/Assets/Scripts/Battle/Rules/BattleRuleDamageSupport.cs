using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 伤害管线中的规则支持：全局 OwnerId、规则服务访问、伤害上下文构造、普攻判定。
    /// <para>
    /// 接入位置（英灵 → 怪物）：
    /// <list type="number">
    /// <item><see cref="BattleRuleDamageModifier"/> — 伤害前（OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore）</item>
    /// <item><see cref="BattleRuleHitTrigger"/> — 命中后（OnAttackAfter / OnSkillAfter / OnCriticalAfter 等）</item>
    /// </list>
    /// 规则加成写入 <see cref="DamageContext"/>，不走旧 skillCarrying 跨命中叠乘。
    /// </para>
    /// </summary>
    public static class BattleRuleDamageSupport
    {
        /// <summary>本局战斗规则的全局 OwnerId（与 BattleRuleService 注册一致）。</summary>
        public const int GlobalBattleOwnerId = 0;

        /// <summary>获取本局 <see cref="BattleRuleService"/>；战斗场景外或未初始化时返回 null。</summary>
        public static BattleRuleService GetRuleService()
        {
            return BattleServiceManager.Instance?.GetService<BattleRuleService>();
        }

        /// <summary>
        /// 当前伤害是否来自普攻技能 Id（与 <see cref="SoldierCard.NormalAttackSkillId"/> 比较）。
        /// 用于区分 OnAttackBefore 与 OnSkillBefore 的触发分支。
        /// </summary>
        public static bool IsNormalAttack(DamageContext ctx)
        {
            if (ctx?.Soldier == null || ctx.SkillId <= 0)
            {
                return false;
            }

            return ctx.SkillId == ctx.Soldier.NormalAttackSkillId;
        }

        /// <summary>
        /// 构造伤害前规则上下文：Source=英灵，Target=怪物，携带当前 <see cref="DamageContext"/>。
        /// ValueProvider / EffectExecutor 使用伤害管线专用实现（改上下文，非持久 Buff）。
        /// </summary>
        public static BattleRuleTriggerContext CreateDamageRuleContext(
            DamageContext damageContext,
            TriggerType trigger)
        {
            return new BattleRuleTriggerContext(trigger, GlobalBattleOwnerId)
            {
                Source = damageContext.Soldier,
                Target = damageContext.Monster,
                DamageContext = damageContext,
                ValueProvider = BattleRuleDamageValueProvider.Instance,
                EffectExecutor = BattleRuleDamageEffectExecutor.Instance,
            };
        }
    }

    /// <summary>
    /// 伤害规则条件取值（基于 <see cref="DamageContext"/> / 场上英灵）。
    /// <para>
    /// 英灵/怪物当前血量比例由 <see cref="BattleRuleSoldierHealthConditionValues"/>、
    /// <see cref="BattleRuleMonsterHealthConditionValues"/> 解析（替代旧 EvaluateMonsterConditionsForDamage）。
    /// </para>
    /// <para>
    /// 非生命值 GoalAttribute：Self 读英灵属性，Target 读怪物属性（如攻强、法强用于 AddDamage 来源缩放）。
    /// </para>
    /// </summary>
    public sealed class BattleRuleDamageValueProvider : IBattleRuleValueProvider
    {
        public static readonly BattleRuleDamageValueProvider Instance = new BattleRuleDamageValueProvider();

        /// <inheritdoc />
        public float GetAttributeValue(BattleRuleTriggerContext context, UnitType unitType, AttributeType attributeType)
        {
            // GoalAttribute + 英灵/怪物生命值：读当前 hp 或 hp 比例，而非配置最大生命
            if (BattleRuleSoldierHealthConditionValues.TryResolveGoalAttributeValue(
                    context, unitType, attributeType, out var healthValue))
            {
                return healthValue;
            }

            if (BattleRuleMonsterHealthConditionValues.TryResolveGoalAttributeValue(
                    context, unitType, attributeType, out var monsterHealthValue))
            {
                return monsterHealthValue;
            }

            if (BattleRuleMonsterHealthConditionValues.IsMonsterHealthAttribute(attributeType))
            {
                // 怪物已死亡或未生成时，生命类条件应判为「未满足」而非 0
                return BattleRuleMonsterHealthConditionValues.UnresolvedMonsterHealthValue;
            }

            var damageContext = context?.DamageContext;
            if (damageContext == null)
            {
                return 0f;
            }

            // Self = 本次出伤英灵；Target = 本次受击怪物
            if (unitType == UnitType.Self)
            {
                return damageContext.Soldier != null
                    ? damageContext.Soldier.BaseAttributes.GetFinal(attributeType)
                    : 0f;
            }

            return damageContext.Monster != null
                ? damageContext.Monster.Attributes.GetFinal(attributeType)
                : 0f;
        }

        /// <inheritdoc />
        /// <remarks>伤害前规则一般不用 SpecialType.GoalAttribute（生命比例走 GetAttributeValue）。</remarks>
        public float GetSpecialValue(BattleRuleTriggerContext context, SpecialsType specialType)
        {
            if (specialType == SpecialsType.GoalAttribute)
            {
                return 0f;
            }

            if (specialType == SpecialsType.IsNormalMonster)
            {
                return IsNormalMonster(context) ? 1f : 0f;
            }

            if (specialType == SpecialsType.IsBossMonster)
            {
                return IsEliteOrBossMonster(context) ? 1f : 0f;
            }

            if (specialType == SpecialsType.IsRemoteMonster)
            {
                return IsRemoteMonster(context) ? 1f : 0f;
            }

            if (specialType == SpecialsType.IsFlyingMonster)
            {
                return IsFlyingMonster(context) ? 1f : 0f;
            }

            if (specialType == SpecialsType.HeroNum)
            {
                return CountAliveHeroes();
            }

            var careerType = ToCareerType(specialType);
            if (careerType.HasValue)
            {
                return CountHeroesByCareer(careerType.Value);
            }

            return 0f;
        }

        private static bool IsNormalMonster(BattleRuleTriggerContext context)
        {
            var monster = ResolveMonster(context);
            return monster != null
                && monster._monsterConfig != null
                && monster._monsterConfig.MonsterQuality == MonsterQuality.Ordinary;
        }

        private static bool IsEliteOrBossMonster(BattleRuleTriggerContext context)
        {
            var monster = ResolveMonster(context);
            if (monster == null || monster._monsterConfig == null)
            {
                return false;
            }

            var quality = monster._monsterConfig.MonsterQuality;
            return quality == MonsterQuality.Elite || quality == MonsterQuality.Boss;
        }

        /// <summary>远程怪物：AtkType = Shooting（射击）。</summary>
        private static bool IsRemoteMonster(BattleRuleTriggerContext context)
        {
            var monster = ResolveMonster(context);
            return monster != null
                && monster._monsterConfig != null
                && monster._monsterConfig.AtkType == AtkType.Shooting;
        }

        /// <summary>飞行怪物：MoveType = Flying。</summary>
        private static bool IsFlyingMonster(BattleRuleTriggerContext context)
        {
            var monster = ResolveMonster(context);
            return monster != null
                && monster._monsterConfig != null
                && monster._monsterConfig.MoveType == MoveType.Flying;
        }

        private static MonsterCard ResolveMonster(BattleRuleTriggerContext context)
        {
            if (context == null)
            {
                return null;
            }

            if (context.DamageContext != null && context.DamageContext.Monster != null)
            {
                return context.DamageContext.Monster;
            }

            return context.Target as MonsterCard;
        }

        /// <inheritdoc />
        /// <remarks>伤害管线条件右值固定读 ConditionGroupConfig.ComparisonValue，无动态覆盖。</remarks>
        public float GetComparisonValue(BattleRuleTriggerContext context, ConditionGroupConfig conditionConfig)
        {
            return 0f;
        }

        private static int CountAliveHeroes()
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return 0;
            }

            var count = 0;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                if (!gamePlay.villageGameCards[i].isDie)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountHeroesByCareer(CareerType careerType)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return 0;
            }

            var count = 0;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie)
                {
                    continue;
                }

                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(hero.cardId);
                if (soldierCfg != null && soldierCfg.CarType == careerType)
                {
                    count++;
                }
            }

            return count;
        }

        private static CareerType? ToCareerType(SpecialsType specialType)
        {
            return specialType switch
            {
                SpecialsType.WarriorNum => CareerType.Warrior,
                SpecialsType.ArcherNum => CareerType.Archer,
                SpecialsType.MageNum => CareerType.Wizard,
                SpecialsType.AssassinNum => CareerType.Assassin,
                SpecialsType.AssistantNum => CareerType.Assistant,
                _ => null
            };
        }
    }

    /// <summary>
    /// 伤害规则效果执行入口（OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore）。
    /// 将 EffectGroupConfig.Effects 交给 BattleRuleEffectGroupsApplier 直接修改 DamageContext，
    /// 替代旧 skillCarrying.attributeStructs → AttributesModifier 链路。
    /// </summary>
    public sealed class BattleRuleDamageEffectExecutor : IBattleRuleEffectExecutor
    {
        public static readonly BattleRuleDamageEffectExecutor Instance = new BattleRuleDamageEffectExecutor();

        /// <inheritdoc />
        public void ExecuteEffect(BattleRuleEffectExecuteInfo executeInfo)
        {
            var damageContext = executeInfo?.Context?.DamageContext;
            var effectGroup = executeInfo?.EffectGroupConfig;
            if (damageContext == null || effectGroup == null)
            {
                return;
            }

            BattleRuleEffectGroupsApplier.ApplyForDamagePipeline(
                damageContext,
                executeInfo.Context,
                effectGroup.Effects,
                executeInfo.RuleConfig,
                effectGroup.PersistToAttributeSet);
        }
    }

    /// <summary>
    /// 伤害管线规则节点：在属性/抗性等 Modifier 之前触发本局已学习 SkillRule。
    /// <para>
    /// 注册于 <see cref="DamagePipelineType.SoldierDamage"/> 首位，替代旧
    /// atkCarryings → skillCarrying → AttributesModifier 的条件增伤链路。
    /// </para>
    /// <para>
    /// 单次命中触发顺序（均写 DamageContext，不累加 skillCarrying）：
    /// <list type="number">
    /// <item><see cref="BattleRuleCastDamageSession.TryApplyToDamageContext"/> — 施法时刻持久 ModifyAttr 会话</item>
    /// <item>OnDamageBefore — 任意伤害前（穿透、吸血、AddDamage 等）</item>
    /// <item>OnAttackBefore 或 OnSkillBefore — 普攻/技能二选一（skipCastOnlyEffects 跳过连击类）</item>
    /// <item>OnCriticalBefore — 仅当 ctx.IsCrit</item>
    /// </list>
    /// </para>
    /// </summary>
    public class BattleRuleDamageModifier : IDamageModifier
    {
        /// <inheritdoc />
        public void Apply(DamageContext ctx)
        {
            // 仅处理英灵对怪物的伤害结算（普攻/技能/暴击前规则）。
            if (ctx == null || ctx.Soldier == null || ctx.Monster == null)
            {
                return;
            }

            if (ctx.PipelineType != DamagePipelineType.SoldierDamage)
            {
                return;
            }

            // 连击 / AddSkillNoBuff 重复施法时写入的即时增伤，先于 Before 规则合并进上下文
            BattleRuleCastDamageSession.TryApplyToDamageContext(ctx);

            // 递归防护：ContinuousAttack / AddSkillNoBuff 内部 UseSkill 可设此标志
            if (ctx.SkipBattleRuleBeforeTrigger)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return;
            }

            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            // ComputerGroup 累加须在 OnDamageBefore 判定之前（12111/300001：Pair + AttackCount）。
            // 详见 BattleRule_ComputerGroup_12111_Context.md §2 运行时序。
            BattleRuleComputerSupport.AccumulateOutgoingDamageCount(ctx);

            // 1) 通用伤害前（含 AddPhysicalDamage / 穿透% / 吸血 / Computer 门槛后的 AddDamage 等）
            var damageBeforeContext = BattleRuleDamageSupport.CreateDamageRuleContext(ctx, TriggerType.OnDamageBefore);
            ruleService.TriggerLearnedBattleRules(TriggerType.OnDamageBefore, damageBeforeContext);

            // 2) 普攻与技能 Before 互斥；附带魔法伤害（如 101005）挂在 OnAttackBefore
            if (BattleRuleDamageSupport.IsNormalAttack(ctx))
            {
                var attackContext = BattleRuleDamageSupport.CreateDamageRuleContext(ctx, TriggerType.OnAttackBefore);
                ruleService.TriggerLearnedBattleRules(TriggerType.OnAttackBefore, attackContext, skipCastOnlyEffects: true);
            }
            else if (ctx.SkillId > 0)
            {
                var skillContext = BattleRuleDamageSupport.CreateDamageRuleContext(ctx, TriggerType.OnSkillBefore);
                ruleService.TriggerLearnedBattleRules(TriggerType.OnSkillBefore, skillContext, skipCastOnlyEffects: true);
            }

            // 3) 暴击前（可与 OnDamageBefore / Attack|Skill Before 叠加）
            if (ctx.IsCrit)
            {
                var critContext = BattleRuleDamageSupport.CreateDamageRuleContext(ctx, TriggerType.OnCriticalBefore);
                ruleService.TriggerLearnedBattleRules(TriggerType.OnCriticalBefore, critContext);
            }
        }
    }

    /// <summary>
    /// 命中后规则触发（替代 TakeDamage 内 ConditionsTypeEvent + TriggerBuff 监听）。
    /// <para>
    /// 与伤害前不同：使用 <see cref="BattleRuleAcquireEffectExecutor"/>（学 Buff、CastSkill、CD 等），
    /// 不再走 <see cref="BattleRuleDamageEffectExecutor"/> 的上下文增伤路径。
    /// </para>
    /// </summary>
    public static class BattleRuleHitTrigger
    {
        /// <summary>按指定 Trigger 触发本局已学规则（需有效 DamageContext + 英灵 + 怪物）。</summary>
        public static void Trigger(DamageContext damageContext, TriggerType trigger)
        {
            if (damageContext == null || damageContext.Soldier == null || damageContext.Monster == null)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var ruleContext = new BattleRuleTriggerContext(trigger, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = damageContext.Soldier,
                Target = damageContext.Monster,
                DamageContext = damageContext,
                ValueProvider = BattleRuleDamageValueProvider.Instance,
                // 命中后多为模块行为（学 Buff、学规则等），不走伤害叠加。
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
            };

            ruleService.TriggerLearnedBattleRules(trigger, ruleContext);
        }

        /// <summary>普攻命中后（OnAttackAfter）；非普攻技能 Id 时跳过。</summary>
        public static void TriggerNormalAttackAfter(DamageContext damageContext, ISkill skillCard, SoldierCard soldierCard)
        {
            if (soldierCard == null || skillCard == null || skillCard.Id != soldierCard.NormalAttackSkillId)
            {
                return;
            }

            Trigger(damageContext, TriggerType.OnAttackAfter);
        }

        /// <summary>技能命中后（OnSkillAfter）；普攻 Id 时跳过。</summary>
        public static void TriggerSkillAfter(DamageContext damageContext, ISkill skillCard, SoldierCard soldierCard)
        {
            if (soldierCard == null || skillCard == null || skillCard.Id == soldierCard.NormalAttackSkillId)
            {
                return;
            }

            Trigger(damageContext, TriggerType.OnSkillAfter);
        }

        /// <summary>任意伤害结算后（OnDamageAfter）。</summary>
        public static void TriggerDamageAfter(DamageContext damageContext, SoldierCard soldierCard)
        {
            if (soldierCard == null)
            {
                return;
            }

            Trigger(damageContext, TriggerType.OnDamageAfter);
        }

        /// <summary>暴击命中后（OnCriticalAfter）；非暴击时跳过。</summary>
        public static void TriggerCriticalAfter(DamageContext damageContext)
        {
            if (damageContext == null || !damageContext.IsCrit)
            {
                return;
            }

            Trigger(damageContext, TriggerType.OnCriticalAfter);
        }

        /// <summary>
        /// 技能暴击命中后（OnSkillCriticalAfter）；普攻 Id 或非暴击时跳过。
        /// </summary>
        public static void TriggerSkillCriticalAfter(
            DamageContext damageContext,
            ISkill skillCard,
            SoldierCard soldierCard)
        {
            if (damageContext == null || soldierCard == null || skillCard == null
                || skillCard.Id == soldierCard.NormalAttackSkillId || !damageContext.IsCrit)
            {
                return;
            }

            Trigger(damageContext, TriggerType.OnSkillCriticalAfter);
        }

        /// <summary>
        /// 普攻暴击命中后（OnAttackCriticalAfter）；携带 HitPosition 供 CastSkill 等效果选点。
        /// </summary>
        public static void TriggerAttackCriticalAfter(
            DamageContext damageContext,
            ISkill skillCard,
            SoldierCard soldierCard,
            Vector3 hitPosition)
        {
            if (damageContext == null || soldierCard == null || skillCard == null
                || skillCard.Id != soldierCard.NormalAttackSkillId || !damageContext.IsCrit)
            {
                return;
            }

            var ruleContext = new BattleRuleTriggerContext(TriggerType.OnAttackCriticalAfter, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = damageContext.Soldier,
                Target = damageContext.Monster,
                DamageContext = damageContext,
                HitPosition = hitPosition,
                ValueProvider = BattleRuleDamageValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
            };

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            ruleService.TriggerLearnedBattleRules(TriggerType.OnAttackCriticalAfter, ruleContext);
        }
    }
}
