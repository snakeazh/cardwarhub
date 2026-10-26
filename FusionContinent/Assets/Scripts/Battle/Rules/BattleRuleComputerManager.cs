using Config;

using FusionContinent;

using Manager;

using System.Collections.Generic;

using UnityEngine;



namespace Game.Battle

{
    /// <summary>

    /// ComputerGroup 计数管理器：累加、时间窗口判定、触发后/超时/Buff/战斗结束重置。

    /// 与 SkillRuleConfig.ComputerGroupId 配合，在 TryExecuteRule 中作为效果门槛。

    /// </summary>

    public class BattleRuleComputerManager
    {

        private readonly TbComputerGroupConfig mComputerGroupTable;

        private readonly TbSkillRuleConfig mSkillRuleTable;

        private readonly Dictionary<ComputerCounterKey, BattleRuleCounterData> mCounters = new Dictionary<ComputerCounterKey, BattleRuleCounterData>();

        private readonly Dictionary<int, List<ComputerCounterKey>> mOwnerCounterKeys = new Dictionary<int, List<ComputerCounterKey>>();

        private HashSet<int> mActiveComputerGroupIds;



        public BattleRuleComputerManager(TbComputerGroupConfig computerGroupTable, TbSkillRuleConfig skillRuleTable)
        {

            mComputerGroupTable = computerGroupTable;

            mSkillRuleTable = skillRuleTable;

            RebuildActiveComputerGroupIndex();

        }



        public void RebuildActiveComputerGroupIndex()

        {

            mActiveComputerGroupIds = new HashSet<int>();

            foreach (var ruleId in AttributeManager.BattleLearnedSkillRuleIds)

            {

                var ruleConfig = mSkillRuleTable.GetOrDefault(ruleId);

                if (ruleConfig == null || ruleConfig.ComputerGroupId == null)

                {

                    continue;

                }



                for (int i = 0; i < ruleConfig.ComputerGroupId.Count; i++)
                {
                    mActiveComputerGroupIds.Add(ruleConfig.ComputerGroupId[i]);
                }

            }

        }



        /// <summary>

        /// 累加阶段：CounterTriggerType 事件发生时，为匹配的 ComputerGroup 增加计数。

        /// </summary>

        public void OnCounterEvent(CounterTriggerType triggerType, BattleRuleTriggerContext context)
        {
            OnCounterEvent(triggerType, context, addValueOverride: 0f);
        }

        /// <param name="addValueOverride">
        /// &gt;0 时覆盖配表 AddValue（如战斗币消耗数量）；否则使用配表 AddValue。
        /// </param>
        public void OnCounterEvent(
            CounterTriggerType triggerType,
            BattleRuleTriggerContext context,
            float addValueOverride)
        {
            if (context == null || mActiveComputerGroupIds == null || mActiveComputerGroupIds.Count == 0)
            {
                return;
            }

            if (triggerType == CounterTriggerType.Custom)
            {
                LogTool.LogWarning("[BattleRuleComputerManager] Custom 计数请使用 OnCustomCounterEvent(customEventId, context)");
                return;
            }

            var now = context.CurrentTime > 0f ? context.CurrentTime : Time.time;
            foreach (var groupId in mActiveComputerGroupIds)
            {
                var config = mComputerGroupTable.GetOrDefault(groupId);
                if (config == null || config.TriggerType != triggerType)
                {
                    continue;
                }

                if (!IsCounterTypeMatchingTrigger(config, triggerType))
                {
                    continue;
                }

                if (!TryResolveOwnerInstanceId(config, context, out var ownerInstanceId))
                {
                    continue;
                }

                AddCounter(ownerInstanceId, groupId, config, context, now, addValueOverride);
            }
        }



        /// <summary>

        /// Custom 计数：ResetParam 为自定义事件 Id，与配表 TriggerType/CounterType=Custom 对应。

        /// </summary>

        public void OnCustomCounterEvent(int customEventId, BattleRuleTriggerContext context)

        {

            if (context == null || mActiveComputerGroupIds == null || mActiveComputerGroupIds.Count == 0)
            {
                return;
            }

            var now = context.CurrentTime > 0f ? context.CurrentTime : Time.time;

            foreach (var groupId in mActiveComputerGroupIds)

            {

                var config = mComputerGroupTable.GetOrDefault(groupId);

                if (config == null || config.TriggerType != CounterTriggerType.Custom)

                {

                    continue;

                }



                if (!IsCounterTypeMatchingTrigger(config, CounterTriggerType.Custom))

                {

                    continue;

                }



                if ((int)config.ResetParam != customEventId)

                {

                    continue;

                }



                if (!TryResolveOwnerInstanceId(config, context, out var ownerInstanceId))

                {

                    continue;

                }



                AddCounter(ownerInstanceId, groupId, config, context, now);

            }

        }



