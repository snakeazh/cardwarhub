using System.Collections.Generic;
using Config;

namespace Game.Battle
{
    /// <summary>
    /// 识别需按英灵逐个判条件的 NoCondition 规则（装备词条职业/最高级/主线等）。
    /// </summary>
    public static class BattleRuleScopedConditionSupport
    {
        public static bool UsesPerSoldierScopedConditions(
            SkillRuleConfig ruleConfig,
            TbConditionGroupConfig conditionConfigTable)
        {
            if (ruleConfig == null
                || ruleConfig.ConditionGroupId == null
                || ruleConfig.ConditionGroupId.Count == 0
                || conditionConfigTable == null)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ConditionGroupId.Count; i++)
            {
                var conditionConfig = conditionConfigTable.GetOrDefault(ruleConfig.ConditionGroupId[i]);
                if (conditionConfig == null)
                {
                    continue;
                }

                if (IsPerSoldierScopedSpecialType(conditionConfig.SpecialType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPerSoldierScopedSpecialType(SpecialsType specialType)
        {
            switch (specialType)
            {
                case SpecialsType.WarriorNum:
                case SpecialsType.ArcherNum:
                case SpecialsType.MageNum:
                case SpecialsType.AssassinNum:
                case SpecialsType.AssistantNum:
                case SpecialsType.PowerHero:
                case SpecialsType.IntHero:
                case SpecialsType.AgHero:
                case SpecialsType.HighLevelArcherNum:
                case SpecialsType.HighLevelWarriorNum:
                case SpecialsType.HighLevelMageNum:
                case SpecialsType.HighLevelAssassinNum:
                case SpecialsType.HighLevelAssistantNum:
                case SpecialsType.MainStage:
                case SpecialsType.IsAssistant:
                case SpecialsType.LiveMageNum:
                case SpecialsType.LiveWarriorNum:
                case SpecialsType.BuyEffectsNumInBattle:
                    return true;
                default:
                    return false;
            }
        }
    }
}
