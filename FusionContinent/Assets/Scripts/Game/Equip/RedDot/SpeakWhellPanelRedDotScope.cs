using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class SpeakWhellPanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "SpeakWhellPanel";

        public static class Paths
        {
            public const string WearPrefix = "Wear/";
            public const string BagPrefix = "Bag/";
            public const string GemBagPrefix = "Gem/Bag/";

            public static string WearSlot(int partType) => $"{WearPrefix}{partType}";

            public static string BagEquip(int equipId) => $"{BagPrefix}{equipId}";

            public static string BagFusionGem(int gemId) => $"{GemBagPrefix}{gemId}";
        }

        readonly List<PlayerEquip> _partSlots = new List<PlayerEquip>();

        public int WearerId { get; private set; }

        public int PartSlotCount { get; private set; }

        public override string Scope => ScopeName;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new SpeakWhellPanelLocalRedDotRule());
        }

        /// <summary>
        /// 同步穿戴槽数据。已 Open 时只更新状态并 MarkDirty，避免 Close/Open 销毁 Bag/* 节点导致背包箭头绑定失效。
        /// </summary>
        public void Sync(int wearerId, IReadOnlyList<PlayerEquip> partSlots, int partSlotCount)
        {
            WearerId = wearerId;
            PartSlotCount = partSlotCount;
            _partSlots.Clear();

            if (partSlots != null)
            {
                for (int i = 0; i < partSlots.Count; i++)
                {
                    _partSlots.Add(partSlots[i]);
                }
            }

            if (!IsOpen)
            {
                Open();
                return;
            }

            for (int i = 0; i < PartSlotCount; i++)
            {
                EnsureNode(Paths.WearSlot(i));
            }

            MarkAllDirty();
            Service.FlushNow();
        }

        public PlayerEquip GetEquipAtPart(int partType)
        {
            if (partType < 0 || partType >= _partSlots.Count)
            {
                return null;
            }

            return _partSlots[partType];
        }

        /// <summary>
        /// 判断背包中某件装备是否比该部位当前穿戴的装备战力更高（用于 item 级箭头）。
        /// 槽位为空时，背包装备即算更优。
        /// </summary>
        public bool IsBagEquipBetter(PlayerEquip bagEquip)
        {
            if (bagEquip == null)
            {
                return false;
            }

            PlayerEquip worn = GetEquipAtPart((int)bagEquip.PartType);

            int currentFightNum = worn != null && worn.WearerId == WearerId
                ? EquipReplaceRedDotHelper.CalcEquipFightNum(worn)
                : -1;

            return EquipReplaceRedDotHelper.IsBagEquipBetter(bagEquip, currentFightNum);
        }

        /// <summary>
        /// 指定部位是否存在比当前穿戴更高战力的背包装备。
        /// </summary>
        public bool HasBetterBagEquipAtPart(int partType)
        {
            PlayerEquip worn = GetEquipAtPart(partType);
            bool slotEmpty = worn == null || worn.WearerId != WearerId;
            int currentFightNum = slotEmpty ? -1 : EquipReplaceRedDotHelper.CalcEquipFightNum(worn);
            return EquipReplaceRedDotHelper.HasBetterBagEquip((EquipPartType)partType, currentFightNum, slotEmpty);
        }

        /// <summary>
        /// 背包融合石是否可比任一部位当前镶嵌石更高战力（列表红点）。
        /// </summary>
        public bool IsBagFusionGemBetter(PlayerFusionGem bagGem)
        {
            return FusionGemReplaceRedDotHelper.IsBagGemBetterThanAnyWorn(bagGem, WearerId);
        }

        protected override void BuildNodes(RedDotTree tree)
        {
            for (int i = 0; i < PartSlotCount; i++)
            {
                EnsureNode(tree, Paths.WearSlot(i));
            }
        }

        protected override void ResetState()
        {
            WearerId = 0;
            PartSlotCount = 0;
            _partSlots.Clear();
        }
    }
}