        /// <summary>

        /// 判定阶段：ComputerGroupId 列表 AND；空列表视为通过。

        /// </summary>

        public bool AreComputerGroupsMet(IReadOnlyList<int> groupIds, BattleRuleTriggerContext context)

        {

            if (groupIds == null || groupIds.Count == 0)

            {

                return true;

            }



            if (context == null)

            {

                return false;

            }



            var now = context.CurrentTime > 0f ? context.CurrentTime : Time.time;

            for (int i = 0; i < groupIds.Count; i++)

            {

                var config = mComputerGroupTable.GetOrDefault(groupIds[i]);

                if (config == null)

                {

                    LogTool.LogWarning($"[BattleRuleComputerManager] ComputerGroup 不存在: {groupIds[i]}");

                    return false;

                }



                if (!TryResolveOwnerInstanceId(config, context, out var ownerInstanceId))

                {

                    return false;

                }



                var current = GetCurrentCount(ownerInstanceId, groupIds[i], config, now);

                if (current < config.MaxValue)

                {

                    return false;

                }

            }



            return true;

        }



        /// <summary>

        /// 效果执行成功后，按 ResetType 重置计数。

        /// </summary>

        public void ResetAfterTrigger(IReadOnlyList<int> groupIds, BattleRuleTriggerContext context)

        {

            if (groupIds == null || groupIds.Count == 0 || context == null)

            {

                return;

            }



            for (int i = 0; i < groupIds.Count; i++)

            {

                var config = mComputerGroupTable.GetOrDefault(groupIds[i]);

                if (config == null || config.ResetType != CounterResetType.AfterTrigger)

                {

                    continue;

                }



                if (!TryResolveOwnerInstanceId(config, context, out var ownerInstanceId))
                {
                    continue;
                }

                // 战斗币/计时：扣减阈值，保留超额进度；其余 AfterTrigger 清零。
                if (config.TriggerType == CounterTriggerType.BattleGoldNum
                    || config.TriggerType == CounterTriggerType.EverySecond)
                {
                    SubtractCounter(ownerInstanceId, groupIds[i], config.MaxValue > 0 ? config.MaxValue : 0);
                }
                else
                {
                    ResetCounter(ownerInstanceId, groupIds[i]);
                }
            }
        }

        private void SubtractCounter(int ownerInstanceId, int groupId, int amount)
        {
            var key = new ComputerCounterKey(ownerInstanceId, groupId);
            if (!mCounters.TryGetValue(key, out var data))
            {
                return;
            }

            if (amount <= 0)
            {
                ClearCounterData(data);
                return;
            }

            data.Count = Mathf.Max(0, data.Count - amount);
            data.TriggerTimes.Clear();
            if (data.Count > 0)
            {
                data.TriggerTimes.Enqueue(Time.time);
            }
        }



        /// <summary>

        /// 战斗结束：仅清除 ResetType=BattleEnd 的计数。

        /// </summary>

        public void ResetBattleEnd()

        {

            var removeKeys = new List<ComputerCounterKey>();

            foreach (var pair in mCounters)

            {

                var config = mComputerGroupTable.GetOrDefault(pair.Key.ComputerGroupId);

                if (config != null && config.ResetType == CounterResetType.BattleEnd)

                {

                    removeKeys.Add(pair.Key);

                }

            }



            for (int i = 0; i < removeKeys.Count; i++)

            {

                RemoveCounterEntry(removeKeys[i]);

            }

        }



        /// <summary>

        /// Buff 移除：ResetType=BuffRemove 且 ResetParam 匹配 buffId（0 表示任意 Buff）。

        /// </summary>

        public void OnBuffRemoved(int ownerInstanceId, int buffId)

