using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Skill;
using Unity.Profiling;

namespace FusionContinent
{
    /// <summary>
    /// Buff 控制器
    /// 纯 C# 类，不继承 MonoBehaviour，由外部驱动 Update(dt)
    /// </summary>
    public class BuffController
    {
        private readonly object _owner;
        private readonly List<IBuff> _buffs = new List<IBuff>();

        public BuffController(object owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 获取所有者
        /// </summary>
        public object Owner => _owner;

        /// <summary>
        /// 添加一个 Buff，自动调用 OnAdd
        /// - 若已有同类型 Buff（BuffId + SourceKey 匹配）：未达上限时层数+1并调用 OnStackUp，达到上限则刷新时间
        /// - 若无同类型 Buff：直接添加并调用 OnAdd
        /// Once 类型在 OnAdd 后直接移除（不进入 Update 循环）
        /// </summary>
        public void AddBuff(IBuff buff)
        {
            if (buff == null) return;

            // 查找已有的同类 Buff：按 BuffId + SourceKey 联合判重
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerIterateBuff))
            {
            for (int i = 0; i < _buffs.Count; i++)
            {
                var existing = _buffs[i];
                bool sameBuff = buff.BuffId > 0 && existing.BuffId > 0
                    ? existing.BuffId == buff.BuffId && existing.SourceKey == buff.SourceKey
                    : existing.Type == buff.Type && existing.SourceKey == buff.SourceKey;

                if (sameBuff)
                {
                    // 未达上限：层数+1
                    if (buff.MaxStack <= 0 || existing.Stack < buff.MaxStack)
                    {
                        existing.Stack++;
                        existing.OnStackUp(_owner);
                        LogTool.L($"[BuffController] Buff叠加 → type={buff.Type}, buffId={buff.BuffId}, sourceKey={buff.SourceKey}, stack={existing.Stack}");
                    }
                    // 达到上限：刷新时间
                    else
                    {
                        existing.OnRefreshDuration(_owner);
                        LogTool.L($"[BuffController] Buff达到上限刷新时间 → type={buff.Type}, buffId={buff.BuffId}, sourceKey={buff.SourceKey}, maxStack={buff.MaxStack}");
                    }

                    if (existing.Type == BuffType.Burn && i < m_IntervalAccumulators.Count)
                    {
                        m_IntervalAccumulators[i] = 0f;
                    }

                    // // 新 buff 没用到，OnAdd 中可能播放的 VFX 需要回收
                    // if (buff is BuffBase buffBase)
                    // {
                    //     buffBase.OnRemove(_owner);
                    // }

                    return;
                }
            }
            }

            // 无同类型 Buff，正常添加
            _buffs.Add(buff);
            m_IntervalAccumulators.Add(0f);
            buff.OnAdd(_owner);
        }

        /// <summary>
        /// 移除指定 Buff，自动调用 OnRemove
        /// </summary>
        public bool RemoveBuff(IBuff buff)
        {
            if (buff == null) return false;
            int index = _buffs.IndexOf(buff);
            if (index < 0) return false;
            // LogTool.E($"移除了buff 类型为 {buff.Type} 施加者为{buff.Caster} buff拥有者为{_owner}");
            buff.OnRemove(_owner);
            if (index < _buffs.Count && ReferenceEquals(_buffs[index], buff))
            {
                _buffs.RemoveAt(index);
                if (m_IntervalAccumulators.Count > index)
                    m_IntervalAccumulators.RemoveAt(index);
            }
            else
            {
                index = _buffs.IndexOf(buff);
                if (index >= 0)
                {
                    _buffs.RemoveAt(index);
                    if (m_IntervalAccumulators.Count > index)
                        m_IntervalAccumulators.RemoveAt(index);
                }
            }

            if (buff.BuffId == 10001)
            {
                ISkill skillCard = null;
                var skillContext = buff.SkillContext;
                if (skillContext?.Caster != null)
                {
                    var soldierCard = skillContext.Caster.GetComponent<SoldierCard>();
                    if (soldierCard != null)
                    {
                        skillCard = soldierCard.GetSkillById(skillContext.SkillId);
                    }
                }

                if (skillCard?.Parent?.BaseAttributes != null)
                {
                    skillCard.Parent.BaseAttributes.RemoveBuff(buff.BuffId.ToString());
                }
            }

            if (_owner is UnityEngine.Component ownerComponent)
            {
                Game.Battle.BattleRuleComputerSupport.OnBuffRemoved(ownerComponent, buff.BuffId);
            }

            return true;
        }

