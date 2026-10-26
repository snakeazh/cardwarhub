using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 结束混合 Spine 动画节点（按动画名关闭）
    /// </summary>
    public class StopMixAnimationHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "StopMixAnimation";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var soldierCard = ResolveSoldierCard(context);
            if (soldierCard?.AnimationController == null)
            {
                LogTool.LogWarning("[StopMixAnimationHandler] 施法者缺少 AnimationController，跳过");
                return;
            }

            var animName = node.GetParamString("anim_name", "");
            if (string.IsNullOrEmpty(animName))
            {
                LogTool.LogWarning("[StopMixAnimationHandler] anim_name 为空，跳过");
                return;
            }

            var fadeDuration = node.GetParamFloat("fade_duration", 0.2f);
            soldierCard.AnimationController.StopMixAnimation(animName, fadeDuration);
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