        {

            var removeKeys = new List<ComputerCounterKey>();

            foreach (var pair in mCounters)

            {

                if (pair.Key.OwnerInstanceId != ownerInstanceId)

                {

                    continue;

                }



                var config = mComputerGroupTable.GetOrDefault(pair.Key.ComputerGroupId);

                if (config == null || config.ResetType != CounterResetType.BuffRemove)

                {

                    continue;

                }



                if (config.ResetParam > 0f && (int)config.ResetParam != buffId)

                {

                    continue;

                }



                removeKeys.Add(pair.Key);

            }



            for (int i = 0; i < removeKeys.Count; i++)

            {

                RemoveCounterEntry(removeKeys[i]);

            }

        }



        /// <summary>

        /// 实体离场（怪物死亡、英灵销毁等）：移除该实体关联的全部计数。

        /// </summary>

        public void RemoveCountersByOwnerInstanceId(int ownerInstanceId)

        {

            if (!mOwnerCounterKeys.TryGetValue(ownerInstanceId, out var keys))

            {

                return;

            }



            for (int i = keys.Count - 1; i >= 0; i--)

            {

                RemoveCounterEntry(keys[i]);

            }



            mOwnerCounterKeys.Remove(ownerInstanceId);

        }



        public void ClearCounters()

        {

            mCounters.Clear();

            mOwnerCounterKeys.Clear();

        }



        public void ClearAll()

        {

            ClearCounters();

            if (mActiveComputerGroupIds != null)

            {

                mActiveComputerGroupIds.Clear();

            }

        }



        /// <summary>

        /// ComboCount 专用：将连击层数同步写入对应 ComputerGroup 计数。

        /// </summary>

        public void SyncComboCount(int comboCount, BattleRuleTriggerContext context)

        {

            if (context == null || mActiveComputerGroupIds == null)

            {

                return;

            }



            var now = context.CurrentTime > 0f ? context.CurrentTime : Time.time;

            foreach (var groupId in mActiveComputerGroupIds)

            {

                var config = mComputerGroupTable.GetOrDefault(groupId);

                if (config == null || config.TriggerType != CounterTriggerType.ComboCount)

                {

                    continue;

                }



                if (!IsCounterTypeMatchingTrigger(config, CounterTriggerType.ComboCount))

                {

                    continue;

                }



                if (!TryResolveOwnerInstanceId(config, context, out var ownerInstanceId))

                {

                    continue;

                }



                var key = new ComputerCounterKey(ownerInstanceId, groupId);

                if (!mCounters.TryGetValue(key, out var data))

                {

                    data = new BattleRuleCounterData();

                    mCounters.Add(key, data);

                    TrackOwnerKeys(ownerInstanceId, key, config, context);

                }



                data.Count = comboCount;

                data.LastActivityTime = now;

                data.TriggerTimes.Clear();

                if (comboCount > 0)

                {

                    data.TriggerTimes.Enqueue(now);

                }

            }

        }



        private void AddCounter(
            int ownerInstanceId,
            int groupId,
            ComputerGroupConfig config,
            BattleRuleTriggerContext context,
            float now,
            float addValueOverride = 0f)
        {
            var key = new ComputerCounterKey(ownerInstanceId, groupId);
            if (!mCounters.TryGetValue(key, out var data))
            {
                data = new BattleRuleCounterData();
                mCounters.Add(key, data);
                TrackOwnerKeys(ownerInstanceId, key, config, context);
            }

            TryApplyTimeOutReset(data, config, now);

            var addValue = addValueOverride > 0f
                ? addValueOverride
                : (config.AddValue > 0 ? config.AddValue : 1f);
            // 窗口计数需逐条入队；无窗口时直接累加 Count，避免大额战斗币循环。
            if (config.CounterTime > 0f)
            {
                var steps = Mathf.Max(1, Mathf.RoundToInt(addValue));
                for (int i = 0; i < steps; i++)
                {
                    data.Count++;
                    data.TriggerTimes.Enqueue(now);
                }

                PruneExpiredTimes(data, config.CounterTime, now);
            }
            else
            {
                data.Count += Mathf.Max(1, Mathf.RoundToInt(addValue));
            }

            data.LastActivityTime = now;
            // 不在此处按 MaxValue 截断：大额战斗币消耗需保留超额，触发后按 MaxValue 扣减。
        }



        private int GetCurrentCount(int ownerInstanceId, int groupId, ComputerGroupConfig config, float now)

        {

            var key = new ComputerCounterKey(ownerInstanceId, groupId);

            if (!mCounters.TryGetValue(key, out var data))

            {

                return 0;

            }



            TryApplyTimeOutReset(data, config, now);



            if (config.CounterTime > 0f)

            {

                PruneExpiredTimes(data, config.CounterTime, now);

                return data.TriggerTimes.Count;

            }



            return data.Count;

        }



