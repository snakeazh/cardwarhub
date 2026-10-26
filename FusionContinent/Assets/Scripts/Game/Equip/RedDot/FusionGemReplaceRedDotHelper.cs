using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip.RedDot
{
    /// <summary>
    /// 融合石替换红点判定：背包中存在比当前部位已镶嵌融合石战力更高的石时亮红点。
    /// 单石战力 = GetLevelCfg().FightNum。仅统计已穿戴装备的部位（无装备无法镶嵌）。
    /// </summary>
    public static class FusionGemReplaceRedDotHelper
    {
        public static int CalcFusionGemFightNum(PlayerFusionGem gem)
        {
            if (gem == null || gem.Config == null || gem.Level <= 0)
            {
                return 0;
            }

            return gem.GetLevelCfg().FightNum;
        }

        /// <summary>
        /// 背包中（WearerId==0）是否存在比指定部位当前镶嵌石战力更高的融合石。
        /// currentGemFightNum &lt; 0 表示该部位装备尚未镶嵌融合石。
        /// </summary>
        public static bool HasBetterBagGem(int currentGemFightNum)
        {
            if (Global.PlayerFusionGems == null)
            {
                return false;
            }

            foreach (PlayerFusionGem gem in Global.PlayerFusionGems.Values)
            {
                if (gem == null || gem.WearerId != 0)
                {
                    continue;
                }

                if (currentGemFightNum < 0 || CalcFusionGemFightNum(gem) > currentGemFightNum)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 某件背包融合石是否可比任一已穿戴装备部位的当前镶嵌石更高战力（用于列表 item 红点）。
        /// </summary>
        public static bool IsBagGemBetterThanAnyWorn(PlayerFusionGem bagGem, int wearerId)
        {
            if (bagGem == null || bagGem.WearerId != 0 || wearerId <= 0)
            {
                return false;
            }

            int bagFightNum = CalcFusionGemFightNum(bagGem);
            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            if (partNameList == null)
            {
                return false;
            }

            for (int i = 0; i < partNameList.Count; i++)
            {
                int currentFightNum = GetWornGemFightNumAtPart(wearerId, (EquipPartType)i);
                // -2：该部位无装备，无法镶嵌，跳过
                if (currentFightNum == -2)
                {
                    continue;
                }

                if (currentFightNum < 0 || bagFightNum > currentFightNum)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 统计当前穿戴者各部位中，背包存在更优融合石的部位数量。
        /// </summary>
        public static int CountReplaceableGemParts(int wearerId)
        {
            if (wearerId <= 0)
            {
                return 0;
            }

            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            if (partNameList == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < partNameList.Count; i++)
            {
                int currentFightNum = GetWornGemFightNumAtPart(wearerId, (EquipPartType)i);
                if (currentFightNum == -2)
                {
                    continue;
                }

                if (HasBetterBagGem(currentFightNum))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 返回部位当前镶嵌石战力；无石返回 -1；无装备返回 -2。
        /// </summary>
        static int GetWornGemFightNumAtPart(int wearerId, EquipPartType partType)
        {
            PlayerEquip equip = null;
            foreach (PlayerEquip e in Global.PlayerEquips.Values)
            {
                if (e.WearerId == wearerId && e.PartType == partType)
                {
                    equip = e;
                    break;
                }
            }

            if (equip == null)
            {
                return -2;
            }

            if (Global.PlayerFusionGems == null)
            {
                return -1;
            }

            foreach (PlayerFusionGem gem in Global.PlayerFusionGems.Values)
            {
                if (gem != null && gem.WearerId == equip.Id)
                {
                    return CalcFusionGemFightNum(gem);
                }
            }

            return -1;
        }
    }
}
