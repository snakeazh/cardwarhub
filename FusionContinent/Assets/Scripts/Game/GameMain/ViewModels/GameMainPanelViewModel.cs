using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;
using Config;
using System.Linq;
using FusionContinent;
using FusionContinent.Model;

namespace Game.GameMain
{
    /// <summary>
    /// 游戏主界面 ViewModel
    /// 管理底部导航
    /// </summary>
    public class GameMainPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<GameMainButtonType> CurrentPanelType { get; } = new BindableProperty<GameMainButtonType>();
        public BindableProperty<int> SelectedButtonIndex { get; } = new BindableProperty<int>(-1);
        public BindableProperty<bool> CanClick { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> IsAnimating { get; } = new BindableProperty<bool>(false);
        
        // 底部按钮可见性
        public BindableProperty<bool> IsBottomVisible { get; } = new BindableProperty<bool>(true);
        #endregion

        #region 命令
        public Command<int> OnSelectButtonCommand { get; private set; }
        public Command<GameMainButtonType> OnChangePanelCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 依赖
        private GameMainService mGameMainService;
        #endregion

        #region 配置数据
        private List<GameMainButtonStruct> mButtonList;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mGameMainService = GameServiceManager.Get<GameMainService>();

            // 加载配置
            mButtonList = DataConfigManager.Tables.TbGameConst.GameMainButtonList;

            // 初始化命令
            OnSelectButtonCommand = new Command<int>(OnSelectButton, CanSelectButton);
            OnChangePanelCommand = new Command<GameMainButtonType>(OnChangePanel);
            OnCloseCommand = new Command(OnClose);
            
            // 加载资源数据（用户数据已移到 UserToolPanel）
            LoadResourceData();
            
            // 订阅体力变化消息
            SubscribeStaminaChanges();
            
            LogTool.Log("[GameMainPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // 取消订阅体力变化消息
            UnsubscribeStaminaChanges();
            
            LogTool.Log("[GameMainPanelViewModel] 销毁");
        }

        #region 命令实现

        private void OnSelectButton(int index)
        {
            if (index < 0 || index >= mButtonList.Count) return;
            
            var buttonConfig = mButtonList[index];
            
            // 检查是否重复点击
            if (SelectedButtonIndex.Value == index) return;
            
            // 发送引导事件
            SendGuideEvent(buttonConfig.Type);
            
            // 检查模块功能
            if (!CheckModuleFunction(buttonConfig.Type)) return;
            
            // 更新选中状态
            SelectedButtonIndex.Value = index;
            CurrentPanelType.Value = buttonConfig.Type;
            
            // 触发面板切换
            OnPanelChangeRequested?.Invoke(buttonConfig.Type);
            
            LogTool.Log($"[GameMainPanelViewModel] 选择按钮: {buttonConfig.Type}");
        }

        private bool CanSelectButton(int index)
        {
            return CanClick.Value && !IsAnimating.Value;
        }

        private void OnChangePanel(GameMainButtonType panelType)
        {
            OnPanelChangeRequested?.Invoke(panelType);
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        #endregion

        #region 数据加载

        private void LoadResourceData()
        {
            // 金币
            var goldId = Global.ItemTypeIds[ItemType.Gold];
            Gold.Value = Global.GetItemNumById(goldId);
            
            // 钻石
            var diamondId = Global.ItemTypeIds[ItemType.Diamond];
            Diamond.Value = Global.GetItemNumById(diamondId);
            
            // 体力
            Stamina.Value = Global.PlayerResource.Stamina;
            MaxStamina.Value = Global.PlayerResource.GetMaxStamina();
            
            // 点券
            var couponId = Global.ItemTypeIds[ItemType.Coupon];
            PointCoupon.Value = Global.GetItemNumById(couponId);
        }

        #endregion

        #region 公共方法

        public GameMainButtonStruct GetButtonConfig(int index)
        {
            if (index >= 0 && index < mButtonList.Count)
            {
                return mButtonList[index];
            }
            return null;
        }

        public int GetButtonIndexByType(GameMainButtonType type)
        {
            for (int i = 0; i < mButtonList.Count; i++)
            {
                if (mButtonList[i].Type == type)
                {
                    return i;
                }
            }
            return -1;
        }

        public Color32 GetQualityColor(QualityType quality)
        {
            return GlobalFunc.GetQualityStyle(quality).MainColor;
        }

        public void SetClickCooldown(bool canClick)
        {
            CanClick.Value = canClick;
        }

        public void SetAnimating(bool isAnimating)
        {
            IsAnimating.Value = isAnimating;
        }

        public void UpdateResource(ItemType type, int value)
        {
            switch (type)
            {
                case ItemType.Gold:
                    Gold.Value = value;
                    break;
                case ItemType.Diamond:
                    Diamond.Value = value;
                    break;
                case ItemType.Stamina:
                    Stamina.Value = value;
                    break;
                case ItemType.Coupon:
                    PointCoupon.Value = value;
                    break;
            }
        }

        #endregion

        #region 属性
        // 资源属性（保留在 GameMainPanel，因为底部按钮可能用到）
        public BindableProperty<long> Gold { get; } = new BindableProperty<long>(0);
        public BindableProperty<long> Diamond { get; } = new BindableProperty<long>(0);
        public BindableProperty<long> Stamina { get; } = new BindableProperty<long>(0);
        public BindableProperty<long> MaxStamina { get; } = new BindableProperty<long>(100);
        public BindableProperty<long> PointCoupon { get; } = new BindableProperty<long>(0);
        #endregion

        #region 私有方法

        private System.Action<object> mStaminaChangeHandler;

        /// <summary>
        /// 订阅体力变化消息
        /// </summary>
        private void SubscribeStaminaChanges()
        {
            mStaminaChangeHandler = (stamina) =>
            {
                if (stamina is int staminaValue)
                {
                    Stamina.Value = staminaValue;
                    MaxStamina.Value = Global.PlayerResource.GetMaxStamina();
                    LogTool.Log($"[GameMainPanelViewModel] 体力更新: {staminaValue}");
                }
            };
            
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_STAMINA, mStaminaChangeHandler, Global.PlayerResource.Stamina);
        }

        /// <summary>
        /// 取消订阅体力变化消息
        /// </summary>
        private void UnsubscribeStaminaChanges()
        {
            if (mStaminaChangeHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_STAMINA, mStaminaChangeHandler);
                mStaminaChangeHandler = null;
            }
        }

