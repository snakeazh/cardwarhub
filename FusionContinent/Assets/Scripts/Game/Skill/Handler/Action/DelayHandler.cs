using System;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 延迟节点处理器
    /// </summary>
    public class DelayHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "Delay";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var duration = node.GetParamFloat("duration", 1f);
            var durationAttrName = node.GetParamString("duration_attr", "");

            if (!string.IsNullOrEmpty(durationAttrName))
            {
                var skillCard = ResolveSkillCard(context);
                if (skillCard != null && Enum.TryParse<AttributeType>(durationAttrName, out var durationAttr))
                {
                    duration = skillCard.Attributes.GetFinal(durationAttr);
                    LogTool.Log($"[DelayHandler] 从属性 {durationAttrName} 读取 duration={duration}");
                }
            }

            LogTool.Log($"[DelayHandler] 延迟: {duration} 秒");

            if (duration > 0)
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: context.CancellationToken.Token);
            }

            LogTool.Log($"[DelayHandler] 延迟结束");
        }

        private static ISkill ResolveSkillCard(SkillContext context)
        {
            if (context.Caster is SoldierCard soldierCard)
            {
                return soldierCard.GetSkillById(context.SkillId);
            }

            var casterSoldier = context.Caster?.GetComponent<SoldierCard>();
            return casterSoldier?.GetSkillById(context.SkillId);
        }
    }
}
