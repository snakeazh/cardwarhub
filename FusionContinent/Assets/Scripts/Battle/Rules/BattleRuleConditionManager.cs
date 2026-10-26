using System.Collections.Generic;
using System.Text;
using Config;
using FusionContinent;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 规则条件管理器：按 ConditionGroupId 读取并执行比较判断。
    /// </summary>
    public class BattleRuleConditionManager
    {
        /// <summary>
        /// 条件配置表缓存。
        /// </summary>
        private readonly TbConditionGroupConfig mConditionConfigTable;

        public BattleRuleConditionManager(TbConditionGroupConfig conditionConfigTable)
        {
            mConditionConfigTable = conditionConfigTable;
        }

        /// <summary>
        /// 条件组为 AND 关系：任意一个不满足即返回 false。
        /// </summary>
        public bool AreAllConditionsMet(IReadOnlyList<int> conditionGroupIds, BattleRuleTriggerContext context)
        {
            if (conditionGroupIds == null || conditionGroupIds.Count == 0)
            {
                // 没有条件视为无门槛触发。
                return true;
            }

            for (int i = 0; i < conditionGroupIds.Count; i++)
            {
                var conditionConfig = mConditionConfigTable.GetOrDefault(conditionGroupIds[i]);
                if (conditionConfig == null)
                {
                    LogTool.LogWarning($"[BattleRuleConditionManager] 条件组不存在: {conditionGroupIds[i]}");
                    return false;
                }

                if (!IsConditionMet(conditionConfig, context))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 输出条件组解析后的属性/特殊值摘要，供规则执行日志使用。
        /// </summary>
        public string BuildConditionAttributeSummary(
            IReadOnlyList<int> conditionGroupIds,
            BattleRuleTriggerContext context)
        {
            if (conditionGroupIds == null || conditionGroupIds.Count == 0 || context == null)
            {
                return string.Empty;
            }

            if (context.ValueProvider == null)
            {
                return "ValueProvider=null";
            }

            var summary = new StringBuilder();
            for (int i = 0; i < conditionGroupIds.Count; i++)
            {
                var conditionConfig = mConditionConfigTable.GetOrDefault(conditionGroupIds[i]);
                if (conditionConfig == null)
                {
                    continue;
                }

                if (summary.Length > 0)
                {
                    summary.Append(';');
                }

                var leftValue = ResolveLeftValue(conditionConfig, context);
                var rightValue = ResolveRightValue(conditionConfig, context);
                summary.Append("cond").Append(conditionConfig.Id);
                summary.Append('[').Append(conditionConfig.SpecialType);
                if (conditionConfig.SpecialType == SpecialsType.GoalAttribute)
                {
                    summary.Append('/').Append(conditionConfig.Attribute);
                }

                summary.Append(" left=").Append(leftValue.ToString("F3"));
                summary.Append(' ').Append(conditionConfig.ComparisonType);
                summary.Append(" right=").Append(rightValue.ToString("F3"));
                summary.Append(']');
            }

            return summary.ToString();
        }

        private bool IsConditionMet(ConditionGroupConfig conditionConfig, BattleRuleTriggerContext context)
        {
            if (context == null || context.ValueProvider == null)
            {
                LogTool.LogWarning("[BattleRuleConditionManager] 缺少 ValueProvider，无法计算条件");
                return false;
            }

            var leftValue = ResolveLeftValue(conditionConfig, context);
            var rightValue = ResolveRightValue(conditionConfig, context);

            var met = CompareValues(leftValue, rightValue, conditionConfig.ComparisonType, conditionConfig.ComparisonValueType);
            if (!met && context.Trigger == TriggerType.HpChange)
            {
                var hero = context.Source as SoldierCard;
                var heroInfo = hero != null
                    ? $"hero={hero.cardId}, hp={hero.CurrentHp}/{hero.MaxHp}"
                    : "hero=未知";
                LogTool.Log(
                    $"[BattleRuleConditionManager] HpChange 条件未通过: condId={conditionConfig.Id}, {heroInfo}, left={leftValue:F3}, op={conditionConfig.ComparisonType}, right={rightValue:F3}");
            }

            return met;
        }

        /// <summary>
        /// 解析比较左值：
        /// - GoalAttribute：读取目标单位属性（SoldierMaxHealth 配置项实际取 SoldierCard.CurrentHp/MaxHp）；
        /// - 其他 SpecialType：交给 ValueProvider 处理。
        /// </summary>
        private float ResolveLeftValue(ConditionGroupConfig conditionConfig, BattleRuleTriggerContext context)
        {
            if (conditionConfig.SpecialType == SpecialsType.GoalAttribute)
            {
                return context.ValueProvider.GetAttributeValue(context, UnitType.Target, conditionConfig.Attribute);
            }

            return context.ValueProvider.GetSpecialValue(context, conditionConfig.SpecialType);
        }

        /// <summary>
        /// 解析比较右值：优先取动态值，取不到时回退配置值。
        /// </summary>
        private float ResolveRightValue(ConditionGroupConfig conditionConfig, BattleRuleTriggerContext context)
        {
            var comparisonValue = context.ValueProvider.GetComparisonValue(context, conditionConfig);

            if (Mathf.Approximately(comparisonValue, 0f))
            {
                comparisonValue = conditionConfig.ComparisonValue;
            }

            return comparisonValue;
        }

        /// <summary>
        /// 统一比较入口。Percent 类型会先做归一化（0-1）。
        /// </summary>
        private bool CompareValues(
            float leftValue,
            float rightValue,
            ComparisonsType comparisonType,
            ComparisonValuesType comparisonValueType)
        {
            if (comparisonValueType == ComparisonValuesType.Percent || comparisonType == ComparisonsType.Percent)
            {
                leftValue = NormalizePercent(leftValue);
                rightValue = NormalizePercent(rightValue);
            }

            switch (comparisonType)
            {
                case ComparisonsType.GreaterThanOrEqualTo:
                    return leftValue >= rightValue;
                case ComparisonsType.GreaterThan:
                    return leftValue > rightValue;
                case ComparisonsType.LessThan:
                    return leftValue < rightValue;
                case ComparisonsType.LessThanOrEqualTo:
                    return leftValue <= rightValue;
                case ComparisonsType.EqualTo:
                    return Mathf.Approximately(leftValue, rightValue);
                case ComparisonsType.Percent:
                    return leftValue >= rightValue;
                case ComparisonsType.Yes:
                    return !Mathf.Approximately(leftValue, 0f);
                case ComparisonsType.No:
                    return Mathf.Approximately(leftValue, 0f);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 百分比归一化：
        /// - 60 -> 0.6
        /// - 0.6 -> 0.6
        /// </summary>
        private float NormalizePercent(float value)
        {
            if (value > 1f)
            {
                return value / 100f;
            }

            return value;
        }
    }
}
