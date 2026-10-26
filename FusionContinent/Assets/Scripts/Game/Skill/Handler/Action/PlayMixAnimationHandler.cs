using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 播放混合 Spine 动画节点（立即继续，不等待）
    /// </summary>
    public class PlayMixAnimationHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "PlayMixAnimation";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var soldierCard = ResolveSoldierCard(context);
            if (soldierCard?.AnimationController == null)
            {
                LogTool.LogWarning("[PlayMixAnimationHandler] 施法者缺少 AnimationController，跳过");
                return;
            }

            var animName = node.GetParamString("anim_name", "");
            if (string.IsNullOrEmpty(animName))
            {
                LogTool.LogWarning("[PlayMixAnimationHandler] anim_name 为空，跳过");
                return;
            }

            var track = node.GetParamInt("track", 1);
            var alpha = node.GetParamFloat("alpha", 1f);
            var loop = node.GetParamBool("loop", true);

            soldierCard.AnimationController.PlayMixAnimation(animName, track, alpha, loop);
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
