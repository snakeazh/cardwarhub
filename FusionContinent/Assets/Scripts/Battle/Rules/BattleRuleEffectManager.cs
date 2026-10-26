using Config;
using FusionContinent;
using System;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 规则效果管理器：按 EffectGroupId 读取并触发效果执行。
    /// </summary>
    public class BattleRuleEffectManager
    {
        /// <summary>
        /// 效果配置表缓存。
        /// </summary>
        private readonly TbEffectGroupConfig mEffectConfigTable;

        /// <summary>
        /// 每个效果组执行后触发（用于外层监听日志/统计/联动）。
        /// </summary>
        public event Action<BattleRuleEffectExecuteInfo> OnEffectTriggered;

        public BattleRuleEffectManager(TbEffectGroupConfig effectConfigTable)
        {
            mEffectConfigTable = effectConfigTable;
        }

        /// <summary>
        /// 顺序执行规则挂载的全部效果组。
        /// 具体效果逻辑由 context.EffectExecutor 决定（伤害管线 / 获得命中管线）。
        /// </summary>
        public void ExecuteEffects(IReadOnlyList<int> effectGroupIds, SkillRuleConfig ruleConfig, BattleRuleTriggerContext context)
        {
            if (effectGroupIds == null || effectGroupIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < effectGroupIds.Count; i++)
            {
                var effectConfig = mEffectConfigTable.GetOrDefault(effectGroupIds[i]);
                if (effectConfig == null)
                {
                    LogTool.LogWarning($"[BattleRuleEffectManager] 效果组不存在: {effectGroupIds[i]}");
                    continue;
                }

                var executeInfo = new BattleRuleEffectExecuteInfo(ruleConfig, effectConfig, context);
                if (context?.EffectExecutor != null)
                {
                    // 真实效果逻辑由业务层执行器负责，管理器只做分发。
                    context.EffectExecutor.ExecuteEffect(executeInfo);
                }

                OnEffectTriggered?.Invoke(executeInfo);
            }
        }

        /// <summary>规则效果组中是否包含指定 EffectGroupsType。</summary>
        public bool ContainsEffectType(IReadOnlyList<int> effectGroupIds, EffectGroupsType effectType)
        {
            if (effectGroupIds == null || effectGroupIds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < effectGroupIds.Count; i++)
            {
                var effectConfig = mEffectConfigTable.GetOrDefault(effectGroupIds[i]);
                if (effectConfig?.Effects == null)
                {
                    continue;
                }

                for (int j = 0; j < effectConfig.Effects.Count; j++)
                {
                    if (effectConfig.Effects[j].EffectType == effectType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>效果组是否含施法时刻专属效果（不应在伤害管线 Before 规则中重复触发）。</summary>
        public bool ContainsCastOnlyEffect(IReadOnlyList<int> effectGroupIds)
        {
            return ContainsEffectType(effectGroupIds, EffectGroupsType.AddSkillNoBuff)
                || ContainsEffectType(effectGroupIds, EffectGroupsType.ContinuousAttack);
        }
    }
}
