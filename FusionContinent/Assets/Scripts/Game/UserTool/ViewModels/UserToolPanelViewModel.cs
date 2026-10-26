using System.Collections.Generic;
using UIFramework;
using UnityEngine;
using UIFramework.Services;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.GameMain;

namespace Game.UserTool
{
    /// <summary>
    /// 用户工具栏 ViewModel
    /// 管理顶部资源显示、用户信息（头像、名称、等级）
    /// 支持根据当前激活的全屏界面动态切换显示配置
    /// </summary>
    public class UserToolPanelViewModel : ViewModel
    {
        #region 属性
        // 当前配置
        public BindableProperty<UserToolData> CurrentConfig { get; } = new BindableProperty<UserToolData>(UserToolData.Default);
        
        // 当前界面名称
        public BindableProperty<string> CurrentPanelName { get; } = new BindableProperty<string>("");
        
        // 显示Add按钮的道具ID列表
        public BindableProperty<List<int>> ShowAddButtonItemIds { get; } = new BindableProperty<List<int>>(new List<int>()
        {
            // 1010001,
            // 1010002,
            // 1010003,
        });
        
        // 当前显示的道具ID列表（用于View订阅）
        public BindableProperty<List<int>> CurrentItemIds { get; } = new BindableProperty<List<int>>(new List<int>());
        
        // 用户信息
        public BindableProperty<string> UserName { get; } = new BindableProperty<string>("");
        public BindableProperty<int> AvatarId { get; } = new BindableProperty<int>(UserData.DefaultAvatarId);
        public BindableProperty<int> UserLevel { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> UserExp { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxExp { get; } = new BindableProperty<int>(100);
        public BindableProperty<float> ExpProgress { get; } = new BindableProperty<float>(0f);
        #endregion

        #region 命令
        public Command OnAvatarClickCommand { get; private set; }
        #endregion

        #region 依赖
        private GameMainService mGameMainService;
        #endregion

        #region 事件
        public System.Action OnAvatarClickRequested;
        public System.Action<UserToolData> OnConfigChanged;
        public System.Action<int> OnResourceChanged; // 道具数量变化通知
        #endregion
        
        #region 资源变化订阅
        private Dictionary<int, System.Action<object>> mItemChangeHandlers = new Dictionary<int, System.Action<object>>();
        private System.Action<object> mStaminaChangeHandler;
        private System.Action<object> mUserNameChangeHandler;
        private System.Action<object> mUserLevelInfoChangeHandler;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mGameMainService = GameServiceManager.Get<GameMainService>();
            
            // 初始化命令
            OnAvatarClickCommand = new Command(OnAvatarClick);
            
            // 加载数据
            LoadUserData();
            
            // 订阅配置变化
            SubscribeConfigChanges();
            SubscribeStaminaChanges();
            SubscribeUserNameChanges();
            SubscribeUserLevelInfoChanges();
            InitializeCurrentConfig();
            
            LogTool.Log("[UserToolPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeAllItemChanges();
            UnsubscribeStaminaChanges();
            UnsubscribeUserNameChanges();
            UnsubscribeUserLevelInfoChanges();
            LogTool.Log("[UserToolPanelViewModel] 销毁");
        }

        #region 命令实现

        private void OnAvatarClick()
        {
            OnAvatarClickRequested?.Invoke();
        }

        #endregion

        #region 数据加载

        private void LoadUserData()
        {
            if (Global.User != null)
            {
                UserName.Value = Global.User.UserName;
                AvatarId.Value = Global.User.Avatar > 0 ? Global.User.Avatar : UserData.DefaultAvatarId;
                UserLevel.Value = Global.User.Level;
                
                // 加载经验值
                var levelInfo = Global.User.GetLevelInfo();
                UserExp.Value = levelInfo.Exp;
                var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(UserLevel.Value);
                MaxExp.Value = lvCfg?.Exp ?? 100;
                ExpProgress.Value = MaxExp.Value > 0 ? (float)UserExp.Value / MaxExp.Value : 0f;
            }
        }

        public void RefreshUserData()
        {
            LoadUserData();
        }

        #endregion

        private void InitializeCurrentConfig()
        {
            var config = CurrentConfig.Value;
            if (config?.ItemIds != null)
            {
                CurrentItemIds.Value = new List<int>(config.ItemIds);
                SubscribeItemChanges(config.ItemIds);
            }
        }

        #region 配置变化订阅
        
        private void SubscribeConfigChanges()
        {
            CurrentConfig.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal?.ItemIds != null)
                {
                    // 更新当前道具ID列表
                    CurrentItemIds.Value = new List<int>(newVal.ItemIds);
                    
                    // 重新订阅道具变化
                    SubscribeItemChanges(newVal.ItemIds);
                }
                
                OnConfigChanged?.Invoke(newVal);
            };
        }
        
        #endregion

        #region 资源变化订阅

        private void SubscribeItemChanges(List<int> itemIds)
        {
            // 先取消之前的订阅
            UnsubscribeAllItemChanges();
            
            if (itemIds == null) return;
            
            // 订阅每个道具的变化（ItemNum 为 long，不能用 data is int 判断）
            foreach (var itemId in itemIds)
            {
                var capturedItemId = itemId;
                var handler = new System.Action<object>((data) =>
                {
                    LogTool.Log($"[UserToolPanelViewModel] 道具 {capturedItemId} 数量变化: {data}");
                    OnResourceChanged?.Invoke(capturedItemId);
                });
                
                mItemChangeHandlers[itemId] = handler;
                Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}", handler);
                LogTool.Log($"[UserToolPanelViewModel] 订阅道具变化: {itemId}");
            }
        }

        private void SubscribeStaminaChanges()
        {
            mStaminaChangeHandler = OnStaminaChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_STAMINA, mStaminaChangeHandler);
            LogTool.Log("[UserToolPanelViewModel] 订阅体力变化");
        }

        private void OnStaminaChanged(object stamina)
        {
            var staminaItemId = Global.ItemTypeIds[ItemType.Stamina];
            LogTool.Log($"[UserToolPanelViewModel] 体力变化: {(int)stamina}");
            if (OnResourceChanged != null)
            {
                OnResourceChanged.Invoke(staminaItemId);
            }
        }

        private void SubscribeUserNameChanges()
        {
            mUserNameChangeHandler = OnUserNameChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_NAME, mUserNameChangeHandler);
        }

        private void OnUserNameChanged(object userName)
        {
            LoadUserData();
        }

        private void SubscribeUserLevelInfoChanges()
        {
            mUserLevelInfoChangeHandler = OnUserLevelInfoChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, mUserLevelInfoChangeHandler);
        }

        private void OnUserLevelInfoChanged(object info)
        {
            LoadUserData();
        }

        private void UnsubscribeUserLevelInfoChanges()
        {
            if (mUserLevelInfoChangeHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, mUserLevelInfoChangeHandler);
                mUserLevelInfoChangeHandler = null;
            }
        }

        private void UnsubscribeUserNameChanges()
        {
            if (mUserNameChangeHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_NAME, mUserNameChangeHandler);
                mUserNameChangeHandler = null;
            }
        }

        private void UnsubscribeStaminaChanges()
        {
            if (mStaminaChangeHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_STAMINA, mStaminaChangeHandler);
                mStaminaChangeHandler = null;
            }
        }

        private void UnsubscribeAllItemChanges()
        {
            foreach (var kvp in mItemChangeHandlers)
            {
                Global.Publisher.Unsubscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{kvp.Key}", kvp.Value);
            }
            mItemChangeHandlers.Clear();
        }

        #endregion
    }
}
