using System;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 分支节点处理器
    /// 通过技能等级来判断走哪个分支
    /// </summary>
    public class BranchHandler : ISkillNodeHandler
    {
        public string NodeType => "Branch";
        public int Priority => 0;
        public bool IsAsync => true;

        public async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var condition = node.GetParamString("condition", "SkillLevelGreaterThan");
            var compareValue = node.GetParamFloat("compare_value", 1f);
            var compareAttr = node.GetParamString("compare_attr", "");

            LogTool.Log($"[BranchHandler] 条件判断: condition={condition}, compareValue={compareValue}, compareAttr={compareAttr}, skillLevel={context.Level}, casterLevel={GetCasterLevel(context)}");

            bool result = EvaluateCondition(condition, compareValue, compareAttr, context);

            context.SetExtraData("branch_result", result);

            LogTool.Log($"[BranchHandler] 条件结果: {result}");

            await UniTask.CompletedTask;
        }

        private bool EvaluateCondition(string condition, float compareValue, string compareAttr, SkillContext context)
        {
            int skillLevel = context.Level;
            int casterLevel = GetCasterLevel(context);

            switch (condition)
            {
                case "SkillLevelGreaterThan":
                    return skillLevel > compareValue;

                case "SkillLevelLessThan":
                    return skillLevel < compareValue;

                case "SkillLevelEqual":
                    return skillLevel == compareValue;

                case "SkillLevelGreaterOrEqual":
                    return skillLevel >= compareValue;

                case "SkillLevelLessOrEqual":
                    return skillLevel <= compareValue;

                case "CasterLevelGreaterThan":
                    return casterLevel > compareValue;

                case "CasterLevelLessThan":
                    return casterLevel < compareValue;

                case "CasterLevelEqual":
                    return casterLevel == compareValue;

                case "CasterLevelGreaterOrEqual":
                    return casterLevel >= compareValue;

                case "CasterLevelLessOrEqual":
                    return casterLevel <= compareValue;

                case "SkillAttrGreaterThan":
                    return TryGetSkillAttributeValue(context, compareAttr, out var skillAttrGt) && skillAttrGt > compareValue;

                case "SkillAttrLessThan":
                    return TryGetSkillAttributeValue(context, compareAttr, out var skillAttrLt) && skillAttrLt < compareValue;

                case "SkillAttrEqual":
                    return TryGetSkillAttributeValue(context, compareAttr, out var skillAttrEq) && Mathf.Approximately(skillAttrEq, compareValue);

                case "SkillAttrGreaterOrEqual":
                    return TryGetSkillAttributeValue(context, compareAttr, out var skillAttrGe) && skillAttrGe >= compareValue;

                case "SkillAttrLessOrEqual":
                    return TryGetSkillAttributeValue(context, compareAttr, out var skillAttrLe) && skillAttrLe <= compareValue;

                case "CasterAttrGreaterThan":
                case "CharacterAttrGreaterThan":
                    return TryGetCasterAttributeValue(context, compareAttr, out var casterAttrGt) && casterAttrGt > compareValue;

                case "CasterAttrLessThan":
                case "CharacterAttrLessThan":
                    return TryGetCasterAttributeValue(context, compareAttr, out var casterAttrLt) && casterAttrLt < compareValue;

                case "CasterAttrEqual":
                case "CharacterAttrEqual":
                    return TryGetCasterAttributeValue(context, compareAttr, out var casterAttrEq) && Mathf.Approximately(casterAttrEq, compareValue);

                case "CasterAttrGreaterOrEqual":
                case "CharacterAttrGreaterOrEqual":
                    return TryGetCasterAttributeValue(context, compareAttr, out var casterAttrGe) && casterAttrGe >= compareValue;

                case "CasterAttrLessOrEqual":
                case "CharacterAttrLessOrEqual":
                    return TryGetCasterAttributeValue(context, compareAttr, out var casterAttrLe) && casterAttrLe <= compareValue;

                default:
                    LogTool.LogWarning($"[BranchHandler] 未知的条件类型: {condition}，默认返回 false");
                    return false;
            }
        }

        private bool TryGetSkillAttributeValue(SkillContext context, string compareAttr, out float value)
        {
            value = 0f;
            if (!TryParseAttributeType(compareAttr, out var attrType))
            {
                LogTool.LogWarning($"[BranchHandler] 技能属性判断失败：compare_attr 无效({compareAttr})");
                return false;
            }

            var skillCard = GetSkillCard(context);
            if (skillCard == null)
            {
                LogTool.LogWarning($"[BranchHandler] 技能属性判断失败：未找到技能卡 skillId={context?.SkillId}");
                return false;
            }

            value = skillCard.Attributes.GetFinal(attrType);
            LogTool.Log($"[BranchHandler] 技能属性读取: attr={attrType}, value={value}");
            return true;
        }

        private bool TryGetCasterAttributeValue(SkillContext context, string compareAttr, out float value)
        {
            value = 0f;
            if (!TryParseAttributeType(compareAttr, out var attrType))
            {
                LogTool.LogWarning($"[BranchHandler] 英灵属性判断失败：compare_attr 无效({compareAttr})");
                return false;
            }

            if (context?.Caster == null)
            {
                LogTool.LogWarning("[BranchHandler] 英灵属性判断失败：Caster 为空");
                return false;
            }

            var soldierCard = context.Caster.GetComponent<SoldierCard>();
            if (soldierCard == null)
            {
                LogTool.LogWarning("[BranchHandler] 英灵属性判断失败：Caster 上未找到 SoldierCard");
                return false;
            }

            value = soldierCard.BaseAttributes.GetFinal(attrType);
            LogTool.Log($"[BranchHandler] 英灵属性读取: attr={attrType}, value={value}");
            return true;
        }

        private bool TryParseAttributeType(string attrValue, out AttributeType attrType)
        {
            attrType = default;
            if (string.IsNullOrEmpty(attrValue))
            {
                return false;
            }

            if (int.TryParse(attrValue, out var attrInt))
            {
                attrType = (AttributeType)attrInt;
                return true;
            }

            return Enum.TryParse(attrValue, true, out attrType);
        }

        private ISkill GetSkillCard(SkillContext context)
        {
            if (context?.Caster == null)
            {
                return null;
            }

            var soldierCard = context.Caster.GetComponent<SoldierCard>();
            if (soldierCard == null)
            {
                return null;
            }

            return soldierCard.GetSkillById(context.SkillId);
        }

        private int GetCasterLevel(SkillContext context)
        {
            if (context?.Caster == null)
                return 0;

            var soldier = context.Caster.GetComponent<SoldierCard>();
            return soldier?.level.Value ?? 0;
        }

        public bool Validate(SkillNodeConfig node, out string error)
        {
            error = null;
            return true;
        }
    }
}
