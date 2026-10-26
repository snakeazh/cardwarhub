using System;
using Config;
using FusionContinent.Model;
using Game.Soldier;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;

namespace FusionContinent
{
    /// <summary>
    /// 底栏容器面板 - 聚合合成/分解两个子面板。
    /// 底栏 ExitBtn 负责退出整个界面，ConbineBtn/ResolveBtn（UISelectableToggle）负责切换子面板。
    /// 说明：ResonanceBtn 原为共鸣页签（后复用为抽奖页签），抽奖模块移除后运行时代码整体隐藏（prefab 节点未动）。
    /// </summary>
    [PrefabPath("BottomBar")]
    public class BottomBarNew : SubPanelManager<BottomBarViewModel>
    {
        private BottomBar mOldPanel;

        // Toggle 值变化回调（UnityEvent<bool>，需用 UnityAction<bool> 接收以便精确解绑）
        private UnityAction<bool> _conbineToggleHandler;
        private UnityAction<bool> _resolveToggleHandler;

        // SubPanelManager 基类默认返回 false，这里重写为 true，
        // 使面板可见时触发 UpdateUserToolForPanel，从而显示顶部资源栏（借鉴 ActivityPanelNew）
        protected override bool ShouldUpdateUserToolOnBecameVisible()
        {
            return true;
        }

        void Awake()
        {
            mOldPanel = GetComponent<BottomBar>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BottomBarNew] 找不到 BottomBar 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }
            mOldPanel.enabled = false;

            // 抽奖模块移除：隐藏原共鸣/抽奖页签（参照 GeneralSkillPanelNew 隐藏 ResonanceBtn 的做法）
            mOldPanel.ResonanceBtn.gameObject.SetActive(false);
        }

        protected override void OnBindViewModel(BottomBarViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.ExitBtn.onClick.AddListener(OnExitButtonClicked);

            vm.OnClosePanel += OnClosePanel;
            vm.OnOpenConbinePanel += OnOpenConbinePanel;
            vm.OnOpenResolvePanel += OnOpenResolvePanel;

            // 合成页签：isOn 变 true 时切到合成界面
            _conbineToggleHandler = (isOn) =>
            {
                if (isOn) vm.OnTabSelectedCommand.Execute(BottomBarViewModel.TabType.Conbine);
            };
            mOldPanel.ConbineBtn.onValueChanged.AddListener(_conbineToggleHandler);

            // 分解页签：isOn 变 true 时切到分解界面
            _resolveToggleHandler = (isOn) =>
            {
                if (isOn)
                {
                    UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 3);
                    vm.OnTabSelectedCommand.Execute(BottomBarViewModel.TabType.Resolve);
                }
            };
            mOldPanel.ResolveBtn.onValueChanged.AddListener(_resolveToggleHandler);

            if (UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 3, mOldPanel.ResolveBtn.GetComponent<RectTransform>());
            }
        }

        protected override void OnUnbindViewModel(BottomBarViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.ExitBtn.onClick.RemoveListener(OnExitButtonClicked);

            vm.OnClosePanel -= OnClosePanel;
            vm.OnOpenConbinePanel -= OnOpenConbinePanel;
            vm.OnOpenResolvePanel -= OnOpenResolvePanel;

            if (_conbineToggleHandler != null)
            {
                mOldPanel.ConbineBtn.onValueChanged.RemoveListener(_conbineToggleHandler);
                _conbineToggleHandler = null;
            }
            if (_resolveToggleHandler != null)
            {
                mOldPanel.ResolveBtn.onValueChanged.RemoveListener(_resolveToggleHandler);
                _resolveToggleHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            // 打开时隐藏主界面底部按钮（借鉴 ActivityPanelNew）
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, false);

            // 先把所有页签置为未选中（不触发回调），避免 onValueChanged 重复触发
            mOldPanel.ConbineBtn.SetIsOnWithoutNotify(false);
            mOldPanel.ResolveBtn.SetIsOnWithoutNotify(false);

            // 直接置 isOn = true 触发 onValueChanged 回调，走与用户点击完全一致的路径
            // 这样能确保子面板打开逻辑（含 HideCurrentSubPanel + VM 分发）被完整执行，避免手动调用造成的时序问题
            var tab = VM.InitialTab;
            switch (tab)
            {
                case BottomBarViewModel.TabType.Conbine:
                    mOldPanel.ConbineBtn.isOn = true;
                    break;
                case BottomBarViewModel.TabType.Resolve:
                    mOldPanel.ResolveBtn.isOn = true;
                    break;
            }
        }

        #region 子面板切换

        private void OnOpenConbinePanel()
        {
            // 切换前隐藏当前子面板（首次打开时 mCurrentSubPanel 为 null，安全跳过）
            HideCurrentSubPanel();
            var vm = new GeneralCardConbinePanelViewModel();
            vm.OnCreate();
            OpenOrReuseSubPanel<GeneralCardConbinePanelNew, GeneralCardConbinePanelViewModel>(vm);
        }

        private void OnOpenResolvePanel()
        {
            HideCurrentSubPanel();
            var vm = new GeneralCardResolvePanelViewModel();
            vm.OnCreate();
            OpenOrReuseSubPanel<GeneralCardResolvePanelNew, GeneralCardResolvePanelViewModel>(vm);
        }

        #endregion

        #region 事件处理

        private void OnExitButtonClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        private void OnClosePanel()
        {
            HideCurrentSubPanel();

            // 关闭时恢复主界面底部按钮（借鉴 ActivityPanelNew）
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);

            CloseSelf();
        }

        #endregion
    }
}