        private void SendGuideEvent(GameMainButtonType buttonType)
        {
            switch (buttonType)
            {
                case GameMainButtonType.Shop:
                    UnlockGuideManager.Send(ModuleFunctionType.Shop, 1);
                    break;
                case GameMainButtonType.World:
                    // UnlockGuideManager.Send(ModuleFunctionType.SetCardGroup, 1);
                    UnlockGuideManager.Send(ModuleFunctionType.Talent, 8);
                    break;
                case GameMainButtonType.GeneralSkill:
                    UnlockGuideManager.Send(ModuleFunctionType.Talent, 1);
                    UnlockGuideManager.Send(ModuleFunctionType.FusionStoneGamePlay, 1);
                    break;
            }
        }

        private bool CheckModuleFunction(GameMainButtonType buttonType)
        {
            var data = DataConfigManager.Tables.TbGameConst.GameMainButtonList.Find(ele => ele.Type == buttonType);
            if (data != null)
            {
                return CheckModuleFunction(data.UnLockType);
            }
            return true;
        }

        private bool CheckModuleFunction(ModuleFunctionType type)
        {
            var tipStr = DataConfigManager.Tables.TbPlayerConst.ModuleFunctionUnlockTip;
            var mfCfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele => ele.ModuleType == type);
            if (mfCfg != null && mfCfg.UnlockUserLv > Global.User.Level)
            {
                Toast.Show(tipStr.Replace("[UserLv]", $"{mfCfg.UnlockUserLv}"), 1f);
                return false;
            }
            return true;
        }

        #endregion

        #region 事件

        public System.Action<GameMainButtonType> OnPanelChangeRequested;
        public System.Action OnCloseRequested;

        #endregion
    }
}
