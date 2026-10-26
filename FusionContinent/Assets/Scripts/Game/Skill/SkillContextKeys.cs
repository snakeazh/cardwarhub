namespace FusionContinent.Skill
{
    /// <summary>
    /// SkillContext 扩展数据键名（战斗规则等非技能图流程写入）。
    /// </summary>
    public static class SkillContextKeys
    {
        /// <summary>
        /// 链式学习规则来源参数代理 <see cref="ISkill"/>（仅 Attributes 有效，供 Buff *AttrName 读取）。
        /// </summary>
        public const string RuleSourceParamSkill = "BattleRuleSourceParamSkill";
    }
}
