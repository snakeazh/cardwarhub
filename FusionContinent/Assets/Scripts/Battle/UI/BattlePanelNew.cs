using Game.Battle;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("BattlePanel")]
    /// <summary>
    /// 战斗主界面 - 新框架版本
    /// 使用 SubPanelManager 管理多个子界面，支持子界面并存
    /// </summary>    
    public class BattlePanelNew : SubPanelManager<BattlePanelViewModel>
    {

        private BattlePanel mGameLandUI;

        // 当前激活的子界面类型
        private BattleSubPanelType mCurrentSubPanelType = BattleSubPanelType.Time;

        // 子界面全部打开完成回调（最简）
        public System.Action OnAllSubPanelsOpened;
        private int _expectedSubPanelCount;
        private int _openedSubPanelCount;
        private bool _allSubPanelsOpenedNotified;

        public bool AllSubPanelOpened
        {
            get
            {
                return _allSubPanelsOpenedNotified;
            }
        }
        void Awake()
        {
            // 获取 GameLandUI 引用
            mGameLandUI = GetComponent<BattlePanel>();
            if (mGameLandUI == null)
            {
                LogTool.LogError("[BattlePanelNew] GameLandUI.Default 为空！请确保 GameLandUI 先于 BattlePanelNew 初始化");
                return;
            }

            // 设置子界面容器为 GameLandUI 本身（因为所有UI都在 GameLandUI 下）
            // mSubPanelContainer = mGameLandUI.transform;

            LogTool.Log("[BattlePanelNew] 已关联 GameLandUI");
        }

        protected override void OnBindViewModel(BattlePanelViewModel vm)
        {
            if (mGameLandUI == null) return;

            // 订阅子界面显示事件
            vm.OnSubPanelShown += OnSubPanelShown;
            vm.OnSubPanelHidden += OnSubPanelHidden;
            vm.OnBattlePaused += OnBattlePaused;
            vm.OnBattleResumed += OnBattleResumed;
            vm.OnExitBattleRequested += OnExitBattleRequested;

            // 根据当前配置初始化子界面
            InitializeSubPanels(vm);
        }

        protected override void OnUnbindViewModel(BattlePanelViewModel vm)
        {
            if (vm == null) return;

            // 取消订阅
            vm.OnSubPanelShown -= OnSubPanelShown;
            vm.OnSubPanelHidden -= OnSubPanelHidden;
            vm.OnBattlePaused -= OnBattlePaused;
            vm.OnBattleResumed -= OnBattleResumed;
            vm.OnExitBattleRequested -= OnExitBattleRequested;

            // 解绑 SkillService
            var skillService = BattleServiceManager.Instance?.GetService<BattleSkillService>();
            skillService?.UnbindViewModel();

            // 清理所有子界面
            CloseAllSubPanels();
        }

        #region 子界面管理

        /// <summary>
        /// 初始化子界面
        /// </summary>
        private void InitializeSubPanels(BattlePanelViewModel vm)
        {
            _expectedSubPanelCount = 0;
            _openedSubPanelCount = 0;
            _allSubPanelsOpenedNotified = false;

            // 显示所有配置的子界面（仅统计实际会创建的子界面）
            foreach (var panelType in vm.ActiveSubPanels.Value)
            {
                if (ShowSubPanel(panelType, vm))
                {
                    _expectedSubPanelCount++;
                }
            }

            // 没有需要创建的子界面时，直接回调
            if (_expectedSubPanelCount == 0)
            {
                NotifyAllSubPanelsOpened();
            }
        }

        /// <summary>
        /// 显示指定类型的子界面
        /// </summary>
        private bool ShowSubPanel(BattleSubPanelType panelType, BattlePanelViewModel vm)
        {
            switch (panelType)
            {
                case BattleSubPanelType.Time:
                    ShowTimePanel();
                    return true;
                case BattleSubPanelType.Inter:
                    ShowInterPanel();
                    return true;
                case BattleSubPanelType.Schools:
                    ShowSchoolsPanel();
                    return true;
                case BattleSubPanelType.CityLevel:
                    // TODO: ShowCityLevelPanel();
                    return false;
                case BattleSubPanelType.School:
                    // TODO: ShowSchoolPanel();
                    return false;
                case BattleSubPanelType.SkillCard:
                    // TODO: ShowSkillsPanel();
                    return false;
                case BattleSubPanelType.PassiveCard:
                    ShowPassiveCardPanel();
                    return true;
                case BattleSubPanelType.CombatInfo:
                    // TODO: ShowCombatInfoPanel();
                    return false;
                case BattleSubPanelType.Settings:
                    // TODO: ShowSettingsPanel();
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 显示时间管理子界面
        /// </summary>
        private void ShowTimePanel()
        {
            // 从 BattleServiceManager 获取 BattleTimeService
            var timeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();
            if (timeService == null)
            {
                LogTool.LogError("[BattlePanelNew] BattleTimeService 未找到");
                return;
            }

            // 获取 BattleSkillService
            var skillService = BattleServiceManager.Instance?.GetService<BattleSkillService>();
            if (skillService == null)
            {
                LogTool.LogError("[BattlePanelNew] BattleSkillService 未找到");
                return;
            }

            // 创建 ViewModel
            var timeVm = new BattleTimeViewModel();
            timeVm.OnCreate();
            
            // 同步 Service 状态到 ViewModel
            SyncTimeServiceToViewModel(timeService, timeVm);

            // 绑定 SkillService 到 ViewModel
            skillService.BindViewModel(timeVm);

            // 创建本地子界面（不参与UIManager管理）
            CreateLocalSubPanel<BattleTimePanelNew, BattleTimeViewModel>(
                timeVm,
                (panel) =>
                {
                    LogTool.Log("[BattlePanelNew] BattleTimePanelNew 创建完成");
                    MarkSubPanelOpened();
                    
                    // 设置 AutoToggle 的 Image Transform 用于旋转动画
                    if (panel.mOldPanel?.AutoToggle != null)
                    {
                        var imageTransform = panel.mOldPanel.AutoToggle.transform.Find("Image");
                        if (imageTransform != null)
                        {
                            skillService.SetAutoToggleImage(imageTransform);
                        }
                    }
                    
                    // 强制更新技能卡牌状态（如果自动释放已开启）
                    skillService.ForceUpdateToAutoReleaseState();
                }
            );
        }

        /// <summary>
        /// 同步 TimeService 状态到 ViewModel
        /// </summary>
        private void SyncTimeServiceToViewModel(BattleTimeService service, BattleTimeViewModel vm)
        {
            // 同步时间
            vm.StartTime.Value = service.CurrentTime;

            // 同步倍速状态（二档：SpeedLevel=0 表示 x1，SpeedLevel>=1 表示 x2）
            vm.IsX2Speed.Value = service.SpeedLevel >= 1;
            vm.SpeedText.Value = service.SpeedLevel >= 1 ? "x2" : "x1";

            // 同步暂停状态
            vm.IsPaused.Value = service.IsPaused;
        }

        /// <summary>
        /// 显示交互子界面
        /// </summary>
        private void ShowInterPanel()
        {
            // 创建 ViewModel
            var interVm = new BattleInterViewModel();
            interVm.OnCreate();

            // 创建本地子界面（不参与UIManager管理）
            CreateLocalSubPanel<BattleInterPanelNew, BattleInterViewModel>(
                interVm,
                (panel) =>
                {
                    LogTool.Log("[BattlePanelNew] BattleInterPanelNew 创建完成");
                    MarkSubPanelOpened();
                }
            );
        }

        /// <summary>
        /// 显示流派子界面
        /// </summary>
        private void ShowSchoolsPanel()
        {
            // 创建 ViewModel
            var schoolsVm = new BattleSchoolsViewModel();
            schoolsVm.OnCreate();

            // 创建本地子界面（不参与UIManager管理）
            CreateLocalSubPanel<BattleSchoolsPanelNew, BattleSchoolsViewModel>(
                schoolsVm,
                (panel) =>
                {
                    LogTool.Log("[BattlePanelNew] BattleSchoolsPanelNew 创建完成");
                    MarkSubPanelOpened();
                }
            );
        }

        /// <summary>
        /// 显示技能子界面
        /// </summary>
        private void ShowSkillsPanel()
        {
            // 创建 ViewModel
            var skillsVm = new BattleSkillsViewModel();
            skillsVm.OnCreate();

            // 创建本地子界面（不参与UIManager管理）
            CreateLocalSubPanel<BattleSkillsPanelNew, BattleSkillsViewModel>(
                skillsVm,
                (panel) =>
                {
                    LogTool.Log("[BattlePanelNew] BattleSkillsPanelNew 创建完成");
                }
            );
        }

        /// <summary>
        /// 显示被动卡牌子界面
        /// </summary>
        private void ShowPassiveCardPanel()
        {
            // 创建 ViewModel
            var passiveCardVm = new BattlePassiveCardViewModel();
            passiveCardVm.OnCreate();

            // 创建本地子界面（不参与UIManager管理）
            CreateLocalSubPanel<BattlePassiveCardPanelNew, BattlePassiveCardViewModel>(
                passiveCardVm,
                (panel) =>
                {
                    LogTool.Log("[BattlePanelNew] BattlePassiveCardPanelNew 创建完成");
                    MarkSubPanelOpened();
                }
            );
        }

        private void MarkSubPanelOpened()
        {
            _openedSubPanelCount++;
            if (_openedSubPanelCount >= _expectedSubPanelCount)
            {
                NotifyAllSubPanelsOpened();
            }
        }

        private void NotifyAllSubPanelsOpened()
        {
            if (_allSubPanelsOpenedNotified)
            {
                return;
            }

            _allSubPanelsOpenedNotified = true;
            LogTool.Log("[BattlePanelNew] 所有子界面打开完成");
            OnAllSubPanelsOpened?.Invoke();
        }

        #endregion

        #region 事件处理

        private void OnSubPanelShown(BattleSubPanelType panelType)
        {
            LogTool.Log($"[BattlePanelNew] 子界面显示: {panelType}");
        }

        private void OnSubPanelHidden(BattleSubPanelType panelType)
        {
            LogTool.Log($"[BattlePanelNew] 子界面隐藏: {panelType}");
        }

        private void OnBattlePaused()
        {
            LogTool.Log("[BattlePanelNew] 战斗暂停");
        }

        private void OnBattleResumed()
        {
            LogTool.Log("[BattlePanelNew] 战斗恢复");
        }

        private void OnExitBattleRequested()
        {
            LogTool.Log("[BattlePanelNew] 请求退出战斗");
            
            // 调用 BattleSceneLifecycle 退出战斗
            var lifecycle = FindObjectOfType<BattleSceneLifecycle>();
            if (lifecycle != null)
            {
                lifecycle.ExitBattleScene();
            }
            else
            {
                LogTool.LogWarning("[BattlePanelNew] 未找到 BattleSceneLifecycle，直接关闭面板");
                CloseSelf();
            }
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 面板打开时的逻辑
            LogTool.Log("[BattlePanelNew] 打开");
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
            LogTool.Log("[BattlePanelNew] 显示");
        }

        protected override void OnHide()
        {
            LogTool.Log("[BattlePanelNew] 隐藏");
            base.OnHide();
        }

        protected override void OnClose()
        {
            LogTool.Log("[BattlePanelNew] 关闭");
            base.OnClose();
        }
    }
}
