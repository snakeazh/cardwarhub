using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.Challenge;
using Game.Challenge.RedDot;
using Game.RedDot;
using Game.RedDot.View;
using QFramework;
using ScrollView;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("MonsterAtlasPanel")]
    public class MonsterAtlasPanelNew : UIPanel<MonsterAtlasPanelViewModel>
    {
        private MonsterAtlasPanel mOldPanel;

        // 具名回调，便于成对解绑
        private UnityAction mCloseBtnHandler;
        private UnityAction mMaskHandler;
        private UnityAction<bool> mNormalToggleHandler;
        private UnityAction<bool> mEliteToggleHandler;
        private UnityAction<bool> mBossToggleHandler;
        private System.Action mItemsChangedHandler;
        private System.Action<int> mOpenDetailHandler;
        private System.Action mClosePanelHandler;
        private System.Action<string, string> mUnlockProgressHandler;

        private List<MonsterAtlasItemData> mItems = new List<MonsterAtlasItemData>();
        private bool mIsClosing;
        private System.Action<object> mAtlasStatusChangedHandler;
        private readonly MonsterAtlasPanelRedDotScope mRedDotScope = new MonsterAtlasPanelRedDotScope();

        void Awake()
        {
            mOldPanel = GetComponent<MonsterAtlasPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MonsterAtlasPanelNew] 找不到 MonsterAtlasPanel 组件");
            }
        }

        protected override void OnBindViewModel(MonsterAtlasPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mItems = vm.Items.Value ?? new List<MonsterAtlasItemData>();
            mRedDotScope.Open();

            // 关闭入口：CloseBtn 与 Mask 点击都关闭
            mCloseBtnHandler = OnCloseButtonClicked;
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);
            mMaskHandler = OnCloseButtonClicked;
            mOldPanel.Mask.onClick.AddListener(mMaskHandler);

            // 分类筛选 Tab：三个 Toggle 互斥，选中时切到对应品质
            // 用 SetIsOnWithoutNotify 同步初始态，避免触发 onValueChanged 循环
            mNormalToggleHandler = isOn => { if (isOn) vm.OnSwitchQualityCommand.Execute((int)MonsterQuality.Ordinary); };
            mEliteToggleHandler = isOn => { if (isOn) vm.OnSwitchQualityCommand.Execute((int)MonsterQuality.Elite); };
            mBossToggleHandler = isOn => { if (isOn) vm.OnSwitchQualityCommand.Execute((int)MonsterQuality.Boss); };
            mOldPanel.NormalToggle.onValueChanged.AddListener(mNormalToggleHandler);
            mOldPanel.EliteToggle.onValueChanged.AddListener(mEliteToggleHandler);
            mOldPanel.BossToggle.onValueChanged.AddListener(mBossToggleHandler);
            SyncToggleFromVm(vm);
            BindQualityToggleRedDots();

            // ScrollView 填充回调
            mOldPanel.CardScrollview.SetUpdateFunc(OnUpdateCardItem);

            // 解锁进度文案绑定
            mUnlockProgressHandler = (o, n) => { if (mOldPanel.Tip != null) mOldPanel.Tip.text = n; };
            vm.UnlockProgressText.OnValueChanged += mUnlockProgressHandler;
            if (mOldPanel.Tip != null) mOldPanel.Tip.text = vm.UnlockProgressText.Value;

            // VM 事件
            mItemsChangedHandler = OnItemsChanged;
            vm.OnItemsChanged += mItemsChangedHandler;
            mOpenDetailHandler = OnOpenDetailRequested;
            vm.OnOpenDetailRequested += mOpenDetailHandler;
            mClosePanelHandler = OnClosePanelRequested;
            vm.OnClosePanel += mClosePanelHandler;

            // 订阅图鉴状态变化（详情面板领奖后广播），刷新列表解锁/红点
            mAtlasStatusChangedHandler = OnAtlasStatusChanged;
            Global.Publisher.Subscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, mAtlasStatusChangedHandler);
        }

        protected override void OnUnbindViewModel(MonsterAtlasPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }
            if (mMaskHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(mMaskHandler);
                mMaskHandler = null;
            }

            if (mNormalToggleHandler != null)
            {
                mOldPanel.NormalToggle.onValueChanged.RemoveListener(mNormalToggleHandler);
                mNormalToggleHandler = null;
            }
            if (mEliteToggleHandler != null)
            {
                mOldPanel.EliteToggle.onValueChanged.RemoveListener(mEliteToggleHandler);
                mEliteToggleHandler = null;
            }
            if (mBossToggleHandler != null)
            {
                mOldPanel.BossToggle.onValueChanged.RemoveListener(mBossToggleHandler);
                mBossToggleHandler = null;
            }

            if (mUnlockProgressHandler != null)
            {
                vm.UnlockProgressText.OnValueChanged -= mUnlockProgressHandler;
                mUnlockProgressHandler = null;
            }

            if (mItemsChangedHandler != null)
            {
                vm.OnItemsChanged -= mItemsChangedHandler;
                mItemsChangedHandler = null;
            }
            if (mOpenDetailHandler != null)
            {
                vm.OnOpenDetailRequested -= mOpenDetailHandler;
                mOpenDetailHandler = null;
            }
            if (mClosePanelHandler != null)
            {
                vm.OnClosePanel -= mClosePanelHandler;
                mClosePanelHandler = null;
            }

            if (mAtlasStatusChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, mAtlasStatusChangedHandler);
                mAtlasStatusChangedHandler = null;
            }

            ReleaseScrollView();
            mRedDotScope.Close();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null || VM == null) return;
            mIsClosing = false;
            BindQualityToggleRedDots();
            mRedDotScope.NotifyDataChanged();
            RefreshList();
            mOldPanel.CardScrollview.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.CardScrollview.PlayScaleAnimationDOTween();
        }

        // ---------- ScrollView 回调 ----------

        private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || dataIndex >= mItems.Count) return;
            var item = mItems[dataIndex];

            var bagScrollItem = (BagScrollItem)scrollItem;

            // 图标：Global.MonsterIconSpriteAtlas.GetSpriteExt 按 CardBaseConfig.Icon 加载
            var slot = bagScrollItem.GetComponent<MonsterCardAtlasSlot>();
            slot.SetData(new MonsterCardAtlasSlotData
            {
                Quality = item.Quality,
                CardName = item.Name,
                IconSprite = Global.MonsterIconSpriteAtlas.GetSpriteExt(item.Icon),
                IsUnlock = item.Status != RewardStatus.Default,
                Label = item.Label,
            });

            // 红点：可领取时显示（局部树）；清掉旧 LocalRedDot 避免双红点
            LocalRedDot oldLocalRed = bagScrollItem.GetComponent<LocalRedDot>();
            if (oldLocalRed != null)
            {
                oldLocalRed.SetRed(0);
            }

            BindMonsterItemRedDot(bagScrollItem.transform, item.MonsterId);

            // 点击：未解锁不响应；解锁后通知 VM → 打开详情面板
            bagScrollItem.OnClick = () =>
            {
                if (item.Status == RewardStatus.Default) return;
                VM.OnItemClickCommand.Execute(item.MonsterId);
            };
        }

        private void BindQualityToggleRedDots()
        {
            if (!mRedDotScope.IsOpen)
            {
                return;
            }

            BindToggleRedDot(mOldPanel.NormalToggle.transform, MonsterQuality.Ordinary);
            BindToggleRedDot(mOldPanel.EliteToggle.transform, MonsterQuality.Elite);
            BindToggleRedDot(mOldPanel.BossToggle.transform, MonsterQuality.Boss);
        }

        private void BindToggleRedDot(Transform toggleRoot, MonsterQuality quality)
        {
            toggleRoot.BindLocalRedDot(mRedDotScope, MonsterAtlasPanelRedDotScope.Paths.Quality(quality));
            LocalRedDotView view = toggleRoot.GetComponent<LocalRedDotView>();
            if (view != null)
            {
                view.RefreshWatch();
            }
        }

        private void BindMonsterItemRedDot(Transform itemRoot, int monsterId)
        {
            if (!mRedDotScope.IsOpen)
            {
                return;
            }

            ClearChildLocalRedDotViews(itemRoot);
            itemRoot.BindLocalRedDot(mRedDotScope, MonsterAtlasPanelRedDotScope.Paths.Monster(monsterId));
            LocalRedDotView view = itemRoot.GetComponent<LocalRedDotView>();
            if (view != null)
            {
                view.RefreshWatch();
            }
        }

        private void ClearChildLocalRedDotViews(Transform root)
        {
            LocalRedDotView[] childViews = root.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < childViews.Length; i++)
            {
                LocalRedDotView view = childViews[i];
                if (view.transform != root)
                {
                    Destroy(view.gameObject);
                }
            }
        }

        // ---------- 交互 ----------

        /// <summary>按 VM 当前筛选品质，无回调地同步三个 Toggle 的选中态</summary>
        private void SyncToggleFromVm(MonsterAtlasPanelViewModel vm)
        {
            var q = vm.SelectedQuality.Value;
            mOldPanel.NormalToggle.SetIsOnWithoutNotify(q == MonsterQuality.Ordinary);
            mOldPanel.EliteToggle.SetIsOnWithoutNotify(q == MonsterQuality.Elite);
            mOldPanel.BossToggle.SetIsOnWithoutNotify(q == MonsterQuality.Boss);
        }

        private void OnCloseButtonClicked()
        {
            if (mIsClosing) return;
            mIsClosing = true;
            PlayCloseAnimation(() => CloseSelf());
        }

        private void OnOpenDetailRequested(int monsterId)
        {
            // 打开详情面板（已迁移为 MVVM）：构造 VM 并 Setup(monsterId) 后打开
            var vm = new MonsterAtlasDetailPanelViewModel();
            vm.OnCreate();
            vm.Setup(monsterId);
            UIFramework.UIManager.OpenPanelAsync<MonsterAtlasDetailPanelNew, MonsterAtlasDetailPanelViewModel>(
                vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnClosePanelRequested()
        {
            OnCloseButtonClicked();
        }

        private void OnItemsChanged()
        {
            // VM 筛选/刷新后 Items 已替换为新 List，这里重新取引用
            mItems = VM.Items.Value;
            RefreshList();
        }

        // 详情面板领奖后广播，重新读取解锁状态并刷新列表
        private void OnAtlasStatusChanged(object _)
        {
            if (VM == null) return;
            VM.RefreshStatus();
            BindQualityToggleRedDots();
            mRedDotScope.NotifyDataChanged();
        }

        // ---------- 刷新 ----------

        private void RefreshList()
        {
            if (mOldPanel.CardScrollview == null) return;
            mOldPanel.CardScrollview.ShowList(0);
            mOldPanel.CardScrollview.ShowList(mItems.Count);
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
            if (mOldPanel.CardScrollview == null) return;
            mOldPanel.CardScrollview.ShowList(0);
            mOldPanel.CardScrollview.SetUpdateFunc(null);
        }
    }
}
