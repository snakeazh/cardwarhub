using UIFramework;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 战斗主界面 ViewModel
    /// 管理所有战斗子界面的显示和切换
    /// </summary>
    public class BattlePanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 当前显示的子界面类型列表
        /// </summary>
        public BindableProperty<List<BattleSubPanelType>> ActiveSubPanels { get; } = 
            new BindableProperty<List<BattleSubPanelType>>(new List<BattleSubPanelType>());

        /// <summary>
        /// 是否暂停
        /// </summary>
        public BindableProperty<bool> IsPaused { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 当前波次
        /// </summary>
        public BindableProperty<int> CurrentWave { get; } = new BindableProperty<int>(1);

        /// <summary>
        /// 总波次
        /// </summary>
        public BindableProperty<int> TotalWaves { get; } = new BindableProperty<int>(10);
        #endregion

        #region 命令
        public Command<BattleSubPanelType> OnShowSubPanelCommand { get; private set; }
        public Command<BattleSubPanelType> OnHideSubPanelCommand { get; private set; }
        public Command OnPauseCommand { get; private set; }
        public Command OnResumeCommand { get; private set; }
        public Command OnExitBattleCommand { get; private set; }
        #endregion

        #region 依赖
        private BattleTimeService mTimeService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            
            // 从 BattleServiceManager 获取 Service
            mTimeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();
            
            // 初始化命令
            OnShowSubPanelCommand = new Command<BattleSubPanelType>(OnShowSubPanel);
            OnHideSubPanelCommand = new Command<BattleSubPanelType>(OnHideSubPanel);
            OnPauseCommand = new Command(OnPause);
            OnResumeCommand = new Command(OnResume);
            OnExitBattleCommand = new Command(OnExitBattle);
            
            // 默认显示时间管理、交互、流派、技能和被动卡牌子界面
            ShowSubPanel(BattleSubPanelType.Time);
            ShowSubPanel(BattleSubPanelType.Inter);
            ShowSubPanel(BattleSubPanelType.Schools);
            ShowSubPanel(BattleSubPanelType.Skills);
            ShowSubPanel(BattleSubPanelType.PassiveCard);
            
            LogTool.Log("[BattlePanelViewModel] 创建完成");
        }

        #region 命令实现

        private void OnShowSubPanel(BattleSubPanelType panelType)
        {
            ShowSubPanel(panelType);
        }

        private void OnHideSubPanel(BattleSubPanelType panelType)
        {
            HideSubPanel(panelType);
        }

        private void OnPause()
        {
            IsPaused.Value = true;
            mTimeService?.Pause();
            OnBattlePaused?.Invoke();
        }

        private void OnResume()
        {
            IsPaused.Value = false;
            mTimeService?.Resume();
            OnBattleResumed?.Invoke();
        }

        private void OnExitBattle()
        {
            OnExitBattleRequested?.Invoke();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示子界面
        /// </summary>
        public void ShowSubPanel(BattleSubPanelType panelType)
        {
            if (!ActiveSubPanels.Value.Contains(panelType))
            {
                ActiveSubPanels.Value.Add(panelType);
                ActiveSubPanels.Value = new List<BattleSubPanelType>(ActiveSubPanels.Value); // 触发通知
                OnSubPanelShown?.Invoke(panelType);
            }
        }

        /// <summary>
        /// 隐藏子界面
        /// </summary>
        public void HideSubPanel(BattleSubPanelType panelType)
        {
            if (ActiveSubPanels.Value.Contains(panelType))
            {
                ActiveSubPanels.Value.Remove(panelType);
                ActiveSubPanels.Value = new List<BattleSubPanelType>(ActiveSubPanels.Value); // 触发通知
                OnSubPanelHidden?.Invoke(panelType);
            }
        }

        /// <summary>
        /// 切换子界面显示状态
        /// </summary>
        public void ToggleSubPanel(BattleSubPanelType panelType)
        {
            if (ActiveSubPanels.Value.Contains(panelType))
            {
                HideSubPanel(panelType);
            }
            else
            {
                ShowSubPanel(panelType);
            }
        }

        /// <summary>
        /// 检查子界面是否显示
        /// </summary>
        public bool IsSubPanelActive(BattleSubPanelType panelType)
        {
            return ActiveSubPanels.Value.Contains(panelType);
        }

        #endregion

        #region 事件

        public System.Action<BattleSubPanelType> OnSubPanelShown;
        public System.Action<BattleSubPanelType> OnSubPanelHidden;
        public System.Action OnBattlePaused;
        public System.Action OnBattleResumed;
        public System.Action OnExitBattleRequested;

        #endregion
    }

    /// <summary>
    /// 战斗子界面类型
    /// </summary>
    public enum BattleSubPanelType
    {
        Time,           // 时间管理
        Inter,          // 交互面板（金币、购买按钮）
        Schools,        // 流派系统
        Skills,         // 技能卡牌
        PassiveCard,    // 被动卡牌
        CityLevel,      // 城市等级
        School,         // 流派系统（旧）
        SkillCard,      // 技能卡牌（旧）
        CombatInfo,     // 战斗信息
        Settings,       // 设置
    }
}
