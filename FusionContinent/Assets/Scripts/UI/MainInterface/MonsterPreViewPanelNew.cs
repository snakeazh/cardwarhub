using System.Collections.Generic;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.Challenge;
using ScrollView;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;

namespace FusionContinent
{
    [PrefabPath("MonsterPreViewPanel")]
    public class MonsterPreViewPanelNew : UIPanel<MonsterPreViewPanelViewModel>
    {
        private MonsterPreViewPanel mOldPanel;

        private bool mIsClosing;

        void Awake()
        {
            mOldPanel = GetComponent<MonsterPreViewPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MonsterPreViewPanelNew] 找不到 MonsterPreViewPanel 组件");
            }
        }

        protected override void OnBindViewModel(MonsterPreViewPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭入口：Quit 与 ExitBtn 都关闭
            mOldPanel.Quit.onClick.AddListener(OnCloseButtonClicked);
            mOldPanel.ExitBtn.onClick.AddListener(OnCloseButtonClicked);

            // ScrollView 逐项填充回调
            mOldPanel.MonsterSCView.SetUpdateFunc(OnUpdateMonsterItem);
            mOldPanel.RewardSCView.SetUpdateFunc(OnUpdateRewardItem);

            // VM 列表就绪事件
            vm.OnItemsChanged += OnItemsChanged;
            vm.OnRewardItemsChanged += OnRewardItemsChanged;
            vm.OnClosePanel += OnClosePanelRequested;
        }

        protected override void OnUnbindViewModel(MonsterPreViewPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.Quit.onClick.RemoveListener(OnCloseButtonClicked);
            mOldPanel.ExitBtn.onClick.RemoveListener(OnCloseButtonClicked);

            vm.OnItemsChanged -= OnItemsChanged;
            vm.OnRewardItemsChanged -= OnRewardItemsChanged;
            vm.OnClosePanel -= OnClosePanelRequested;

            ReleaseScrollView();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null || VM == null) return;

            mIsClosing = false;
            RefreshList();
            RefreshRewardList();
            mOldPanel.MonsterSCView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.MonsterSCView.PlayScaleAnimationDOTween();
        }

        // ---------- ScrollView 回调 ----------

        private void OnUpdateMonsterItem(int dataIndex, ScrollItem scrollItem)
        {
            var items = VM.Items.Value;
            if (dataIndex < 0 || dataIndex >= items.Count) return;

            var item = items[dataIndex];
            var bagScrollItem = (BagScrollItem)scrollItem;

            // 预览面板：所有怪物全部可见（不锁）
            var slot = bagScrollItem.GetComponent<MonsterCardAtlasSlot>();
            slot.SetData(new MonsterCardAtlasSlotData
            {
                Quality = item.Quality,
                CardName = item.Name,
                IconSprite = Global.MonsterIconSpriteAtlas.GetSpriteExt(item.Icon),
                IsUnlock = true,
                Label = item.Label,
            });

            // 点击怪物卡片 → 在卡片正下方弹出详情小窗
            int monsterId = item.MonsterId;
            Transform cardTransform = bagScrollItem.transform;
            bagScrollItem.OnClick = () => OpenDetailPop(monsterId, cardTransform);
        }

        private void OnUpdateRewardItem(int dataIndex, ScrollItem scrollItem)
        {
            var rewards = VM.RewardItems.Value;
            if (dataIndex < 0 || dataIndex >= rewards.Count) return;

            var reward = rewards[dataIndex];
            var bagScrollItem = (BagScrollItem)scrollItem;
            bagScrollItem.GetComponent<RewardEntity>().SetData(reward);
        }

        // ---------- 交互 ----------

        private void OnCloseButtonClicked()
        {
            if (mIsClosing) return;
            mIsClosing = true;
            PlayCloseAnimation(() => CloseSelf());
        }

        private void OnClosePanelRequested()
        {
            OnCloseButtonClicked();
        }

        private void OnItemsChanged()
        {
            RefreshList();
        }

        private void OnRewardItemsChanged()
        {
            RefreshRewardList();
        }

        // 点击怪物卡片，在卡片正下方弹出详情小窗
        private void OpenDetailPop(int monsterId, Transform cardTransform)
        {
            var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(monsterId);
            if (cardCfg == null)
            {
                LogTool.LogError("[MonsterPreViewPanelNew] 配置缺失，monsterId=" + monsterId);
                return;
            }

            var anchor = cardTransform.Find("TweenTarget");
            if (anchor == null)
            {
                LogTool.LogError("[MonsterPreViewPanelNew] 找不到 TweenTarget 节点，monsterId=" + monsterId);
                return;
            }

            GlobalFunc.OpenCommonDetailPopPanel(anchor, cardCfg.Name, cardCfg.Desc);
        }

        // ---------- 刷新 ----------

        private void RefreshList()
        {
            if (mOldPanel.MonsterSCView == null || VM == null) return;

            var items = VM.Items.Value;
            mOldPanel.MonsterSCView.ShowList(0);
            mOldPanel.MonsterSCView.ShowList(items != null ? items.Count : 0);
        }

        private void RefreshRewardList()
        {
            if (mOldPanel.RewardSCView == null || VM == null) return;

            var rewards = VM.RewardItems.Value;
            mOldPanel.RewardSCView.ShowList(0);
            mOldPanel.RewardSCView.ShowList(rewards != null ? rewards.Count : 0);
        }

        // ---------- 动画 ----------

        private void PlayCloseAnimation(UnityAction onComplete)
        {
            if (mOldPanel.CloseAnimation == null)
            {
                onComplete();
                return;
            }
            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, onComplete);
        }

        private void ReleaseScrollView()
        {
            if (mOldPanel.MonsterSCView != null)
            {
                mOldPanel.MonsterSCView.ShowList(0);
                mOldPanel.MonsterSCView.SetUpdateFunc(null);
            }
            if (mOldPanel.RewardSCView != null)
            {
                mOldPanel.RewardSCView.ShowList(0);
                mOldPanel.RewardSCView.SetUpdateFunc(null);
            }
        }
    }
}
