using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 将 EffectGroup.Attributes 中的英灵属性落到单个 SoldierCard（新召唤补同步用）。
    /// </summary>
    public static class BattleRuleSoldierAttributeApplier
    {
        public static void ApplyToSoldier(
            SoldierCard soldier,
            AttributeStruct attr,
            AttributeModifier modifier)
        {
            if (soldier == null || soldier.isDie || attr == null || modifier == null)
            {
                return;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
            if (attrCfg == null || attrCfg.Category != AttributeCategoryType.Soldier)
            {
                return;
            }

            AttributeManager.SetAttributeLogic(modifier, new AttributeData(attr), targetId: soldier.cardId);
        }
    }
}
