using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;

namespace FusionContinent
{
    public partial class GamePlayManager
    {
        private const string CarBindingSourceKey = "CarBinding";

        /// <summary>
        /// 按编队职业种类刷新职业羁绊（CarBinding）对 ModuleAttributes 的加成。
        /// </summary>
        public void RefreshCarBindingAttributes()
        {
            if (ModuleAttributes == null)
            {
                return;
            }

            ModuleAttributes.RemoveAttributeSource(AttributeSourceType.GeneralSkill, CarBindingSourceKey);

            var bindingCfg = GetCarBindingConfig();
            if (bindingCfg == null)
            {
                return;
            }

            var distinctCarTypeCount = CountDistinctCarTypesFromTeam();
            var tier = ResolveCarBindingTier(bindingCfg, distinctCarTypeCount);
            if (tier != null && tier.Attributes.Count > 0)
            {
                ModuleAttributes.ApplyAttributeSource(AttributeSourceType.GeneralSkill, CarBindingSourceKey, tier.Attributes);
            }
        }

        private static BindingConfig GetCarBindingConfig()
        {
            foreach (var cfg in DataConfigManager.Tables.TbBindingConfig.DataList)
            {
                if (cfg.Type == BindingType.CarBinding)
                {
                    return cfg;
                }
            }

            return null;
        }

        private static int CountDistinctCarTypesFromTeam()
        {
            var carTypes = new HashSet<CareerType>();
            foreach (var teamCard in Global.SoldierTeams)
            {
                if (teamCard.CardType != CardType.Soldier || teamCard.CardId <= 0)
                {
                    continue;
                }

                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(teamCard.CardId);
                if (soldierCfg == null)
                {
                    continue;
                }

                carTypes.Add(soldierCfg.CarType);
            }

            return carTypes.Count;
        }

        private static CarCountListStruct ResolveCarBindingTier(BindingConfig bindingCfg, int distinctCarTypeCount)
        {
            CarCountListStruct bestTier = null;
            foreach (var tier in bindingCfg.CarCountList)
            {
                if (distinctCarTypeCount < tier.CarCount)
                {
                    continue;
                }

                if (bestTier == null || tier.CarCount > bestTier.CarCount)
                {
                    bestTier = tier;
                }
            }

            return bestTier;
        }
    }
}
