using FusionContinent.Skill.Config;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 范围选取处理器：检测范围内目标并写入 HitMonsters，不造成伤害。
    /// 配合 ApplyBuff（target=Hit）对范围内目标施加 Buff。
    /// </summary>
    public class AoeSelectHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "AoeSelect";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var hits = AoeHitUtility.QueryMonsters(node, context);
            bool clearBefore = node.GetParamBool("clear_hit_list", true);
            AoeHitUtility.AddToHitMonsters(context, hits, clearBefore);

            LogTool.Log($"[AoeSelectHandler] 范围选取: 命中 {hits.Count} 个目标, clear_hit_list={clearBefore}");
        }
    }
}