        /// <summary>
        /// 移除指定类型的所有 Buff
        /// </summary>
        public void RemoveBuffsOfType(BuffType type)
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i].Type == type)
                {
                    _buffs[i].OnRemove(_owner);
                    _buffs.RemoveAt(i);
                    if (m_IntervalAccumulators.Count > i)
                        m_IntervalAccumulators.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 获取指定类型的所有 Buff
        /// </summary>
        public IEnumerable<IBuff> GetBuffs(BuffType type)
        {
            foreach (var buff in _buffs)
            {
                if (buff.Type == type) yield return buff;
            }
        }

        /// <summary>
        /// 检查是否拥有指定类型的 Buff
        /// </summary>
        public bool HasBuff(BuffType type)
        {
            foreach (var buff in _buffs)
            {
                if (buff.Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有 Buff
        /// </summary>
        public IReadOnlyList<IBuff> GetAllBuffs() => _buffs;

        /// <summary>
        /// 更新所有 Buff（外部每帧/每 tick 调用）
        /// </summary>
        public void Update(float dt)
        {
            if (_buffs.Count == 0) return;

            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (i >= _buffs.Count) continue;
                var buff = _buffs[i];
                buff.OnTick(_owner, dt);

                // OnTick / OnIntervalTick 可能触发 Die、ClearAll 等，导致列表被清空或索引失效
                if (!IsBuffAtIndex(i, buff))
                    continue;

                if (buff.TickInterval > 0)
                {
                    EnsureIntervalAccumulatorSlot(i);
                    if (!IsBuffAtIndex(i, buff))
                        continue;

                    m_IntervalAccumulators[i] += dt;
                    if (m_IntervalAccumulators[i] >= buff.TickInterval)
                    {
                        var accumulated = m_IntervalAccumulators[i];
                        buff.OnIntervalTick(_owner, accumulated);

                        if (!IsBuffAtIndex(i, buff))
                            continue;

                        m_IntervalAccumulators[i] = 0f;
                    }
                }

                if (!IsBuffAtIndex(i, buff))
                    continue;

                if (buff.IsFinished)
                {
                    RemoveBuff(buff);
                }
            }
        }

        private bool IsBuffAtIndex(int index, IBuff buff)
        {
            return index >= 0
                   && index < _buffs.Count
                   && index < m_IntervalAccumulators.Count
                   && ReferenceEquals(_buffs[index], buff);
        }

        private void EnsureIntervalAccumulatorSlot(int index)
        {
            while (m_IntervalAccumulators.Count <= index)
            {
                m_IntervalAccumulators.Add(0f);
            }
        }

        private List<float> m_IntervalAccumulators = new List<float>();

        /// <summary>
        /// 清理所有 Buff，自动调用 OnRemove
        /// </summary>
        public virtual void ClearAll()
        {
            while (_buffs.Count > 0)
            {
                var lastIndex = _buffs.Count - 1;
                var buff = _buffs[lastIndex];
                if (!RemoveBuff(buff) && lastIndex < _buffs.Count)
                {
                    _buffs.RemoveAt(lastIndex);
                    if (m_IntervalAccumulators.Count > lastIndex)
                        m_IntervalAccumulators.RemoveAt(lastIndex);
                }
            }
            _buffs.Clear();
            m_IntervalAccumulators.Clear();
        }
    }
}
