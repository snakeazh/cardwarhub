using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.CombatPower
{
    /// <summary>
    /// 配置表战力数值读取（用于单件/单卡展示）。账号总战力以服务端为准，勿在此重算。
    /// </summary>
    public static class FightNumCalcHelper
    {
        public static int CalcSoldierFightNum(int soldierId, int soldierLv, PlayerCard card = null)
        {
            if (soldierId <= 0 || soldierLv <= 0)
            {
                return 0;
            }

            card = card ?? Global.GetCardById(soldierId);
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldierId);
            if (soldierCfg == null || soldierLv > soldierCfg.LevelPropertyList.Count)
            {
                return 0;
            }

            var fightNum = soldierCfg.LevelPropertyList[soldierLv - 1].FightNum;

            if (card?.ObjData != null && card.ObjData.Skin > 0)
            {
                var skinConfig = DataConfigManager.Tables.TbSkinConfig.Get(card.ObjData.Skin);
                if (skinConfig != null)
                {
                    fightNum += skinConfig.FightNum;
                }
            }

            if (card?.ObjData != null && card.ObjData.IsAwaked)
            {
                fightNum += soldierCfg.AwakingFightNum;
            }

            return fightNum;
        }
    }
}
