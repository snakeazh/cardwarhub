using Config;

namespace Game.Battle
{
    /// <summary>
    /// 本局已学习的战斗规则条目（允许同 RuleId 多次叠加）。
    /// </summary>
    public readonly struct BattleLearnedRuleEntry
    {
        public int RuleId { get; }
        public string StackSourceKey { get; }
        public AttributeSourceType SourceType { get; }

        /// <summary>
        /// 英雄专属规则：对应 <see cref="SchoolCardConfig.IsAddHero"/>（英灵 cardId）。
        /// 0 表示全局规则；&gt;0 时仅该英灵触发/计数/获得效果。
        /// </summary>
        public int ScopeHeroCardId { get; }

        public BattleLearnedRuleEntry(
            int ruleId,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard,
            int scopeHeroCardId = 0)
        {
            RuleId = ruleId;
            StackSourceKey = stackSourceKey ?? string.Empty;
            SourceType = sourceType;
            ScopeHeroCardId = scopeHeroCardId;
        }
    }
}
