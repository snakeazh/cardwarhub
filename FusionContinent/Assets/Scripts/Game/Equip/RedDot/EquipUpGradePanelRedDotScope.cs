using System.Collections.Generic;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class EquipUpGradePanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "EquipUpGradePanel";

        public static class Paths
        {
            public const string WearPrefix = "Wear/";

            public static string WearSlot(int equipInstanceId) => $"{WearPrefix}{equipInstanceId}";
        }

        readonly List<int> _wearEquipIds = new List<int>();

        public int SoldierId { get; private set; }

        public int CurrentPage { get; private set; } = EquipForgeRedDotHelper.ForgePageIndex;

        public int SelectedEquipId { get; private set; }

        public int LockedEffectCount { get; private set; }

        public IReadOnlyList<int> WearEquipIds => _wearEquipIds;

        public override string Scope => ScopeName;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new EquipUpGradePanelLocalRedDotRule());
        }

        public void Sync(int soldierId, IReadOnlyList<PlayerEquip> wearEquips, int currentPage)
        {
            if (IsOpen)
            {
                Close();
            }

            SoldierId = soldierId;
            CurrentPage = currentPage;
            _wearEquipIds.Clear();

            if (wearEquips != null)
            {
                for (int i = 0; i < wearEquips.Count; i++)
                {
                    _wearEquipIds.Add(wearEquips[i].Id);
                }
            }

            Open();
        }

        public void SetRefineContext(int selectedEquipId, int lockedEffectCount)
        {
            SelectedEquipId = selectedEquipId;
            LockedEffectCount = lockedEffectCount;

            if (IsOpen)
            {
                MarkAllDirty();
            }
        }

        public void SetCurrentPage(int currentPage)
        {
            if (CurrentPage == currentPage)
            {
                return;
            }

            CurrentPage = currentPage;
            if (IsOpen)
            {
                MarkAllDirty();
            }
        }

        public void NotifyDataChanged()
        {
            MarkAllDirty();
        }

        protected override void BuildNodes(RedDotTree tree)
        {
            for (int i = 0; i < _wearEquipIds.Count; i++)
            {
                EnsureNode(tree, Paths.WearSlot(_wearEquipIds[i]));
            }
        }

        protected override void ResetState()
        {
            SoldierId = 0;
            CurrentPage = EquipForgeRedDotHelper.ForgePageIndex;
            SelectedEquipId = 0;
            LockedEffectCount = 0;
            _wearEquipIds.Clear();
        }
    }
}
