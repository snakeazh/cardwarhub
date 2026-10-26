using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent.Buff;
using FusionContinent.Skill.Config;
using FusionContinent.Skill.Handler;
using UnityEngine;

namespace FusionContinent.Monster.Mechanic
{
    internal sealed class RuleBasedMonsterMechanic : MonsterMechanicBase
    {
        private readonly List<MonsterMechanicRuleConfig> _rules;
        private readonly HashSet<int> _triggeredRuleIds = new HashSet<int>();
        private readonly List<Coroutine> _scheduledActions = new List<Coroutine>();
        private readonly Dictionary<int, float> _outgoingDamageModifiers = new Dictionary<int, float>();

        private float _previousHpPercent = 1f;
        private int _nextModifierId;
        private bool _cleanedUp;

        public RuleBasedMonsterMechanic(MonsterMechanicConfig config)
            : base(config)
        {
            _rules = config.RuleIds_Ref;
            ValidateConfig(config);
        }

        public override void OnSpawn()
        {
            LogTool.Log(LogPrefix() + " OnSpawn 开始检查 Spawn 规则");
            TriggerRules(MonsterMechanicTriggerType.Spawn);
        }

        public override void OnTakeDamage(ref DamageContext context)
        {
            bool hasAttacker = TryGetAttackerCareer(context, out CareerType attackerCareer);
            LogTool.Log(
                LogPrefix()
                + " OnTakeDamage"
                + " DamageType=" + context.DamageType
                + " Pipeline=" + context.PipelineType
                + " HasAttacker=" + hasAttacker
                + " AttackerCareer=" + (hasAttacker ? attackerCareer.ToString() : "None")
                + " FinalDamage=" + context.FinalDamage
                + " TrueDamage=" + context.TrueDamage);

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (!IsTakeDamageBeforeTrigger(rule.Trigger))
                {
                    continue;
                }

                bool careerMatched = MatchesTriggerCareer(rule.Trigger, hasAttacker, attackerCareer);
                bool damageMatched = MatchesDamageType(rule.DamageType, context);
                bool canTrigger = CanTrigger(rule);
                if (!careerMatched || !damageMatched || !canTrigger)
                {
                    LogTool.Log(
                        LogPrefix()
                        + " " + rule.Trigger + " 跳过"
                        + " RuleId=" + rule.Id
                        + " NeedDamageType=" + rule.DamageType
                        + " CareerMatched=" + careerMatched
                        + " AttackerCareer=" + (hasAttacker ? attackerCareer.ToString() : "None")
                        + " Matched=" + damageMatched
                        + " CanTrigger=" + canTrigger
                        + " TriggerOnce=" + rule.TriggerOnce);
                    continue;
                }

                LogTool.Log(
                    LogPrefix()
                    + " " + rule.Trigger + " 触发"
                    + " RuleId=" + rule.Id
                    + " Desc=" + rule.Desc
                    + " AttackerCareer=" + (hasAttacker ? attackerCareer.ToString() : "None")
                    + " ActionCount=" + rule.ActionIds_Ref.Count);

                MarkTriggered(rule);
                for (int actionIndex = 0; actionIndex < rule.ActionIds_Ref.Count; actionIndex++)
                {
                    var action = rule.ActionIds_Ref[actionIndex];
                    if (action.Type == MonsterMechanicActionType.ModifyIncomingDamage)
                    {
                        ApplyIncomingDamageModifier(action, ref context);
                    }
                    else
                    {
                        ScheduleOrExecute(action);
                    }
                }
            }
        }

        public override void OnDealDamage(ref int damage)
        {
            float modifier = 0f;
            foreach (var entry in _outgoingDamageModifiers)
            {
                modifier += entry.Value;
            }

            if (modifier == 0f && _outgoingDamageModifiers.Count == 0)
            {
                return;
            }

            int before = damage;
            damage = Mathf.Max(0, Mathf.RoundToInt(damage * Mathf.Max(0f, 1f + modifier)));
            LogTool.Log(
                LogPrefix()
                + " OnDealDamage 增伤生效"
                + " ModifierSum=" + modifier
                + " ActiveCount=" + _outgoingDamageModifiers.Count
                + " Before=" + before
                + " After=" + damage);
        }

