using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip.RedDot
{
    /// <summary>
    /// 装备替换红点判定：当背包中存在比当前穿戴装备战力更高的同部位装备时亮红点。
    /// 单件装备战力 = Config.FightNum + EntryFightNum×词条数 + EffectsFightNum×特效数。
    /// （槽位等级战力同部位固定，比较时可忽略。）
    /// </summary>
    public static class EquipReplaceRedDotHelper
    {
        /// <summary>
        /// 计算单件装备战力（不含槽位等级战力，用于同部位横向比较）。
        /// </summary>
        public static int CalcEquipFightNum(PlayerEquip equip)
        {
            if (equip == null || equip.Config == null)
            {
                return 0;
            }

            int fightNum = equip.Config.FightNum;

            if (equip.Entrys != null && equip.Entrys.Count > 0)
            {
                fightNum += equip.Config.EntryFightNum * equip.Entrys.Count;
            }

            if (equip.Effects != null && equip.Effects.Count > 0)
            {
                fightNum += equip.Config.EffectsFightNum * equip.Effects.Count;
            }

            return fightNum;
        }

        /// <summary>
        /// 背包中（WearerId==0）是否存在比指定穿戴装备战力更高的同部位装备。
        /// 当前穿戴为空时，背包中存在该部位装备即返回 true。
        /// </summary>
        public static bool HasBetterBagEquip(EquipPartType partType, int currentEquipFightNum, bool slotEmpty)
        {
            foreach (PlayerEquip equip in Global.PlayerEquips.Values)
            {
                if (equip.WearerId != 0 || equip.PartType != partType)
                {
                    continue;
                }

                // 空槽位：背包装备存在即算更优；否则需严格大于当前穿戴。
                if (slotEmpty || CalcEquipFightNum(equip) > currentEquipFightNum)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断某件背包装备是否比该部位当前穿戴的装备战力更高（用于 item 级红点）。
        /// currentEquipFightNum < 0 表示当前槽位为空。
        /// </summary>
        public static bool IsBagEquipBetter(PlayerEquip bagEquip, int currentEquipFightNum)
        {
            if (bagEquip == null || bagEquip.WearerId != 0)
            {
                return false;
            }

            return currentEquipFightNum < 0 || CalcEquipFightNum(bagEquip) > currentEquipFightNum;
        }

        /// <summary>
        /// 统计当前穿戴者各部位中，背包存在更优装备的部位数量（用于导航栏/页签聚合红点）。
        /// </summary>
        public static int CountReplaceableParts(int wearerId)
        {
            if (wearerId <= 0)
            {
                return 0;
            }

            // 先收集穿戴者各部位当前穿戴装备及其战力（-1 表示空槽位）。
            var wornFightNums = new Dictionary<EquipPartType, int>();
            foreach (PlayerEquip equip in Global.PlayerEquips.Values)
            {
                if (equip.WearerId != wearerId)
                {
                    continue;
                }

                // 同部位取第一件（正常每部位只穿一件）
                if (!wornFightNums.ContainsKey(equip.PartType))
                {
                    wornFightNums[equip.PartType] = CalcEquipFightNum(equip);
                }
            }

            int count = 0;
            var checkedParts = new HashSet<EquipPartType>();
            foreach (PlayerEquip equip in Global.PlayerEquips.Values)
            {
                if (equip.WearerId != 0)
                {
                    continue;
                }

                EquipPartType pt = equip.PartType;
                if (!checkedParts.Add(pt))
                {
                    continue;
                }

                bool slotEmpty = !wornFightNums.TryGetValue(pt, out int wornFightNum);
                int current = slotEmpty ? -1 : wornFightNum;
                if (HasBetterBagEquip(pt, current, slotEmpty))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
