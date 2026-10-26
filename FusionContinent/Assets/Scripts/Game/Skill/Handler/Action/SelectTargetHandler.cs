using FusionContinent.Skill.Config;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 选取目标并写入 SkillContext.SelectedTargets，供后续 ApplyBuff / Effect / Projectile 等节点复用。
    /// </summary>
    public class SelectTargetHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "SelectTarget";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var target = node.GetParamString("target", "Target");
            bool clearBefore = node.GetParamBool("clear_selected_list", true);
            bool excludePrevious = node.GetParamBool("exclude_previous_selected", true);
            var resolved = SkillTargetResolver.Resolve(target, context, excludePrevious);
            SkillTargetResolver.WriteSelectedTargets(context, resolved, clearBefore);
            SkillTargetResolver.ApplySelectionToContext(context);

            var syncedTargetName = context.Target != null ? context.Target.name : "(null)";
            LogTool.Log($"[SelectTargetHandler] 选取目标: {resolved.Count} 个, target={target}, clear_selected_list={clearBefore}, exclude_previous_selected={excludePrevious}, synced_target={syncedTargetName}");
        }
    }
}
