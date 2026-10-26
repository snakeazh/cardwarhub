using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Game.Equip;
using Game.Equip.RedDot;
using Game.RedDot;
using Game.RedDot.View;
using QFramework;
using ScrollView;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成面板 - MVVM架构
    /// 复用原 FusionGemCompoundPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("FusionGemCompoundPanel")]
    public class FusionGemCompoundPanelNew : UIPanel<FusionGemCompoundPanelViewModel>
    {
        private FusionGemCompoundPanel mOldPanel;
        private UIScrollView mFusionScrollView;
        private Button mExitButton;
        private readonly FusionGemCompoundPanelRedDotScope mRedDotScope = new FusionGemCompoundPanelRedDotScope();

        #region 生命周期

        void Awake()
        {
            mOldPanel = GetComponent<FusionGemCompoundPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[FusionGemCompoundPanelNew] 找不到 FusionGemCompoundPanel 组件！");
                return;
            }

            LogTool.Log("[FusionGemCompoundPanelNew] 已关联 FusionGemCompoundPanel");
        }

        protected override void OnBindViewModel(FusionGemCompoundPanelViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            mRedDotScope.Open(vm.MainGemId.Value);
            BindFusionScrollView();
            BindButtons(vm);

            vm.OnCloseRequested += OnCloseRequested;
            vm.OnMainGemPresentationRefreshRequested += OnMainGemPresentationRefreshRequested;
            vm.FusionGemList.OnValueChanged += OnFusionGemListChanged;
            vm.SecondaryGemId.OnValueChanged += OnSecondaryGemIdChanged;
            vm.MainGemId.OnValueChanged += OnMainGemIdChanged;
        }

        protected override void OnUnbindViewModel(FusionGemCompoundPanelViewModel vm)
        {
            if (mOldPanel != null)
            {
                UnbindButtons();
            }

            ReleaseFusionScrollView();

            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnMainGemPresentationRefreshRequested -= OnMainGemPresentationRefreshRequested;
            vm.FusionGemList.OnValueChanged -= OnFusionGemListChanged;
            vm.SecondaryGemId.OnValueChanged -= OnSecondaryGemIdChanged;
            vm.MainGemId.OnValueChanged -= OnMainGemIdChanged;
            mRedDotScope.Close();
        }

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            if (VM != null)
            {
                mRedDotScope.SetMainGemId(VM.MainGemId.Value);
            }

            ShowGemScroll();
            VM?.RefreshMainGemPresentation();
        }

        public override void Show()
        {
            base.Show();
        }

        protected override void OnClose()
        {
            ReleaseFusionScrollView();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        /// <param name="mainGemId">主宝石流水 id（PlayerFusionGem.Id）</param>
        public void SetData(int mainGemId)
        {
            VM?.Initialize(mainGemId);
            mRedDotScope.SetMainGemId(mainGemId);
            ShowGemScroll();
        }

        #endregion

        #region 按钮绑定

        private void BindButtons(FusionGemCompoundPanelViewModel vm)
        {
            if (mOldPanel.ExitBtn != null)
            {
                mExitButton = mOldPanel.ExitBtn.GetComponent<Button>();
                if (mExitButton == null)
                {
                    mExitButton = mOldPanel.ExitBtn.GetOrAddComponent<Button>();
                }

                mExitButton.onClick.AddListener(OnExitBtnClicked);
            }

            if (mOldPanel.AdvancedBtn != null)
            {
                mOldPanel.AdvancedBtn.onClick.AddListener(OnAdvancedBtnClicked);
            }
        }

        private void UnbindButtons()
        {
            if (mExitButton != null)
            {
                mExitButton.onClick.RemoveListener(OnExitBtnClicked);
                mExitButton = null;
            }

            if (mOldPanel.AdvancedBtn != null)
            {
                mOldPanel.AdvancedBtn.onClick.RemoveListener(OnAdvancedBtnClicked);
            }
        }

        private void OnExitBtnClicked()
        {
            VM?.OnCloseCommand?.Execute();
        }

        private void OnAdvancedBtnClicked()
        {
            VM?.OnAdvancedCommand?.Execute();
        }

        #endregion

        #region ViewModel 事件（第 2 层）

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private void OnMainGemPresentationRefreshRequested()
        {
            RefreshMainGemPresentation(VM);
        }

        private void OnFusionGemListChanged(List<PlayerFusionGem> oldVal, List<PlayerFusionGem> newVal)
        {
            mRedDotScope.NotifyDataChanged();
            ShowGemScroll();
        }

        private void OnSecondaryGemIdChanged(int oldVal, int newVal)
        {
            RefreshGemScrollSelection();
        }

        private void OnMainGemIdChanged(int oldVal, int newVal)
        {
            mRedDotScope.SetMainGemId(newVal);
        }

        #endregion

        #region ScrollView

        private void BindFusionScrollView()
        {
            if (mOldPanel?.FusionScrollView == null)
            {
                LogTool.LogError("[FusionGemCompoundPanelNew] FusionScrollView 未绑定");
                return;
            }

            mFusionScrollView = mOldPanel.FusionScrollView.GetComponent<UIVerticalScrollViewLock>();
            if (mFusionScrollView == null)
            {
                mFusionScrollView = mOldPanel.FusionScrollView.GetComponent<UIScrollView>();
            }

            if (mFusionScrollView == null)
            {
                LogTool.LogError("[FusionGemCompoundPanelNew] FusionScrollView 缺少 UIScrollView 组件");
                return;
            }

            mFusionScrollView.SetUpdateFunc(OnUpdateFusionGemItem);
            mFusionScrollView.SetUpdateSelectFunc(OnSelectFusionGemItem);
        }

        private void ReleaseFusionScrollView()
        {
            if (mFusionScrollView != null)
            {
                mFusionScrollView.ReleaseScrollView();
                mFusionScrollView = null;
            }
        }

        private void ShowGemScroll()
        {
            if (mFusionScrollView == null)
            {
                return;
            }

            var list = VM?.FusionGemList.Value;
            var count = list != null ? list.Count : 0;

            mFusionScrollView.ShowList(0);
            mFusionScrollView.ShowList(count);
            mFusionScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mFusionScrollView.RefreshSelect();
        }

        private void RefreshGemScrollSelection()
        {
            if (mFusionScrollView == null)
            {
                return;
            }

            var count = VM?.FusionGemList.Value?.Count ?? 0;
            if (count <= 0)
            {
                return;
            }

            mFusionScrollView.ShowList(count);
        }

        private void OnUpdateFusionGemItem(int dataIndex, ScrollItem scrollItem)
        {
            var list = VM?.FusionGemList.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return;
            }

            var bagScrollItem = scrollItem as BagScrollItem;
            if (bagScrollItem == null)
            {
                return;
            }

            var gem = list[dataIndex];
            if (gem?.Config == null)
            {
                return;
            }

            var gemStoneEntity = bagScrollItem.GetComponent<GemStoneEntity>();
            if (gemStoneEntity == null)
            {
                return;
            }

            var secondaryGemId = VM?.SecondaryGemId.Value ?? 0;
            gemStoneEntity.SetData(new GemStoneData
            {
                GemId = gem.GemId,
                Equip = GlobalFunc.IsFusionGemEquipped(gem),
                Level = gem.Level,
                IsSelect = secondaryGemId > 0 && gem.Id == secondaryGemId,
            });

            var gemId = gem.Id;
            bagScrollItem.OnClick = () => VM?.OnSelectGemCommand?.Execute(gemId);
            BindSecondaryGemRedDot(bagScrollItem.transform, gemId);
        }

        private void BindSecondaryGemRedDot(Transform itemRoot, int gemId)
        {
            if (itemRoot == null || !mRedDotScope.IsOpen)
            {
                return;
            }

            ClearChildLocalRedDotViews(itemRoot);
            itemRoot.BindLocalRedDot(
                mRedDotScope,
                FusionGemCompoundPanelRedDotScope.Paths.Secondary(gemId));
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

        private void OnSelectFusionGemItem(int dataIndex, ScrollItem scrollItem)
        {
            var gem = GetGemByListIndex(dataIndex);
            if (gem == null)
            {
                return;
            }

            VM?.OnSelectGemCommand?.Execute(gem.Id);
        }

        #endregion

        #region 顶部展示（主宝石，统一刷新）

        private void RefreshMainGemPresentation(FusionGemCompoundPanelViewModel vm)
        {
            if (mOldPanel == null || vm == null)
            {
                return;
            }

            var gem = GetGemById(vm.MainGemId.Value);
            if (gem?.Config == null)
            {
                ClearMainGemPresentation();
                return;
            }

            if (mOldPanel.FusionIcon != null)
            {
                var sprite = Global.FusionGemIconSpriteAtlas?.GetSprite(gem.Config.Icon);
                mOldPanel.FusionIcon.sprite = sprite;
                mOldPanel.FusionIcon.enabled = sprite != null;
            }

            if (mOldPanel.FusionName != null)
            {
                mOldPanel.FusionName.text = gem.Config.Name ?? string.Empty;
            }

            if (mOldPanel.FusionProbabilityText != null)
            {
                mOldPanel.FusionProbabilityText.text = GameServiceManager.Get<FusionGemService>()
                    .GetGemLevelText(gem.Level);
            }

            RefreshFusionGemRoot(gem);
        }

        private void ClearMainGemPresentation()
        {
            if (mOldPanel.FusionIcon != null)
            {
                mOldPanel.FusionIcon.sprite = null;
                mOldPanel.FusionIcon.enabled = false;
            }

            if (mOldPanel.FusionName != null)
            {
                mOldPanel.FusionName.text = string.Empty;
            }

            if (mOldPanel.FusionProbabilityText != null)
            {
                mOldPanel.FusionProbabilityText.text = string.Empty;
            }

            if (mOldPanel.FusionGemRoot != null)
            {
                mOldPanel.FusionGemRoot.gameObject.SetActive(false);
            }
        }

        private void RefreshFusionGemRoot(PlayerFusionGem gem)
        {
            if (mOldPanel.FusionGemRoot == null)
            {
                return;
            }

            var isMaxLevel = gem.IsMaxLevel();
            mOldPanel.FusionGemRoot.gameObject.SetActive(!isMaxLevel);
            if (isMaxLevel)
            {
                return;
            }

            var fusionGemService = GameServiceManager.Get<FusionGemService>();
            var currentDisplayLevel = gem.Level;
            var nextDisplayLevel = gem.Level + 1;

            if (mOldPanel.GemProgress != null)
            {
                mOldPanel.GemProgress.value = Mathf.Clamp01(gem.Progress);
                mOldPanel.GemProgress.fillRect.gameObject.SetActive(mOldPanel.GemProgress.value >= 0.01f);
            }

            if (mOldPanel.GemStartLv != null)
            {
                mOldPanel.GemStartLv.text = fusionGemService.GetGemLevelText(currentDisplayLevel);
            }

            if (mOldPanel.GemEndLv != null)
            {
                mOldPanel.GemEndLv.text = fusionGemService.GetGemLevelText(nextDisplayLevel);
            }
        }

        private PlayerFusionGem GetGemById(int gemId)
        {
            if (gemId <= 0 || Global.PlayerFusionGems == null)
            {
                return null;
            }

            return Global.PlayerFusionGems.TryGetValue(gemId, out var gem) ? gem : null;
        }

        private PlayerFusionGem GetGemByListIndex(int dataIndex)
        {
            var list = VM?.FusionGemList.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return null;
            }

            return list[dataIndex];
        }

        #endregion
    }
}
