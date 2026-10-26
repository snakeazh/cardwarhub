using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip.RedDot
{
    public enum EquipWearSlotRedDotMode
    {
        /// <summary>按当前页签：锻造页只判锻造，洗练页只判洗练。</summary>
        ByCurrentPage = 0,
        /// <summary>锻造或洗练任一可操作即亮。</summary>
        ForgeOrRefine = 1,
    }

    public readonly struct EquipWearSlotRedDotContext
    {
        public EquipWearSlotRedDotMode Mode { get; }
        public int PageIndex { get; }
        public int SelectedEquipId { get; }
        public int LockedEffectCount { get; }

        EquipWearSlotRedDotContext(
            EquipWearSlotRedDotMode mode, int pageIndex, int selectedEquipId, int lockedEffectCount)
        {
            Mode = mode;
            PageIndex = pageIndex;
            SelectedEquipId = selectedEquipId;
            LockedEffectCount = lockedEffectCount;
        }

        public static EquipWearSlotRedDotContext ByPage(
            int pageIndex, int selectedEquipId = 0, int lockedEffectCount = 0)
        {
            return new EquipWearSlotRedDotContext(
                EquipWearSlotRedDotMode.ByCurrentPage, pageIndex, selectedEquipId, lockedEffectCount);
        }

        public static EquipWearSlotRedDotContext ForgeOrRefineAny()
        {
            return new EquipWearSlotRedDotContext(
                EquipWearSlotRedDotMode.ForgeOrRefine, 0, 0, 0);
        }
    }

    public static class EquipForgeRedDotHelper
    {
        public const int ForgePageIndex = 0;
        public const int RefinePageIndex = 1;

        public static bool CheckWearEquipForgeRedPoint(PlayerEquip equip)
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip, false))
            {
                return false;
            }

            if (!Global.PlayerEquipSlots.TryGetValue(equip.PartType, out PlayerEquipSlot slot))
            {
                slot = PlayerEquipSlot.CreateDefault(equip.PartType);
            }

            SlotLevelListStruct levelCfg = slot.GetLevelCfg();
            if (levelCfg == null || levelCfg.UpPhaseConsumes.Count == 0)
            {
                return false;
            }

            return CheckConsumesEnough(levelCfg.UpPhaseConsumes);
        }

        public static bool CheckWearEquipRefineRedPoint(PlayerEquip equip, int lockedCount = 0)
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip, false))
            {
                return false;
            }

            EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null || equipCfg.EntryNum <= 0)
            {
                return false;
            }

            return CheckRefreshEntryConsumesEnough(equipCfg, lockedCount);
        }

        public static bool CheckWearEquipSlotRedPoint(
            PlayerEquip equip, EquipWearSlotRedDotContext context)
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip, false) || equip == null)
            {
                return false;
            }

            if (context.Mode == EquipWearSlotRedDotMode.ForgeOrRefine)
            {
                return CheckWearEquipForgeRedPoint(equip)
                    || CheckWearEquipRefineRedPoint(equip, 0);
            }

            int lockedCount = equip.Id == context.SelectedEquipId ? context.LockedEffectCount : 0;
            if (context.PageIndex == RefinePageIndex)
            {
                return CheckWearEquipRefineRedPoint(equip, lockedCount);
            }

            return CheckWearEquipForgeRedPoint(equip);
        }

        public static int EvaluateWearEquipSlotRedDot(
            PlayerEquip equip, EquipWearSlotRedDotContext context)
        {
            return CheckWearEquipSlotRedPoint(equip, context) ? 1 : 0;
        }

        public static bool CheckWearEquipRedPoint(
            PlayerEquip equip, int pageIndex, int selectedEquipId = 0, int selectedLockedCount = 0)
        {
            return CheckWearEquipSlotRedPoint(
                equip, EquipWearSlotRedDotContext.ByPage(pageIndex, selectedEquipId, selectedLockedCount));
        }

        public static bool CheckWearEquipForgeOrRefineRedPoint(PlayerEquip equip)
        {
            return CheckWearEquipSlotRedPoint(equip, EquipWearSlotRedDotContext.ForgeOrRefineAny());
        }

        public static int CountWearEquipRedPoints(
            int soldierId, IReadOnlyList<int> wearEquipIds, int pageIndex,
            int selectedEquipId = 0, int selectedLockedCount = 0)
        {
            if (wearEquipIds == null || wearEquipIds.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < wearEquipIds.Count; i++)
            {
                if (!Global.PlayerEquips.TryGetValue(wearEquipIds[i], out PlayerEquip equip))
                {
                    continue;
                }

                if (equip.WearerId != soldierId)
                {
                    continue;
                }

                if (CheckWearEquipRedPoint(equip, pageIndex, selectedEquipId, selectedLockedCount))
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountSoldierWearEquipRedPoints(
            int soldierId, int pageIndex, int selectedEquipId = 0, int selectedLockedCount = 0)
        {
            if (soldierId <= 0)
            {
                return 0;
            }

            int count = 0;
            var equips = new List<PlayerEquip>(Global.PlayerEquips.Values);
            for (int i = 0; i < equips.Count; i++)
            {
                PlayerEquip equip = equips[i];
                if (equip.WearerId != soldierId)
                {
                    continue;
                }

                if (CheckWearEquipRedPoint(equip, pageIndex, selectedEquipId, selectedLockedCount))
                {
                    count++;
                }
            }

            return count;
        }

        public static void CollectOwnedSoldierIds(List<int> soldierIds)
        {
            soldierIds.Clear();
            foreach (CardBaseConfig card in DataConfigManager.Tables.TbCardBaseConfig.DataList)
            {
                if (card.Type != CardType.Soldier || !Global.PlayerCards.ContainsKey(card.Id))
                {
                    continue;
                }

                if (Global.PlayerCards[card.Id].IsHave)
                {
                    soldierIds.Add(card.Id);
                }
            }
        }

        public static int CountAllSoldiersWearEquipRedPoints(int pageIndex)
        {
            var soldierIds = new List<int>();
            CollectOwnedSoldierIds(soldierIds);

            int count = 0;
            for (int i = 0; i < soldierIds.Count; i++)
            {
                count += CountSoldierWearEquipRedPoints(soldierIds[i], pageIndex, 0, 0);
            }

            return count;
        }

        public static void CollectForgeConsumeItemIds(List<int> itemIds)
        {
            var seen = new HashSet<int>();
            foreach (EquipSlotConfig slotConfig in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
            {
                for (int i = 0; i < slotConfig.SlotLevelList.Count; i++)
                {
                    List<RewardAndNum> consumes = slotConfig.SlotLevelList[i].UpPhaseConsumes;
                    for (int j = 0; j < consumes.Count; j++)
                    {
                        int itemId = consumes[j].Id;
                        if (itemId > 0 && seen.Add(itemId))
                        {
                            itemIds.Add(itemId);
                        }
                    }
                }
            }
        }

        public static void CollectRefineConsumeItemIds(List<int> itemIds)
        {
            var seen = new HashSet<int>();
            foreach (EquipConfig equipConfig in DataConfigManager.Tables.TbEquipConfig.DataList)
            {
                CollectConsumeItemIds(equipConfig.RefreshEntryConsume, itemIds, seen);
                CollectConsumeItemIds(equipConfig.LockEffectRefreshEntryConsume, itemIds, seen);
            }
        }

        public static void CollectAllConsumeItemIds(List<int> itemIds)
        {
            var seen = new HashSet<int>();
            foreach (EquipSlotConfig slotConfig in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
            {
                for (int i = 0; i < slotConfig.SlotLevelList.Count; i++)
                {
                    CollectConsumeItemIds(slotConfig.SlotLevelList[i].UpPhaseConsumes, itemIds, seen);
                }
            }

            foreach (EquipConfig equipConfig in DataConfigManager.Tables.TbEquipConfig.DataList)
            {
                CollectConsumeItemIds(equipConfig.RefreshEntryConsume, itemIds, seen);
                CollectConsumeItemIds(equipConfig.LockEffectRefreshEntryConsume, itemIds, seen);
            }
        }

        public static bool CheckConsumesEnough(List<RewardAndNum> consumeItems)
        {
            foreach (RewardAndNum item in consumeItems)
            {
                if (item.Num == 0)
                {
                    continue;
                }

                if (Global.GetItemNumById(item.Id) < item.Num)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckRefreshEntryConsumesEnough(EquipConfig equipCfg, int lockedCount)
        {
            foreach (RewardAndNum item in equipCfg.RefreshEntryConsume)
            {
                if (item.Num == 0)
                {
                    continue;
                }

                if (Global.GetItemNumById(item.Id) < item.Num)
                {
                    return false;
                }
            }

            if (lockedCount > 0)
            {
                foreach (RewardAndNum item in equipCfg.LockEffectRefreshEntryConsume)
                {
                    if (item.Num == 0)
                    {
                        continue;
                    }

                    if (Global.GetItemNumById(item.Id) < item.Num * lockedCount)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static void CollectConsumeItemIds(
            List<RewardAndNum> consumes, List<int> itemIds, HashSet<int> seen)
        {
            for (int i = 0; i < consumes.Count; i++)
            {
                int itemId = consumes[i].Id;
                if (itemId > 0 && seen.Add(itemId))
                {
                    itemIds.Add(itemId);
                }
            }
        }
    }
}