        public override void OnHpChanged(float hpPercent)
        {
            LogTool.Log(
                LogPrefix()
                + " OnHpChanged"
                + " Prev=" + _previousHpPercent.ToString("F3")
                + " Curr=" + hpPercent.ToString("F3"));

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule.Trigger != MonsterMechanicTriggerType.HpBelow)
                {
                    continue;
                }

                bool crossed = _previousHpPercent > rule.HpThreshold && hpPercent <= rule.HpThreshold;
                bool canTrigger = CanTrigger(rule);
                if (!crossed || !canTrigger)
                {
                    LogTool.Log(
                        LogPrefix()
                        + " HpBelow 未触发"
                        + " RuleId=" + rule.Id
                        + " Threshold=" + rule.HpThreshold
                        + " Crossed=" + crossed
                        + " CanTrigger=" + canTrigger
                        + " AlreadyTriggered=" + _triggeredRuleIds.Contains(rule.Id));
                    continue;
                }

                LogTool.Log(
                    LogPrefix()
                    + " HpBelow 触发"
                    + " RuleId=" + rule.Id
                    + " Desc=" + rule.Desc
                    + " Threshold=" + rule.HpThreshold
                    + " Prev=" + _previousHpPercent.ToString("F3")
                    + " Curr=" + hpPercent.ToString("F3"));
                TriggerRule(rule);
            }

            _previousHpPercent = hpPercent;
        }

        public override void OnDeath()
        {
            LogTool.Log(LogPrefix() + " OnDeath 清理延迟动作并检查 Death 规则");
            CancelScheduledActions();
            TriggerRules(MonsterMechanicTriggerType.Death);
        }

        public override void OnCleanup()
        {
            if (!_cleanedUp)
            {
                _cleanedUp = true;
                LogTool.Log(LogPrefix() + " OnCleanup");
                CancelScheduledActions();
            }

            base.OnCleanup();
        }

        private void TriggerRules(MonsterMechanicTriggerType trigger)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule.Trigger == trigger && CanTrigger(rule))
                {
                    LogTool.Log(
                        LogPrefix()
                        + " TriggerRules 触发"
                        + " Trigger=" + trigger
                        + " RuleId=" + rule.Id
                        + " Desc=" + rule.Desc);
                    TriggerRule(rule);
                }
            }
        }

        private void TriggerRule(MonsterMechanicRuleConfig rule)
        {
            MarkTriggered(rule);
            for (int i = 0; i < rule.ActionIds_Ref.Count; i++)
            {
                ScheduleOrExecute(rule.ActionIds_Ref[i]);
            }
        }

        private bool CanTrigger(MonsterMechanicRuleConfig rule)
        {
            return rule.TriggerOnce == 0 || !_triggeredRuleIds.Contains(rule.Id);
        }

        private void MarkTriggered(MonsterMechanicRuleConfig rule)
        {
            if (rule.TriggerOnce != 0)
            {
                _triggeredRuleIds.Add(rule.Id);
                LogTool.Log(LogPrefix() + " MarkTriggeredOnce RuleId=" + rule.Id);
            }
        }

        private void ScheduleOrExecute(MonsterMechanicActionConfig action)
        {
            if (action.Delay <= 0f)
            {
                ExecuteAction(action);
                return;
            }

            LogTool.Log(
                LogPrefix()
                + " 延迟动作已调度"
                + " ActionId=" + action.Id
                + " Type=" + action.Type
                + " Delay=" + action.Delay
                + " Duration=" + action.Duration
                + " Value=" + action.Value
                + " Count=" + action.Count
                + " BuffID=" + action.BuffID);
            _scheduledActions.Add(Owner.StartCoroutine(ExecuteAfterDelay(action)));
        }

        private IEnumerator ExecuteAfterDelay(MonsterMechanicActionConfig action)
        {
            yield return new WaitForSeconds(action.Delay);
            if (!_cleanedUp)
            {
                LogTool.Log(
                    LogPrefix()
                    + " 延迟动作到期执行"
                    + " ActionId=" + action.Id
                    + " Type=" + action.Type);
                ExecuteAction(action);
            }
            else
            {
                LogTool.Log(
                    LogPrefix()
                    + " 延迟动作到期但已 Cleanup，跳过"
                    + " ActionId=" + action.Id);
            }
        }

        private void ExecuteAction(MonsterMechanicActionConfig action)
        {
            LogTool.Log(
                LogPrefix()
                + " ExecuteAction"
                + " ActionId=" + action.Id
                + " Desc=" + action.Desc
                + " Type=" + action.Type
                + " Target=" + action.Target
                + " Delay=" + action.Delay
                + " Duration=" + action.Duration
                + " Value=" + action.Value
                + " Count=" + action.Count
                + " BuffID=" + action.BuffID);

            switch (action.Type)
            {
                case MonsterMechanicActionType.ModifyOutgoingDamage:
                    AddOutgoingDamageModifier(action);
                    break;
                case MonsterMechanicActionType.AddBuff:
                    AddBuff(action);
                    break;
                case MonsterMechanicActionType.SilenceRandomSkills:
                    int silenced = GameLand.Default.SilenceRandomSkillCards(action.Count, action.Duration);
                    LogTool.Log(
                        LogPrefix()
                        + " SilenceRandomSkills 结果"
                        + " RequestCount=" + action.Count
                        + " SilencedCount=" + silenced
                        + " Duration=" + action.Duration);
                    break;
                case MonsterMechanicActionType.ModifyIncomingDamage:
                    throw new InvalidOperationException(
                        "ModifyIncomingDamage must execute during TakeDamageBefore triggers. ActionId="
                        + action.Id);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action.Type), action.Type, "Unsupported monster mechanic action.");
            }
        }

        private void ApplyIncomingDamageModifier(
            MonsterMechanicActionConfig action,
            ref DamageContext context)
        {
            float beforeFinal = context.FinalDamage;
            float beforeTrue = context.TrueDamage;
            float multiplier = Mathf.Max(0f, 1f + action.Value);
            context.FinalDamage *= multiplier;
            context.TrueDamage *= multiplier;
            LogTool.Log(
                LogPrefix()
                + " ModifyIncomingDamage"
                + " ActionId=" + action.Id
                + " Value=" + action.Value
                + " Multiplier=" + multiplier
                + " Final " + beforeFinal + "->" + context.FinalDamage
                + " True " + beforeTrue + "->" + context.TrueDamage);
        }

        private void AddOutgoingDamageModifier(MonsterMechanicActionConfig action)
        {
            int modifierId = ++_nextModifierId;
            _outgoingDamageModifiers.Add(modifierId, action.Value);
            LogTool.Log(
                LogPrefix()
                + " AddOutgoingDamageModifier"
                + " ModifierId=" + modifierId
                + " Value=" + action.Value
                + " Duration=" + action.Duration
                + " ActiveCount=" + _outgoingDamageModifiers.Count);
            if (action.Duration > 0f)
            {
                _scheduledActions.Add(Owner.StartCoroutine(
                    RemoveOutgoingDamageModifierAfter(modifierId, action.Duration)));
            }
        }

        private IEnumerator RemoveOutgoingDamageModifierAfter(int modifierId, float duration)
        {
            yield return new WaitForSeconds(duration);
            _outgoingDamageModifiers.Remove(modifierId);
            LogTool.Log(
                LogPrefix()
                + " RemoveOutgoingDamageModifier"
                + " ModifierId=" + modifierId
                + " RemainingCount=" + _outgoingDamageModifiers.Count);
        }

        private void AddBuff(MonsterMechanicActionConfig action)
        {
            bool applied = BuffApplier.ApplyBuff(action.BuffID, null, Owner);
            if (!applied)
            {
                throw new InvalidOperationException(
                    "Monster buff action could not apply buff. ActionId=" + action.Id
                    + ", BuffID=" + action.BuffID);
            }

            bool hasInvincibleBuff = Owner.BuffController != null
                && Owner.BuffController.HasBuff(BuffType.Invincible);
            bool abnormalInvincible = Owner.monsterData.monsterAbnormal != null
                && Owner.monsterData.monsterAbnormal.invincible;

            // Buff 已挂上但异常标记未同步时，补一次异常状态，避免无敌空挂仍扣血
            if (hasInvincibleBuff && !abnormalInvincible && Owner.monsterData.monsterAbnormal != null)
            {
                Owner.monsterData.monsterAbnormal.Apply(
                    UnusualStatusType.Invincible, 0f, global::Config.ValueType.Number);
                abnormalInvincible = Owner.monsterData.monsterAbnormal.invincible;
            }

            LogTool.Log(
                LogPrefix()
                + " AddBuff"
                + " ActionId=" + action.Id
                + " BuffID=" + action.BuffID
                + " Duration=" + action.Duration
                + " Value=" + action.Value
                + " HasInvincibleBuff=" + hasInvincibleBuff
                + " AbnormalInvincible=" + abnormalInvincible);
        }

        private bool MatchesDamageType(MonsterMechanicDamageType damageType, DamageContext context)
        {
            switch (damageType)
            {
                case MonsterMechanicDamageType.All:
                    return true;
                case MonsterMechanicDamageType.Physical:
                    return context.DamageType == TakeDamageType.PhysicalDamage;
                case MonsterMechanicDamageType.Magic:
                    return context.DamageType == TakeDamageType.MagicDamage
                           || context.DamageType == TakeDamageType.MagicDamageThunder;
                case MonsterMechanicDamageType.Real:
                    return context.DamageType == TakeDamageType.RealDamage;
                case MonsterMechanicDamageType.Burn:
                    return context.PipelineType == DamagePipelineType.BuffSkillDamage
                           && Owner.BuffController.HasBuff(BuffType.Burn);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(damageType), damageType, "Unsupported mechanic damage type.");
            }
        }

        private static bool IsTakeDamageBeforeTrigger(MonsterMechanicTriggerType trigger)
        {
            return trigger == MonsterMechanicTriggerType.TakeDamageBefore
                   || trigger == MonsterMechanicTriggerType.TakeMageDamageBefore
                   || trigger == MonsterMechanicTriggerType.TakeWarriorDamageBefore;
        }

        private static bool TryGetAttackerCareer(DamageContext context, out CareerType career)
        {
            if (context.Soldier == null)
            {
                career = default;
                return false;
            }

            career = context.Soldier.soldierBattle.CarType;
            return true;
        }

        private static bool MatchesTriggerCareer(
            MonsterMechanicTriggerType trigger,
            bool hasAttacker,
            CareerType attackerCareer)
        {
            switch (trigger)
            {
                case MonsterMechanicTriggerType.TakeDamageBefore:
                    return true;
                case MonsterMechanicTriggerType.TakeMageDamageBefore:
                    return hasAttacker && attackerCareer == CareerType.Wizard;
                case MonsterMechanicTriggerType.TakeWarriorDamageBefore:
                    return hasAttacker && attackerCareer == CareerType.Warrior;
                default:
                    return false;
            }
        }

        private string LogPrefix()
        {
            int monsterId = Owner != null ? Owner._cardId : 0;
            string uniqueId = Owner != null ? Owner.uniqueId : "-";
            return "[MonsterMechanic]"
                   + " MonsterId=" + monsterId
                   + " UniqueId=" + uniqueId
                   + " GroupId=" + Config.Id;
        }

        private void ValidateConfig(MonsterMechanicConfig config)
        {
            if (_rules == null || _rules.Count == 0)
            {
                throw new InvalidOperationException(
                    "Monster mechanic group has no rules. MechanicGroupId=" + config.Id);
            }

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule == null)
                {
                    throw new InvalidOperationException(
                        "Monster mechanic group contains a missing rule reference. MechanicGroupId=" + config.Id);
                }

                if (rule.TriggerOnce != 0 && rule.TriggerOnce != 1)
                {
                    throw new InvalidOperationException(
                        "TriggerOnce must be 0 or 1. RuleId=" + rule.Id);
                }

                if (rule.Trigger == MonsterMechanicTriggerType.HpBelow
                    && (rule.HpThreshold <= 0f || rule.HpThreshold > 1f))
                {
                    throw new InvalidOperationException(
                        "HpThreshold must be in (0, 1]. RuleId=" + rule.Id);
                }

                if (rule.ActionIds_Ref == null || rule.ActionIds_Ref.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Monster mechanic rule has no actions. RuleId=" + rule.Id);
                }

                for (int actionIndex = 0; actionIndex < rule.ActionIds_Ref.Count; actionIndex++)
                {
                    ValidateAction(rule, rule.ActionIds_Ref[actionIndex]);
                }
            }
        }

        private void ValidateAction(
            MonsterMechanicRuleConfig rule,
            MonsterMechanicActionConfig action)
        {
            if (action == null)
            {
                throw new InvalidOperationException(
                    "Monster mechanic rule contains a missing action reference. RuleId=" + rule.Id);
            }

            if (action.Delay < 0f || action.Duration < 0f)
            {
                throw new InvalidOperationException(
                    "Action delay and duration must be non-negative. ActionId=" + action.Id);
            }

            if (rule.Trigger == MonsterMechanicTriggerType.Death && action.Delay > 0f)
            {
                throw new InvalidOperationException(
                    "Death actions must start immediately because the monster is about to be recycled. ActionId="
                    + action.Id);
            }

            if (action.Type == MonsterMechanicActionType.ModifyIncomingDamage
                && (!IsTakeDamageBeforeTrigger(rule.Trigger)
                    || action.Delay != 0f
                    || action.Duration != 0f
                    || action.Target != MonsterMechanicTargetType.Self))
            {
                throw new InvalidOperationException(
                    "ModifyIncomingDamage requires TakeDamageBefore/TakeMageDamageBefore/TakeWarriorDamageBefore, Self, Delay=0, Duration=0. ActionId="
                    + action.Id);
            }

            if (action.Type == MonsterMechanicActionType.ModifyOutgoingDamage
                && (action.Target != MonsterMechanicTargetType.Self || action.Duration <= 0f))
            {
                throw new InvalidOperationException(
                    "ModifyOutgoingDamage requires Self and Duration>0. ActionId=" + action.Id);
            }

            if (action.Type == MonsterMechanicActionType.AddBuff
                && (action.Target != MonsterMechanicTargetType.Self || action.BuffID <= 0))
            {
                throw new InvalidOperationException(
                    "AddBuff requires Self and BuffID>0. ActionId=" + action.Id
                    + ", BuffID=" + action.BuffID);
            }

            if (action.Type == MonsterMechanicActionType.AddBuff)
            {
                var effectConfigs = SkillBuffTemplateProvider.GetEffects(action.BuffID);
                if (effectConfigs == null || effectConfigs.Count == 0)
                {
                    throw new InvalidOperationException(
                        "AddBuff BuffID has no template in SkillBuffTemplateLibrary. ActionId="
                        + action.Id
                        + ", BuffID=" + action.BuffID);
                }
            }

            if (action.Type == MonsterMechanicActionType.SilenceRandomSkills
                && (action.Target != MonsterMechanicTargetType.PlayerSkillDeck
                    || action.Duration <= 0f
                    || action.Count <= 0))
            {
                throw new InvalidOperationException(
                    "SilenceRandomSkills requires PlayerSkillDeck, Duration>0 and Count>0. ActionId="
                    + action.Id);
            }
        }

        private void CancelScheduledActions()
        {
            if (_scheduledActions.Count > 0 || _outgoingDamageModifiers.Count > 0)
            {
                LogTool.Log(
                    LogPrefix()
                    + " CancelScheduledActions"
                    + " ScheduledCount=" + _scheduledActions.Count
                    + " OutgoingModifierCount=" + _outgoingDamageModifiers.Count);
            }

            for (int i = 0; i < _scheduledActions.Count; i++)
            {
                Owner.StopCoroutine(_scheduledActions[i]);
            }

            _scheduledActions.Clear();
            _outgoingDamageModifiers.Clear();
        }
    }
}
