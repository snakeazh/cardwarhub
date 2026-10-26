using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能图节点 <c>ReturnAnimation</c>：在 hide 类技能末尾播放 return 表现动画。
    /// <para>
    /// 前置条件：Spine 技能动画已触发 <c>hide</c> 事件（<see cref="SoldierAnimationController"/> 进入等待态）。
    /// 校验 <see cref="SkillContext.SkillAnimationPlayId"/> 与当前 <see cref="SoldierAnimationController.CurrentPlayId"/>，
    /// 防止旧技能 Return 覆盖新技能动画。详见 <c>HideReturnAnimation_配置说明.md</c>。
    /// </para>
    /// </summary>
    public class ReturnAnimationHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "ReturnAnimation";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var soldierCard = ResolveSoldierCard(context);
            if (soldierCard == null || soldierCard.AnimationController == null)
            {
                LogTool.LogWarning("[ReturnAnimationHandler] 施法者缺少 AnimationController，跳过");
                return;
            }

            var controller = soldierCard.AnimationController;

            // 快照 playId 来自 SkillRuntime.ApplySkillAnimationPlayId（atk 后、节点图执行前）
            var cachedPlayId = context.SkillAnimationPlayId;
            if (!controller.CanPlayReturn(cachedPlayId))
            {
                LogTool.Log("[ReturnAnimationHandler] 无法播放 Return（未 hide 等待或 playId 已变化），跳过");
                return;
            }

            var animName = node.GetParamString("anim_name", "return");
            if (string.IsNullOrEmpty(animName))
            {
                LogTool.LogWarning("[ReturnAnimationHandler] anim_name 为空，跳过");
                return;
            }

            controller.PlayReturnAnimation(animName);
        }

        private static SoldierCard ResolveSoldierCard(SkillContext context)
        {
            if (context.Caster is SoldierCard soldierCard)
            {
                return soldierCard;
            }

            return context.Caster != null ? context.Caster.GetComponent<SoldierCard>() : null;
        }
    }
}
