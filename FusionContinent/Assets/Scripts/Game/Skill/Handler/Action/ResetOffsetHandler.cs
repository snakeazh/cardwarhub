using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 重置位置偏移节点处理器
    /// 将 context.PositionOffset 清零，后续节点从原始 TargetPosition 重新计算偏移
    /// </summary>
    public class ResetOffsetHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "ResetOffset";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var previousOffset = context.EffectOffset;
            context.EffectOffset = Vector2.zero;
            context.HasEffectOffset = false;
            LogTool.Log($"[ResetOffsetHandler] 偏移已重置，之前的偏移量: {previousOffset}");
        }
    }
}