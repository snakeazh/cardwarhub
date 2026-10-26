using System.Collections.Generic;
using Game.RedDot;

namespace Game.Soldier.RedDot
{
    public sealed class SoldierPanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "SoldierPanel";

        public override string Scope => ScopeName;

        public static class Paths
        {
            public const string CurrentUpgrade = "Current/Upgrade";
            public const string CurrentDevelop = "Current/Develop";
            public const string CurrentAwakening = "Current/Awakening";
            public const string ArrowLeft = "Arrow/Left";
            public const string ArrowRight = "Arrow/Right";

            public static string CardUpgrade(int cardId) => $"Card/{cardId}/Upgrade";
            public static string CardDevelop(int cardId) => $"Card/{cardId}/Develop";
            public static string CardActivate(int cardId) => $"Card/{cardId}/Activate";
            /// <summary>已拥有英灵列表提示：本体可升级/养成/觉醒。</summary>
            public static string CardOwnedHint(int cardId) => $"Card/{cardId}/OwnedHint";
            public static string CardAwakening(int cardId) => $"Card/{cardId}/Awakening";
        }

        int _selectedCardId;
        IReadOnlyList<int> _soldierCardIds;

        // 引用计数：SoldierMainPanel 与 SoldierPanel 两个面板各自持有 scope，
        // 但局部红点 tree 按 scope 名唯一。计数到 0 才真正销毁 tree，
        // 避免一个面板 Close 时把另一个面板仍在用的 tree 一起销毁。
        static int s_refCount;

        public int SelectedCardId => _selectedCardId;
        public IReadOnlyList<int> SoldierCardIds => _soldierCardIds;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new SoldierPanelLocalRedDotRule());
        }

        public void Open(IReadOnlyList<int> soldierCardIds)
        {
            _soldierCardIds = soldierCardIds;
            Open();
        }

        /// <summary>
        /// 多面板共享引用：打开当前 scope 并 +1 引用计数。
        /// tree 已存在时会复用（基类 Open 内部处理），不会重复创建。
        /// </summary>
        public void OpenShared(IReadOnlyList<int> soldierCardIds)
        {
            s_refCount++;
            Open(soldierCardIds);
        }

        /// <summary>
        /// 多面板共享引用：-1 引用计数，仅当计数归零才真正 Close 销毁 tree。
        /// 这样 SoldierMainPanel / SoldierPanel 各自开关互不影响。
        /// </summary>
        public void CloseShared()
        {
            if (s_refCount > 0)
            {
                s_refCount--;
            }

            if (s_refCount > 0)
            {
                return;
            }

            Close();
        }

        protected override void BuildNodes(RedDotTree tree)
        {
            RegisterNodeType(tree, RedNodeType.SoliderUpgradeCard, Paths.CurrentUpgrade);
            RegisterNodeType(tree, RedNodeType.SoliderDevelopCard, Paths.CurrentDevelop);
            RegisterNodeType(tree, RedNodeType.SoliderAwakeningCard, Paths.CurrentAwakening);

            if (_soldierCardIds != null)
            {
                for (int i = 0; i < _soldierCardIds.Count; i++)
                {
                    int cardId = _soldierCardIds[i];
                    EnsureNode(tree, Paths.CardUpgrade(cardId));
                    EnsureNode(tree, Paths.CardDevelop(cardId));
                    EnsureNode(tree, Paths.CardActivate(cardId));
                    EnsureNode(tree, Paths.CardOwnedHint(cardId));
                    EnsureNode(tree, Paths.CardAwakening(cardId));
                }
            }
        }

        public void SetSelectedCard(int cardId)
        {
            _selectedCardId = cardId;
            MarkDirty(
                RedNodeType.SoliderUpgradeCard,
                RedNodeType.SoliderDevelopCard,
                RedNodeType.SoliderAwakeningCard);
        }

        public void NotifyUpgradeDataChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.SoliderCanUpgrade);
            Service.MarkDirty(RedNodeType.SoliderCanActivate);
            Service.MarkDirty(RedNodeType.SoliderCanAwakening);
        }

        public void NotifyAwakeningDataChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.SoliderCanAwakening);
        }

        public void NotifyDevelopChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.SoliderCanUpgrade);
        }

        protected override void ResetState()
        {
            _selectedCardId = 0;
            _soldierCardIds = null;
        }
    }
}
