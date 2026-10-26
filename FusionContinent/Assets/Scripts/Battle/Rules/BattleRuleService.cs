using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UIFramework.Services;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 战斗规则服务：本局 SkillRule 的运行时调度中心。
    /// <para>
    /// 职责：规则注册（按 OwnerId）、触发时机分发、冷却/次数/概率/条件校验、效果组执行。
    /// 已学习规则来源：<see cref="AttributeManager.BattleLearnedSkillRuleIds"/>。
    /// </para>
    /// <para>
    /// 触发链路：TriggerXxx → BuildContext → CanTrigger → PassProbability
    /// → ConditionManager → EffectManager → MarkTriggered。
    /// 伤害前/暴击前等改伤走 <see cref="BattleRuleDamageEffectExecutor"/>；
    /// 获得时/命中后/HpChange 走 <see cref="BattleRuleAcquireEffectExecutor"/>。
    /// </para>
    /// </summary>
    public class BattleRuleService : GameServiceBase
    {
        /// <summary>
        /// OwnerId -> RuleId 列表。
        /// </summary>
        private readonly Dictionary<int, List<int>> mOwnerRuleMap = new Dictionary<int, List<int>>();

        /// <summary>
        /// 每条规则的运行时状态（次数、冷却）。
        /// </summary>
        private readonly Dictionary<RuleRuntimeKey, RuleRuntimeState> mRuleRuntimeStates = new Dictionary<RuleRuntimeKey, RuleRuntimeState>();

        /// <summary>
        /// 每个 owner 的上下文依赖绑定。
        /// </summary>
        private readonly Dictionary<int, OwnerRuleBinding> mOwnerBindings = new Dictionary<int, OwnerRuleBinding>();

        /// <summary>SkillRule 配置表（Luban）。</summary>
        private TbSkillRuleConfig mSkillRuleTable;

        /// <summary>条件组 AND 判定。</summary>
        private BattleRuleConditionManager mConditionManager;

        /// <summary>条件组配置表（阵容规则识别用）。</summary>
        private TbConditionGroupConfig mConditionConfigTable;

        /// <summary>效果组分发与执行。</summary>
        private BattleRuleEffectManager mEffectManager;

        /// <summary>ComputerGroup 计数管理。</summary>
        private BattleRuleComputerManager mComputerManager;

        /// <summary>
        /// 规则效果执行回调（透传自 EffectManager）。
        /// </summary>
        public event Action<BattleRuleEffectExecuteInfo> OnRuleEffectTriggered;

        public BattleRuleComputerManager ComputerManager => mComputerManager;

        /// <summary>
        /// 初始化：加载配置表、创建条件/效果管理器、注册全局战斗规则、补触发 NoCondition 规则。
        /// </summary>
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();

            mSkillRuleTable = DataConfigManager.Tables.TbSkillRuleConfig;
            mConditionConfigTable = DataConfigManager.Tables.TbConditionGroupConfig;
            mConditionManager = new BattleRuleConditionManager(mConditionConfigTable);
            mEffectManager = new BattleRuleEffectManager(DataConfigManager.Tables.TbEffectGroupConfig);
            mEffectManager.OnEffectTriggered += HandleEffectTriggered;
            mComputerManager = new BattleRuleComputerManager(
                DataConfigManager.Tables.TbComputerGroupConfig,
                mSkillRuleTable);

            var learnedRuleIds = new List<int>();
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                learnedRuleIds.Add(learnedRules[i].RuleId);
            }

            RegisterOwnerRules(
                BattleRuleDamageSupport.GlobalBattleOwnerId,
                learnedRuleIds,
                BattleRuleDamageValueProvider.Instance,
                BattleRuleDamageEffectExecutor.Instance);

            TriggerPendingNoConditionRulesOnInit();
            BattleRuleAttachedDamageSupport.TriggerPendingAttachedPhysicalDamageRulesOnInit();
            TriggerPendingHpChangeRulesOnInit();
            mComputerManager.RebuildActiveComputerGroupIndex();

            LogTool.Log("[BattleRuleService] 初始化完成");
        }

        /// <summary>本局已学规则变化后，刷新 ComputerGroup 活跃索引。</summary>
        public void RefreshComputerGroupIndex()
        {
            if (mComputerManager != null)
            {
                mComputerManager.RebuildActiveComputerGroupIndex();
            }
        }

        /// <summary>清空全部运行时计数（新局开战、局内重开时调用）。</summary>
        public void ClearComputerCounters()
        {
            if (mComputerManager != null)
            {
                mComputerManager.ClearCounters();
            }
        }

        public int GetRuleTriggerCount(int ownerId, int ruleId)
        {
            var key = new RuleRuntimeKey(ownerId, ruleId);
            if (!mRuleRuntimeStates.TryGetValue(key, out var state))
            {
                return 0;
            }

            return state.TriggerCount;
        }

        /// <summary>
        /// 获得规则时尝试直接触发（TriggerType.NoCondition）。
        /// ConditionGroupId 通过则立即执行 EffectGroupId。
        /// </summary>
        public bool TryTriggerNoConditionOnAcquire(
            int ruleId,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard)
        {
            return TryTriggerNoConditionOnAcquire(new BattleLearnedRuleEntry(ruleId, stackSourceKey, sourceType));
        }

        public bool TryTriggerNoConditionOnAcquire(BattleLearnedRuleEntry entry)
        {
            var ruleId = entry.RuleId;
            var stackSourceKey = entry.StackSourceKey;
            var sourceType = entry.SourceType;
            var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);
            if (ruleConfig == null || ruleConfig.Trigger != TriggerType.NoCondition)
            {
                return false;
            }

            if (BattleRuleRosterCompositionSupport.IsGloballyEffectiveNoConditionRule(ruleConfig))
            {
                return TryApplyRosterCompositionRule(ruleId);
            }

            if (BattleRuleSoldierHealthConditionValues.UsesSoldierHealthConditionGroups(
                    ruleConfig.ConditionGroupId, mConditionConfigTable)
                || BattleRuleScopedConditionSupport.UsesPerSoldierScopedConditions(
                    ruleConfig, mConditionConfigTable))
            {
                return TryApplyNoConditionRuleToAllSoldiers(ruleId, stackSourceKey, sourceType);
            }

            if (BattleRuleHeroScopeSupport.IsHeroScoped(entry))
            {
                var scopedHero = GamePlayManager.Default.GetVillageGameCard(entry.ScopeHeroCardId);
                if (scopedHero == null || scopedHero.isDie)
                {
                    LogTool.LogWarning(
                        $"[BattleRuleService] 英雄专属 NoCondition 规则跳过：场上无目标英灵, RuleId={ruleId}, hero={entry.ScopeHeroCardId}");
                    return false;
                }

                var scopedContext = new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
                {
                    Source = scopedHero,
                    ValueProvider = BattleRuleAcquireValueProvider.Instance,
                    EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                    CurrentTime = Time.time,
                    StackSourceKey = stackSourceKey,
                    ModifierSourceType = sourceType,
                    ScopeHeroCardId = entry.ScopeHeroCardId,
                };

                var scopedTriggered = TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, scopedContext);
                if (scopedTriggered)
                {
                    LogTool.Log(
                        $"[BattleRuleService] 英雄专属直接触发规则成功: RuleId={ruleId}, hero={entry.ScopeHeroCardId}, StackKey={stackSourceKey}");
                }

                return scopedTriggered;
            }

            var context = new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
                StackSourceKey = stackSourceKey,
                ModifierSourceType = sourceType,
            };

            var triggered = TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, context);
            if (triggered)
            {
                LogTool.Log($"[BattleRuleService] 直接触发规则成功: RuleId={ruleId}, StackKey={stackSourceKey}");
            }

            return triggered;
        }

        /// <summary>
        /// ComputerGroup 累加后：仅重试 Trigger=NoCondition 且 ComputerGroup.TriggerType 匹配的已学规则。
        /// </summary>
        public int TryTriggerNoConditionRulesForComputerTrigger(
            CounterTriggerType counterTrigger,
            BattleRuleTriggerContext sharedContext)
        {
            var triggeredCount = 0;
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null || ruleConfig.Trigger != TriggerType.NoCondition)
                {
                    continue;
                }

                if (!RuleUsesComputerTrigger(ruleConfig, counterTrigger))
                {
                    continue;
                }

                var context = sharedContext != null
                    ? CloneTriggerContext(sharedContext, entry)
                    : BuildNoConditionContext(entry);

                if (ShouldPickRandomNonAssistantTarget(ruleConfig))
                {
                    var target = PickRandomNonAssistantSoldier();
                    if (target == null)
                    {
                        continue;
                    }

                    context.Source = target;
                    context.ValueProvider = BattleRuleAcquireValueProvider.Instance;
                }
                else if (BattleRuleRosterCompositionSupport.IsGloballyEffectiveRule(ruleConfig))
                {
                    context.ValueProvider = BattleRuleLineupValueProvider.Instance;
                    context.Source = null;
                }
                else if (BattleRuleScopedConditionSupport.UsesPerSoldierScopedConditions(
                             ruleConfig, mConditionConfigTable)
                         || UsesLiveCareerCountConditions(ruleConfig))
                {
                    if (TryApplyNoConditionRuleToAllSoldiers(
                            entry.RuleId, entry.StackSourceKey, entry.SourceType))
                    {
                        triggeredCount++;
                    }

                    continue;
                }

                if (TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, context))
                {
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        /// <summary>
        /// 局内购买强化种类变化后：重试依赖 BuyEffectsNumInBattle 条件的 NoCondition 规则。
        /// </summary>
        public int TryTriggerNoConditionRulesForBuyEffects(BattleRuleTriggerContext sharedContext)
        {
            var triggeredCount = 0;
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null || ruleConfig.Trigger != TriggerType.NoCondition)
                {
                    continue;
                }

                if (!RuleUsesBuyEffectsCondition(ruleConfig))
                {
                    continue;
                }

                var context = sharedContext != null
                    ? CloneTriggerContext(sharedContext, entry)
                    : BuildNoConditionContext(entry);
                context.ValueProvider = BattleRuleLineupValueProvider.Instance;
                context.Source = null;

                if (TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, context))
                {
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        private bool RuleUsesBuyEffectsCondition(SkillRuleConfig ruleConfig)
        {
            if (ruleConfig.ConditionGroupId == null || ruleConfig.ConditionGroupId.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ConditionGroupId.Count; i++)
            {
                var conditionConfig = mConditionConfigTable.GetOrDefault(ruleConfig.ConditionGroupId[i]);
                if (conditionConfig != null
                    && conditionConfig.SpecialType == SpecialsType.BuyEffectsNumInBattle)
                {
                    return true;
                }
            }

            return false;
        }

        private bool RuleUsesComputerTrigger(SkillRuleConfig ruleConfig, CounterTriggerType counterTrigger)
        {
            if (ruleConfig.ComputerGroupId == null || ruleConfig.ComputerGroupId.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ComputerGroupId.Count; i++)
            {
                var computerConfig = mComputerManager != null
                    ? DataConfigManager.Tables.TbComputerGroupConfig.GetOrDefault(ruleConfig.ComputerGroupId[i])
                    : null;
                if (computerConfig != null && computerConfig.TriggerType == counterTrigger)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldPickRandomNonAssistantTarget(SkillRuleConfig ruleConfig)
        {
            if (ruleConfig.ConditionGroupId == null || ruleConfig.ConditionGroupId.Count == 0)
            {
                return false;
            }

            if (!mEffectManager.ContainsEffectType(ruleConfig.EffectGroupId, EffectGroupsType.AddBuff))
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ConditionGroupId.Count; i++)
            {
                var conditionConfig = mConditionConfigTable.GetOrDefault(ruleConfig.ConditionGroupId[i]);
                if (conditionConfig != null
                    && conditionConfig.SpecialType == SpecialsType.IsAssistant
                    && conditionConfig.ComparisonType == ComparisonsType.No)
                {
                    return true;
                }
            }

            return false;
        }

        private bool UsesLiveCareerCountConditions(SkillRuleConfig ruleConfig)
        {
            if (ruleConfig.ConditionGroupId == null)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ConditionGroupId.Count; i++)
            {
                var conditionConfig = mConditionConfigTable.GetOrDefault(ruleConfig.ConditionGroupId[i]);
                if (conditionConfig == null)
                {
                    continue;
                }

                if (conditionConfig.SpecialType == SpecialsType.LiveMageNum
                    || conditionConfig.SpecialType == SpecialsType.LiveWarriorNum
                    || conditionConfig.SpecialType == SpecialsType.BuyEffectsNumInBattle)
                {
                    return true;
                }
            }

            return false;
        }

        private static SoldierCard PickRandomNonAssistantSoldier()
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return null;
            }

            var candidates = new List<SoldierCard>();
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie || hero.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(hero.cardId);
                if (soldierCfg != null && soldierCfg.CarType == CareerType.Assistant)
                {
                    continue;
                }

                candidates.Add(hero);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static BattleRuleTriggerContext BuildNoConditionContext(BattleLearnedRuleEntry entry)
        {
            return new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
                StackSourceKey = entry.StackSourceKey,
                ModifierSourceType = entry.SourceType,
                ScopeHeroCardId = entry.ScopeHeroCardId,
            };
        }

        private static BattleRuleTriggerContext CloneTriggerContext(
            BattleRuleTriggerContext sharedContext,
            BattleLearnedRuleEntry entry)
        {
            return new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = sharedContext.Source,
                Target = sharedContext.Target,
                ValueProvider = sharedContext.ValueProvider ?? BattleRuleAcquireValueProvider.Instance,
                EffectExecutor = sharedContext.EffectExecutor ?? BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = sharedContext.CurrentTime > 0f ? sharedContext.CurrentTime : Time.time,
                StackSourceKey = entry.StackSourceKey,
                ModifierSourceType = entry.SourceType,
                ScopeHeroCardId = entry.ScopeHeroCardId,
            };
        }

        /// <summary>
        /// 战斗服务初始化时，补触发「获得早于服务初始化」的直接触发规则。
        /// </summary>
        public void TriggerPendingNoConditionRulesOnInit()
        {
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null || ruleConfig.Trigger != TriggerType.NoCondition)
                {
                    continue;
                }

                if (BattleRuleRosterCompositionSupport.IsGloballyEffectiveNoConditionRule(ruleConfig))
                {
                    TryApplyRosterCompositionRule(entry.RuleId);
                }
                else if (entry.SourceType != AttributeSourceType.Equip
                         && entry.SourceType != AttributeSourceType.FusionGem
                         && !IsDeferredSpeakerWeaponRuleEntry(entry))
                {
                    TryTriggerNoConditionOnAcquire(entry);
                }
            }
        }

        /// <summary>
        /// 阵容组成规则：按入局编队 Global.SoldierTeams 判条件，满足则全队持久 ModifyAttr（整局保持，不因死亡/未生成撤销）。
        /// </summary>
        public bool TryApplyRosterCompositionRule(int ruleId)
        {
            var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);
            if (ruleConfig == null
                || ruleConfig.Trigger != TriggerType.NoCondition
                || !BattleRuleRosterCompositionSupport.IsGloballyEffectiveNoConditionRule(ruleConfig))
            {
                return false;
            }

            var rosterDesc = BattleRuleRosterCompositionSupport.DescribeLineupConditions(
                ruleConfig.ConditionGroupId,
                mConditionConfigTable);

            if (!BattleRuleRosterCompositionSupport.IsLineupConditionMet(ruleConfig.ConditionGroupId, mConditionManager))
            {
                LogTool.Log(
                    $"[BattleRuleService] 入局阵容规则未满足: RuleId={ruleId}, {rosterDesc}");
                return false;
            }

            var context = BattleRuleRosterCompositionSupport.CreateLineupRuleContext();

            for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
            {
                var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[i]);
                if (effectGroup == null)
                {
                    continue;
                }

                BattleRuleAcquireEffectExecutor.ApplyEffectGroup(ruleConfig, effectGroup, context, null);
            }

            LogTool.Log(
                $"[BattleRuleService] 入局阵容规则已生效: RuleId={ruleId}, {rosterDesc}, effects={string.Join(",", ruleConfig.EffectGroupId)}");
            return true;
        }

        /// <summary>
        /// GloballyEffective=1：按入局编队判条件，对单个英灵补同步全队规则效果。
        /// </summary>
        public void ApplyRosterCompositionRulesToSoldier(SoldierCard soldier)
        {
            if (soldier == null || soldier.isDie || soldier.RoleType == SoldierRoleType.Mirror)
            {
                return;
            }

            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null
                    || !BattleRuleRosterCompositionSupport.IsGloballyEffectiveNoConditionRule(ruleConfig))
                {
                    continue;
                }

                if (!BattleRuleRosterCompositionSupport.IsLineupConditionMet(ruleConfig.ConditionGroupId, mConditionManager))
                {
                    continue;
                }

                var context = BattleRuleRosterCompositionSupport.CreateLineupRuleContext();
                context.StackSourceKey = entry.StackSourceKey;
                context.ModifierSourceType = entry.SourceType;
                context.ScopeHeroCardId = entry.ScopeHeroCardId;
                for (int g = 0; g < ruleConfig.EffectGroupId.Count; g++)
                {
                    var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[g]);
                    if (effectGroup == null)
                    {
                        continue;
                    }

                    BattleRuleAcquireEffectExecutor.ApplyEffectGroup(ruleConfig, effectGroup, context, soldier);
                }

                LogTool.Log(
                    $"[BattleRuleService] 入局阵容规则补同步: RuleId={entry.RuleId}, hero={soldier.cardId}");
            }
        }

        /// <summary>
        /// GloballyEffective=0 的装备规则：按英灵逐个判条件并施加（含月影祝福叠加 StackKey）。
        /// </summary>
        public void ApplyEquipLearnedRulesToSoldier(SoldierCard soldier)
        {
            if (soldier == null || soldier.isDie || soldier.RoleType == SoldierRoleType.Mirror)
            {
                return;
            }

            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            if (learnedRules.Count == 0)
            {
                return;
            }

            var appliedStackKeys = new HashSet<string>();

            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.SourceType != AttributeSourceType.Equip
                    && entry.SourceType != AttributeSourceType.FusionGem
                    && !IsDeferredSpeakerWeaponRuleEntry(entry))
                {
                    continue;
                }

                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null)
                {
                    continue;
                }

                if (BattleRuleRosterCompositionSupport.IsGloballyEffectiveRule(ruleConfig))
                {
                    continue;
                }

                BattleRuleNewSoldierSyncSupport.ReevaluateRuleOnSoldier(
                    mConditionManager,
                    soldier,
                    ruleConfig,
                    entry.StackSourceKey,
                    entry.SourceType);
                appliedStackKeys.Add(entry.StackSourceKey);
            }

            ApplyMissingEquipEntryEffectBonusRules(soldier, learnedRules, appliedStackKeys);
        }

        /// <summary>
        /// 月影祝福(126)：若 Module 登记阶段未生成 EntryEffectBonus 条目，按同件装备配置补施加叠加层。
        /// </summary>
        private void ApplyMissingEquipEntryEffectBonusRules(
            SoldierCard soldier,
            IReadOnlyList<BattleLearnedRuleEntry> learnedRules,
            HashSet<string> appliedStackKeys)
        {
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.SourceType != AttributeSourceType.Equip)
                {
                    continue;
                }

                if (!FusionContinent.Model.Equip.TryParseEquipEntryStackSourceKey(
                        entry.StackSourceKey, out var equipInstanceId, out var entryId))
                {
                    continue;
                }

                if (!Global.PlayerEquips.TryGetValue(equipInstanceId, out var equip))
                {
                    continue;
                }

                var bonusStackCount = FusionContinent.Model.Equip.GetEquipEntryEffectBonusStackCount(equip);
                if (bonusStackCount <= 0)
                {
                    continue;
                }

                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null
                    || BattleRuleRosterCompositionSupport.IsGloballyEffectiveRule(ruleConfig))
                {
                    continue;
                }

                for (int bonusIndex = 1; bonusIndex <= bonusStackCount; bonusIndex++)
                {
                    var bonusStackSourceKey = FusionContinent.Model.Equip.BuildEquipEntryEffectBonusStackSourceKey(
                        equipInstanceId,
                        entryId,
                        bonusIndex);
                    if (appliedStackKeys.Contains(bonusStackSourceKey))
                    {
                        continue;
                    }

                    BattleRuleNewSoldierSyncSupport.ReevaluateRuleOnSoldier(
                        mConditionManager,
                        soldier,
                        ruleConfig,
                        bonusStackSourceKey,
                        entry.SourceType);
                    appliedStackKeys.Add(bonusStackSourceKey);
                    LogTool.Log(
                        "[BattleRuleService] 月影祝福补同步: RuleId=" + entry.RuleId
                        + ", StackKey=" + bonusStackSourceKey + ", hero=" + soldier.cardId);
                }
            }
        }

        /// <summary>
        /// 战斗服务初始化时，补同步「学习早于服务初始化」的 HpChange 规则（满血加 Buff 等）。
        /// </summary>
        public void TriggerPendingHpChangeRulesOnInit()
        {
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                TryApplyHpChangeRuleToAllSoldiers(
                    entry.RuleId,
                    entry.StackSourceKey,
                    entry.SourceType);
            }
        }

        /// <summary>
        /// 新召唤英灵入队后，按 SkillRuleConfig.NewSoldierSync 将本局已学规则同步到该英灵。
        /// </summary>
        public void ApplyLearnedRulesToNewSoldier(SoldierCard soldier)
        {
            if (soldier == null || soldier.isDie || soldier.RoleType == SoldierRoleType.Mirror)
            {
                return;
            }

            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.SourceType == AttributeSourceType.Equip
                    || entry.SourceType == AttributeSourceType.FusionGem)
                {
                    continue;
                }

                if (BattleRuleHeroScopeSupport.IsHeroScoped(entry)
                    && soldier.cardId != entry.ScopeHeroCardId)
                {
                    continue;
                }

                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null)
                {
                    continue;
                }

                BattleRuleNewSoldierSyncSupport.ApplyToSoldier(
                    this,
                    mConditionManager,
                    mConditionConfigTable,
                    soldier,
                    ruleConfig,
                    entry.StackSourceKey,
                    entry.SourceType);
            }
        }

        /// <summary>
        /// 获得 HpChange 规则时：对场上所有存活英灵按当前生命比例判条件并执行效果（不经过冷却/次数，与入队补同步一致）。
        /// </summary>
        public void TryApplyHpChangeRuleToAllSoldiers(
            int ruleId,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard)
        {
            var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);
            if (ruleConfig == null || ruleConfig.Trigger != TriggerType.HpChange)
            {
                return;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie || hero.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                var context = new BattleRuleTriggerContext(TriggerType.HpChange, BattleRuleDamageSupport.GlobalBattleOwnerId)
                {
                    Source = hero,
                    Target = hero,
                    ValueProvider = BattleRuleHpChangeValueProvider.Instance,
                    EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                    CurrentTime = Time.time,
                    StackSourceKey = stackSourceKey,
                    ModifierSourceType = sourceType,
                };

                if (!mConditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
                {
                    BattleRuleHpChangeModifyAttrSupport.RemoveModifyAttrEffects(ruleConfig, context);
                    continue;
                }

                mEffectManager.ExecuteEffects(ruleConfig.EffectGroupId, ruleConfig, context);
                LogTool.Log(
                    $"[BattleRuleService] HpChange 规则入队同步: RuleId={ruleId}, StackKey={stackSourceKey}, hero={hero.cardId}");
            }
        }

        /// <summary>
        /// 获得 NoCondition 规则时：对场上英灵逐个判条件并执行（满血加 Buff 等，与 HpChange 入队同步一致）。
        /// </summary>
        public bool TryApplyNoConditionRuleToAllSoldiers(
            int ruleId,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard)
        {
            var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);
            if (ruleConfig == null || ruleConfig.Trigger != TriggerType.NoCondition)
            {
                return false;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return false;
            }

            var triggered = false;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie || hero.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                var context = new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
                {
                    Source = hero,
                    Target = hero,
                    ValueProvider = BattleRuleAcquireValueProvider.Instance,
                    EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                    CurrentTime = Time.time,
                    StackSourceKey = stackSourceKey,
                    ModifierSourceType = sourceType,
                };

                if (!mConditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
                {
                    continue;
                }

                for (int g = 0; g < ruleConfig.EffectGroupId.Count; g++)
                {
                    var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[g]);
                    if (effectGroup == null)
                    {
                        continue;
                    }

                    BattleRuleAcquireEffectExecutor.ApplyEffectGroup(ruleConfig, effectGroup, context, hero);
                }

                triggered = true;
                LogTool.Log(
                    $"[BattleRuleService] NoCondition 规则入队同步: RuleId={ruleId}, StackKey={stackSourceKey}, hero={hero.cardId}");
            }

            return triggered;
        }

        /// <summary>
        /// 触发本局已学习的全局规则（遍历 AttributeManager 缓存，按 Trigger 过滤）。
        /// </summary>
        /// <returns>成功触发的规则条数。</returns>
        public int TriggerLearnedBattleRules(TriggerType trigger, BattleRuleTriggerContext context)
        {
            return TriggerLearnedBattleRules(trigger, context, skipCastOnlyEffects: false);
        }

        /// <param name="skipCastOnlyEffects">为 true 时跳过含 AddSkillNoBuff / ContinuousAttack 的规则（施法时刻已处理）。</param>
        public int TriggerLearnedBattleRules(
            TriggerType trigger,
            BattleRuleTriggerContext context,
            bool skipCastOnlyEffects)
        {
            var triggeredCount = 0;
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (!BattleRuleHeroScopeSupport.MatchesTriggerContext(entry, context))
                {
                    continue;
                }

                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null || ruleConfig.Trigger != trigger)
                {
                    continue;
                }

                if (skipCastOnlyEffects && mEffectManager.ContainsCastOnlyEffect(ruleConfig.EffectGroupId))
                {
                    continue;
                }

                var runtimeContext = BuildContext(
                    BattleRuleDamageSupport.GlobalBattleOwnerId,
                    trigger,
                    context,
                    mOwnerBindings.TryGetValue(BattleRuleDamageSupport.GlobalBattleOwnerId, out var binding) ? binding : null);
                runtimeContext.StackSourceKey = entry.StackSourceKey;
                runtimeContext.ModifierSourceType = entry.SourceType;
                runtimeContext.ScopeHeroCardId = entry.ScopeHeroCardId;

                if (TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, runtimeContext))
                {
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        /// <summary>
        /// 施法时刻触发：仅执行规则效果组中包含指定类型的规则（如普攻连击 ContinuousAttack）。
        /// 与伤害管线 OnAttackBefore 分离，避免每次命中/多段伤害重复触发。
        /// </summary>
        public int TriggerLearnedBattleRulesForCastEffects(
            TriggerType trigger,
            BattleRuleTriggerContext context,
            EffectGroupsType castEffectType)
        {
            var triggeredCount = 0;
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (!BattleRuleHeroScopeSupport.MatchesTriggerContext(entry, context))
                {
                    continue;
                }

                var ruleConfig = mSkillRuleTable.GetOrDefault(entry.RuleId);
                if (ruleConfig == null || ruleConfig.Trigger != trigger)
                {
                    continue;
                }

                if (!mEffectManager.ContainsEffectType(ruleConfig.EffectGroupId, castEffectType))
                {
                    continue;
                }

                var runtimeContext = BuildContext(
                    BattleRuleDamageSupport.GlobalBattleOwnerId,
                    trigger,
                    context,
                    mOwnerBindings.TryGetValue(BattleRuleDamageSupport.GlobalBattleOwnerId, out var binding) ? binding : null);
                runtimeContext.StackSourceKey = entry.StackSourceKey;
                runtimeContext.ModifierSourceType = entry.SourceType;
                runtimeContext.ScopeHeroCardId = entry.ScopeHeroCardId;
                runtimeContext.EffectExecutor = BattleRuleCastEffectExecutor.Instance;

                if (TryExecuteRule(BattleRuleDamageSupport.GlobalBattleOwnerId, ruleConfig, runtimeContext))
                {
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        /// <summary>
        /// 单条规则完整触发流程：冷却/次数 → 概率 → 条件组 → 效果。
        /// </summary>
        private bool TryExecuteRule(int ownerId, SkillRuleConfig ruleConfig, BattleRuleTriggerContext context)
        {
            // 1. 参数校验
            if (ruleConfig == null || context == null)
            {
                return false;
            }

            // 2. 次数上限 / 冷却
            if (!CanTrigger(ownerId, ruleConfig, context.CurrentTime))
            {
                LogHpChangeRuleSkip(ruleConfig, context, "冷却或次数上限");
                return false;
            }

            // 3. 触发概率（伪随机：未触发多掷叠加，连续触发衰减）
            if (!PassProbability(ownerId, ruleConfig.Id, ruleConfig.TriggerPro))
            {
                LogHpChangeRuleSkip(ruleConfig, context, "概率未通过");
                return false;
            }

            // 4. 条件组（AND）
            if (!mConditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
            {
                if (context.Trigger == TriggerType.HpChange)
                {
                    BattleRuleHpChangeModifyAttrSupport.RemoveModifyAttrEffects(ruleConfig, context);
                }

                LogHpChangeRuleSkip(ruleConfig, context, "条件未满足");
                return false;
            }

            // 4.5 ComputerGroup（AND；空列表视为通过）。累加入口见 BattleRuleComputerSupport / BattleRule_ComputerGroup_12111_Context.md
            if (!mComputerManager.AreComputerGroupsMet(ruleConfig.ComputerGroupId, context))
            {
                LogHpChangeRuleSkip(ruleConfig, context, "Computer 未达标");
                return false;
            }

            // 5. 执行效果并记录运行态
            mEffectManager.ExecuteEffects(ruleConfig.EffectGroupId, ruleConfig, context);
            mComputerManager.ResetAfterTrigger(ruleConfig.ComputerGroupId, context);
            MarkTriggered(ownerId, ruleConfig.Id, context.CurrentTime);
            BattleRuleExecutionLogger.LogRuleExecuted(ownerId, ruleConfig, context, mConditionManager);
            return true;
        }

        private static void LogHpChangeRuleSkip(SkillRuleConfig ruleConfig, BattleRuleTriggerContext context, string reason)
        {
            if (context == null)
            {
                return;
            }

            if (context.Trigger == TriggerType.HpChange)
            {
                var hero = context.Source as SoldierCard;
                var heroInfo = hero != null
                    ? $"hero={hero.cardId}, hp={hero.CurrentHp}/{hero.MaxHp}"
                    : "hero=未知";
                LogTool.Log(
                    $"[BattleRuleService] HpChange 规则跳过: RuleId={ruleConfig.Id}, {heroInfo}, reason={reason}");
                return;
            }

            if (context.Trigger == TriggerType.RoundStart)
            {
                LogTool.Log(
                    $"[BattleRuleService] RoundStart 规则跳过: RuleId={ruleConfig.Id}, day={Global.Date.Value}, reason={reason}");
            }
        }

        /// <summary>
        /// 注册某个战斗单位的规则集合，并绑定该 Owner 专用的值提供器与效果执行器。
        /// </summary>
        /// <param name="resetRuntimeState">为 true 时清除该 Owner 已有冷却/次数记录。</param>
        public void RegisterOwnerRules(
            int ownerId,
            IEnumerable<int> ruleIds,
            IBattleRuleValueProvider valueProvider = null,
            IBattleRuleEffectExecutor effectExecutor = null,
            bool resetRuntimeState = true)
        {
            if (ownerId <= 0 || ruleIds == null)
            {
                return;
            }

            var validRuleIds = new List<int>();
            foreach (var ruleId in ruleIds)
            {
                if (mSkillRuleTable.GetOrDefault(ruleId) == null)
                {
                    LogTool.LogWarning($"[BattleRuleService] 规则不存在，注册跳过: {ruleId}");
                    continue;
                }

                validRuleIds.Add(ruleId);
            }

            mOwnerRuleMap[ownerId] = validRuleIds;
            mOwnerBindings[ownerId] = new OwnerRuleBinding
            {
                OwnerId = ownerId,
                ValueProvider = valueProvider,
                EffectExecutor = effectExecutor,
            };

            if (resetRuntimeState)
            {
                RemoveRuntimeStatesByOwner(ownerId);
            }

            mComputerManager.RebuildActiveComputerGroupIndex();

            LogTool.Log($"[BattleRuleService] 注册规则完成, Owner={ownerId}, Count={validRuleIds.Count}");
        }

        /// <summary>
        /// 注销指定 Owner 的规则绑定与运行态（单位离场或对象池复用时调用）。
        /// </summary>
        public void UnregisterOwner(int ownerId)
        {
            mOwnerRuleMap.Remove(ownerId);
            mOwnerBindings.Remove(ownerId);
            // 注销时同步清除该单位规则的运行态，避免脏状态污染复用对象。
            RemoveRuntimeStatesByOwner(ownerId);
        }

        /// <summary>
        /// 触发某个 Owner 在指定时机的全部规则（与 TryExecuteRule 流程一致，但遍历该 Owner 规则列表）。
        /// </summary>
        /// <returns>是否至少触发过一条规则。</returns>
        public bool TriggerForOwner(int ownerId, TriggerType trigger, BattleRuleTriggerContext context = null)
        {
            if (!mOwnerRuleMap.TryGetValue(ownerId, out var ruleIds) || ruleIds.Count == 0)
            {
                return false;
            }

            var binding = mOwnerBindings.TryGetValue(ownerId, out var ownerBinding) ? ownerBinding : null;
            var runtimeContext = BuildContext(ownerId, trigger, context, binding);
            var triggered = false;

            for (int i = 0; i < ruleIds.Count; i++)
            {
                var ruleConfig = mSkillRuleTable.GetOrDefault(ruleIds[i]);
                // 触发时机不匹配直接跳过，避免无效判断。
                if (ruleConfig == null || ruleConfig.Trigger != trigger)
                {
                    continue;
                }

                if (!CanTrigger(ownerId, ruleConfig, runtimeContext.CurrentTime))
                {
                    continue;
                }

                // 概率先判定，避免额外条件计算开销（伪随机按 Owner+Rule 独立记账）。
                if (!PassProbability(ownerId, ruleConfig.Id, ruleConfig.TriggerPro))
                {
                    continue;
                }

                if (!mConditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, runtimeContext))
                {
                    continue;
                }

                if (!mComputerManager.AreComputerGroupsMet(ruleConfig.ComputerGroupId, runtimeContext))
                {
                    continue;
                }

                mEffectManager.ExecuteEffects(ruleConfig.EffectGroupId, ruleConfig, runtimeContext);
                mComputerManager.ResetAfterTrigger(ruleConfig.ComputerGroupId, runtimeContext);
                MarkTriggered(ownerId, ruleConfig.Id, runtimeContext.CurrentTime);
                BattleRuleExecutionLogger.LogRuleExecuted(ownerId, ruleConfig, runtimeContext, mConditionManager);
                triggered = true;
            }

            return triggered;
        }

        /// <summary>
        /// 遍历所有已注册 Owner，在指定时机触发其规则；返回至少触发过一条规则的 Owner 数量。
        /// </summary>
        public int TriggerForAll(TriggerType trigger, BattleRuleTriggerContext sharedContext = null)
        {
            var triggeredCount = 0;
            var ownerIds = mOwnerRuleMap.Keys.ToList();
            for (int i = 0; i < ownerIds.Count; i++)
            {
                var ownerId = ownerIds[i];
                // 每个 owner 复制一份上下文，避免在循环内互相覆盖 OwnerId 等字段。
                var ownerContext = CloneContext(sharedContext, trigger, ownerId);
                if (TriggerForOwner(ownerId, trigger, ownerContext))
                {
                    triggeredCount++;
                }
            }

            return triggeredCount;
        }

        /// <summary>
        /// 按 RuleId 定向触发（用于强制触发或调试验证）。
        /// </summary>
        public bool TriggerRuleById(int ownerId, int ruleId, BattleRuleTriggerContext context = null)
        {
            if (!mOwnerRuleMap.TryGetValue(ownerId, out var ruleIds) || !ruleIds.Contains(ruleId))
            {
                return false;
            }

            var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);
            if (ruleConfig == null)
            {
                return false;
            }

            var binding = mOwnerBindings.TryGetValue(ownerId, out var ownerBinding) ? ownerBinding : null;
            var trigger = context?.Trigger ?? ruleConfig.Trigger;
            var runtimeContext = BuildContext(ownerId, trigger, context, binding);

            if (!CanTrigger(ownerId, ruleConfig, runtimeContext.CurrentTime))
            {
                return false;
            }

            if (!PassProbability(ownerId, ruleConfig.Id, ruleConfig.TriggerPro))
            {
                return false;
            }

            if (!mConditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, runtimeContext))
            {
                return false;
            }

            mEffectManager.ExecuteEffects(ruleConfig.EffectGroupId, ruleConfig, runtimeContext);
            mComputerManager.ResetAfterTrigger(ruleConfig.ComputerGroupId, runtimeContext);
            MarkTriggered(ownerId, ruleConfig.Id, runtimeContext.CurrentTime);
            BattleRuleExecutionLogger.LogRuleExecuted(ownerId, ruleConfig, runtimeContext, mConditionManager);
            return true;
        }

        /// <summary>
        /// 组装运行时上下文：补 Trigger/Owner/Time，并合并默认绑定。
        /// </summary>
        private BattleRuleTriggerContext BuildContext(
            int ownerId,
            TriggerType trigger,
            BattleRuleTriggerContext context,
            OwnerRuleBinding binding)
        {
            var runtimeContext = context ?? new BattleRuleTriggerContext(trigger, ownerId);
            runtimeContext.Trigger = trigger;
            runtimeContext.OwnerId = ownerId;
            runtimeContext.CurrentTime = runtimeContext.CurrentTime > 0f ? runtimeContext.CurrentTime : Time.time;

            if (runtimeContext.ValueProvider == null && binding != null)
            {
                runtimeContext.ValueProvider = binding.ValueProvider;
            }

            if (runtimeContext.EffectExecutor == null && binding != null)
            {
                runtimeContext.EffectExecutor = binding.EffectExecutor;
            }

            return runtimeContext;
        }

        /// <summary>
        /// 复制共享上下文用于全量触发，避免引用同一实例导致串数据。
        /// </summary>
        private BattleRuleTriggerContext CloneContext(
            BattleRuleTriggerContext sourceContext,
            TriggerType trigger,
            int ownerId)
        {
            if (sourceContext == null)
            {
                return null;
            }

            return new BattleRuleTriggerContext(trigger, ownerId)
            {
                Source = sourceContext.Source,
                Target = sourceContext.Target,
                CurrentTime = sourceContext.CurrentTime,
                ValueProvider = sourceContext.ValueProvider,
                EffectExecutor = sourceContext.EffectExecutor,
                StackSourceKey = sourceContext.StackSourceKey,
                ModifierSourceType = sourceContext.ModifierSourceType,
                ScopeHeroCardId = sourceContext.ScopeHeroCardId,
            };
        }

        /// <summary>
        /// 触发门槛检查：先次数上限，再冷却时间。
        /// </summary>
        private bool CanTrigger(int ownerId, SkillRuleConfig ruleConfig, float now)
        {
            var key = new RuleRuntimeKey(ownerId, ruleConfig.Id);
            if (!mRuleRuntimeStates.TryGetValue(key, out var state))
            {
                return true;
            }

            if (ruleConfig.MaxTriggerNum > 0 && state.TriggerCount >= ruleConfig.MaxTriggerNum)
            {
                return false;
            }

            if (ruleConfig.CoolDown > 0f && now - state.LastTriggerTime < ruleConfig.CoolDown)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 记录触发状态（用于后续冷却与次数判断）。
        /// </summary>
        private void MarkTriggered(int ownerId, int ruleId, float now)
        {
            var key = new RuleRuntimeKey(ownerId, ruleId);
            if (!mRuleRuntimeStates.TryGetValue(key, out var state))
            {
                state = new RuleRuntimeState();
                mRuleRuntimeStates.Add(key, state);
            }

            state.TriggerCount++;
            state.LastTriggerTime = now;
        }

        /// <summary>
        /// 概率判定：兼容 0-1 与 0-100 两种配置格式。
        /// <para>
        /// 0 / 100% 仍为确定性；其余走伪随机：
        /// 未触发按当前档位多掷叠加，触发后失败次数归 0；
        /// 连续触发衰减为 base → base/2 → base*7/30 → 5%。
        /// </para>
        /// </summary>
        private bool PassProbability(int ownerId, int ruleId, float triggerPro)
        {
            var probability = triggerPro > 1f ? triggerPro / 100f : triggerPro;
            probability = Mathf.Clamp01(probability);
            if (probability <= 0f)
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            var key = new RuleRuntimeKey(ownerId, ruleId);
            if (!mRuleRuntimeStates.TryGetValue(key, out var state))
            {
                state = new RuleRuntimeState();
                mRuleRuntimeStates.Add(key, state);
            }

            return state.PseudoRandom.Roll(probability, ownerId, ruleId, "BattleRule");
        }

        /// <summary>
        /// 清除某个 owner 的全部运行态。
        /// </summary>
        private void RemoveRuntimeStatesByOwner(int ownerId)
        {
            var removeKeys = new List<RuleRuntimeKey>();
            foreach (var pair in mRuleRuntimeStates)
            {
                if (pair.Key.OwnerId == ownerId)
                {
                    removeKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                mRuleRuntimeStates.Remove(removeKeys[i]);
            }
        }

        /// <summary>将 EffectManager 的效果事件透传给外部订阅者。</summary>
        private void HandleEffectTriggered(BattleRuleEffectExecuteInfo executeInfo)
        {
            OnRuleEffectTriggered?.Invoke(executeInfo);
        }

        /// <summary>解绑事件并清空全部运行时缓存。</summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (mEffectManager != null)
            {
                mEffectManager.OnEffectTriggered -= HandleEffectTriggered;
            }

            OnRuleEffectTriggered = null;
            mOwnerRuleMap.Clear();
            mRuleRuntimeStates.Clear();
            mOwnerBindings.Clear();

            if (mComputerManager != null)
            {
                mComputerManager.ClearAll();
                mComputerManager = null;
            }

            mSkillRuleTable = null;
            mConditionConfigTable = null;
            mConditionManager = null;
            mEffectManager = null;
        }

        public override void Reset()
        {
            base.Reset();
            mRuleRuntimeStates.Clear();
            if (mComputerManager != null)
            {
                mComputerManager.ResetBattleEnd();
                mComputerManager.RebuildActiveComputerGroupIndex();
            }
        }

        private static bool IsDeferredSpeakerWeaponRuleEntry(BattleLearnedRuleEntry entry)
        {
            return IsDeferredSpeakerModuleRuleEntry(entry);
        }

        private static bool IsDeferredSpeakerModuleRuleEntry(BattleLearnedRuleEntry entry)
        {
            if (entry.SourceType != AttributeSourceType.SpeakerSkin
                || string.IsNullOrEmpty(entry.StackSourceKey))
            {
                return false;
            }

            return entry.StackSourceKey.StartsWith("SpeakerWeapon_")
                || entry.StackSourceKey.StartsWith("SpeakerSkin_");
        }

        /// <summary>
        /// Owner 的依赖绑定（值提供器 + 效果执行器）。
        /// </summary>
        private sealed class OwnerRuleBinding
        {
            public int OwnerId;
            public IBattleRuleValueProvider ValueProvider;
            public IBattleRuleEffectExecutor EffectExecutor;
        }

        /// <summary>
        /// 单条规则运行态。
        /// </summary>
        private sealed class RuleRuntimeState
        {
            public int TriggerCount;
            public float LastTriggerTime;
            public readonly PseudoRandomProbability PseudoRandom = new PseudoRandomProbability();
        }

        /// <summary>
        /// 运行态字典键（OwnerId + RuleId）。
        /// </summary>
        private readonly struct RuleRuntimeKey : IEquatable<RuleRuntimeKey>
        {
            public int OwnerId { get; }
            public int RuleId { get; }

            public RuleRuntimeKey(int ownerId, int ruleId)
            {
                OwnerId = ownerId;
                RuleId = ruleId;
            }

            public bool Equals(RuleRuntimeKey other)
            {
                return OwnerId == other.OwnerId && RuleId == other.RuleId;
            }

            public override bool Equals(object obj)
            {
                return obj is RuleRuntimeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (OwnerId * 397) ^ RuleId;
                }
            }
        }
    }
}