        private void ResetCounter(int ownerInstanceId, int groupId)

        {

            var key = new ComputerCounterKey(ownerInstanceId, groupId);

            if (!mCounters.TryGetValue(key, out var data))

            {

                return;

            }



            ClearCounterData(data);

        }



        private void RemoveCounterEntry(ComputerCounterKey key)

        {

            mCounters.Remove(key);

            UntrackKey(key);

        }



        private static void ClearCounterData(BattleRuleCounterData data)

        {

            data.Count = 0;

            data.LastActivityTime = 0f;

            data.TriggerTimes.Clear();

        }



        private static bool TryApplyTimeOutReset(BattleRuleCounterData data, ComputerGroupConfig config, float now)
        {
            if (config.ResetType != CounterResetType.TimeOut || config.ResetParam <= 0f)
            {
                return false;
            }



            if (data.Count == 0 && data.TriggerTimes.Count == 0)

            {

                return false;

            }



            if (data.LastActivityTime <= 0f)

            {

                return false;

            }



            if (now - data.LastActivityTime <= config.ResetParam)

            {

                return false;

            }



            ClearCounterData(data);

            return true;

        }



        private static void PruneExpiredTimes(BattleRuleCounterData data, float windowSeconds, float now)

        {

            while (data.TriggerTimes.Count > 0)

            {

                var oldest = data.TriggerTimes.Peek();

                if (now - oldest <= windowSeconds)

                {

                    break;

                }



                data.TriggerTimes.Dequeue();

            }



            data.Count = data.TriggerTimes.Count;

        }



        private static bool IsCounterTypeMatchingTrigger(ComputerGroupConfig config, CounterTriggerType triggerType)
        {
            if (AreCounterTypesCompatible(config.CounterType, triggerType))
            {
                return true;
            }

            LogTool.LogWarning(
                $"[BattleRuleComputerManager] CounterType/TriggerType 不一致: groupId={config.Id}, counter={(int)config.CounterType}, trigger={(int)triggerType}");
            return false;
        }

        /// <summary>
        /// CounterType 与 CounterTriggerType 枚举值在 Custom 之后已不对齐，按语义配对。
        /// </summary>
        private static bool AreCounterTypesCompatible(CounterType counterType, CounterTriggerType triggerType)
        {
            if ((int)counterType == (int)triggerType
                && triggerType != CounterTriggerType.Custom
                && triggerType != CounterTriggerType.round
                && triggerType != CounterTriggerType.BattleGoldNum
                && triggerType != CounterTriggerType.EverySecond)
            {
                return true;
            }

            switch (triggerType)
            {
                case CounterTriggerType.Custom:
                    return counterType == CounterType.Custom;
                case CounterTriggerType.round:
                    return counterType == CounterType.round;
                case CounterTriggerType.BattleGoldNum:
                    return counterType == CounterType.BattleGoldCount;
                case CounterTriggerType.EverySecond:
                    return counterType == CounterType.TimeCount;
                case CounterTriggerType.PurchaseEffect:
                    return counterType == CounterType.PurchaseEffectNum;
                case CounterTriggerType.PurchaseExclusiveEffect:
                    return counterType == CounterType.PurchaseExclusiveEffectNum;
                default:
                    return (int)counterType == (int)triggerType;
            }
        }



        private void TrackOwnerKeys(int ownerInstanceId, ComputerCounterKey key, ComputerGroupConfig config, BattleRuleTriggerContext context)

        {

            AddOwnerKey(ownerInstanceId, key);



            if (config.OwnerType != CounterOwnerType.Pair)

            {

                return;

            }



            var sourceId = ResolveSourceInstanceId(context);

            var targetId = ResolveTargetInstanceId(context);

            if (sourceId != 0 && sourceId != ownerInstanceId)

            {

                AddOwnerKey(sourceId, key);

            }



            if (targetId != 0 && targetId != ownerInstanceId)

            {

                AddOwnerKey(targetId, key);

            }

        }



        private void AddOwnerKey(int ownerInstanceId, ComputerCounterKey key)

        {

            if (!mOwnerCounterKeys.TryGetValue(ownerInstanceId, out var keys))

            {

                keys = new List<ComputerCounterKey>();

                mOwnerCounterKeys.Add(ownerInstanceId, keys);

            }



            if (!keys.Contains(key))

            {

                keys.Add(key);

            }

        }



        private void UntrackKey(ComputerCounterKey key)

        {

            var emptyOwners = new List<int>();

            foreach (var pair in mOwnerCounterKeys)

            {

                pair.Value.Remove(key);

                if (pair.Value.Count == 0)

                {

                    emptyOwners.Add(pair.Key);

                }

            }



            for (int i = 0; i < emptyOwners.Count; i++)

            {

                mOwnerCounterKeys.Remove(emptyOwners[i]);

            }

        }



        private static bool TryResolveOwnerInstanceId(

            ComputerGroupConfig config,

            BattleRuleTriggerContext context,

            out int ownerInstanceId)

        {

            ownerInstanceId = 0;

            var soldier = ResolveSoldier(context);

            var monster = ResolveMonster(context);



            switch (config.OwnerType)

            {

                case CounterOwnerType.Self:
                    if (config.TriggerType == CounterTriggerType.KillCount)
                    {
                        if (soldier == null)
                        {
                            return false;
                        }

                        if (BattleRuleHeroScopeSupport.UsesPerSoldierKillCounter(config.Id, soldier.cardId))
                        {
                            ownerInstanceId = soldier.GetInstanceID();
                            return true;
                        }

                        if (BattleRuleHeroScopeSupport.ShouldSkipGlobalKillCount(config.Id, soldier))
                        {
                            return false;
                        }

                        ownerInstanceId = BattleRuleDamageSupport.GlobalBattleOwnerId;
                        return true;
                    }

                    if (config.TriggerType == CounterTriggerType.round
                        || config.TriggerType == CounterTriggerType.BattleGoldNum
                        || config.TriggerType == CounterTriggerType.EverySecond
                        || config.TriggerType == CounterTriggerType.PurchaseEffect
                        || config.TriggerType == CounterTriggerType.PurchaseExclusiveEffect)
                    {
                        ownerInstanceId = BattleRuleDamageSupport.GlobalBattleOwnerId;
                        return true;
                    }

                    if (soldier == null)
                    {
                        return false;
                    }

                    ownerInstanceId = soldier.GetInstanceID();
                    return true;



                case CounterOwnerType.Target:

                    if (monster == null)

                    {

                        return false;

                    }



                    ownerInstanceId = monster.GetInstanceID();

                    return true;



                case CounterOwnerType.Pair:

                    var sourceId = ResolveSourceInstanceId(context);

                    var targetId = ResolveTargetInstanceId(context);

                    if (sourceId == 0 || targetId == 0)

                    {

                        return false;

                    }



                    ownerInstanceId = HashPair(sourceId, targetId);

                    return true;



                default:

                    return false;

            }

        }



        private static SoldierCard ResolveSoldier(BattleRuleTriggerContext context)

        {

            if (context.Source is SoldierCard sourceSoldier)

            {

                return sourceSoldier;

            }



            if (context.Target is SoldierCard targetSoldier)

            {

                return targetSoldier;

            }



            if (context.DamageContext != null && context.DamageContext.Soldier != null)

            {

                return context.DamageContext.Soldier;

            }



            return null;

        }



        private static MonsterCard ResolveMonster(BattleRuleTriggerContext context)

        {

            if (context.Target is MonsterCard targetMonster)

            {

                return targetMonster;

            }



            if (context.Source is MonsterCard sourceMonster)

            {

                return sourceMonster;

            }



            if (context.DamageContext != null && context.DamageContext.Monster != null)

            {

                return context.DamageContext.Monster;

            }



            return null;

        }



        private static int ResolveSourceInstanceId(BattleRuleTriggerContext context)

        {

            if (context.Source is Component sourceComponent)

            {

                return sourceComponent.GetInstanceID();

            }



            return 0;

        }



        private static int ResolveTargetInstanceId(BattleRuleTriggerContext context)

        {

            if (context.Target is Component targetComponent)

            {

                return targetComponent.GetInstanceID();

            }



            return 0;

        }



        private static int HashPair(int a, int b)

        {

            unchecked

            {

                return (a * 397) ^ b;

            }

        }



        private readonly struct ComputerCounterKey

        {

            public readonly int OwnerInstanceId;

            public readonly int ComputerGroupId;



            public ComputerCounterKey(int ownerInstanceId, int computerGroupId)

            {

                OwnerInstanceId = ownerInstanceId;

                ComputerGroupId = computerGroupId;

            }

        }

    }

}


