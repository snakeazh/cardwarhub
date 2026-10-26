using FusionContinent.UI;
using FusionContinent.Model;
using Game.UserTool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIFramework;
using System.Collections.Generic;
using System.Linq;
using Game.GameMain;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 用户工具栏 - 新框架版本 (MVVM)
    /// 复用原 UserToolPanel 的 UI 组件
    /// 管理顶部资源显示、用户信息（头像、名称、等级、经验条）
    /// </summary>
    [UIFramework.PrefabPath("UserToolPanel")]
    public class UserToolPanelNew : UIFramework.UIPanel<UserToolPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private UserToolPanel mOldPanel;
        
        // 动态创建的资源Item列表
        private List<ResourceItem> mDynamicResourceItems = new List<ResourceItem>();
        
        // 资源Item父节点（从mOldPanel.ResourceItem获取）
        private Transform mResourceItemParent;

        private System.Action<UserToolData, UserToolData> mCurrentConfigChangedHandler;
        private System.Action<List<int>, List<int>> mShowAddButtonItemIdsChangedHandler;
        private System.Action<string, string> mUserNameChangedHandler;
        private System.Action<int, int> mAvatarIdChangedHandler;
        private System.Action<int, int> mUserLevelChangedHandler;
        private System.Action<float, float> mExpProgressChangedHandler;
        private UnityEngine.Events.UnityAction mAvatarButtonClickHandler;
        private System.Action<object> mAvatarPubHandler;
        private System.Action<object> mAvatarFramePubHandler;
        protected override bool ShouldUpdateUserToolOnBecameVisible()
        {
            return false;
        }
        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<UserToolPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[UserToolPanelNew] 找不到 UserToolPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[UserToolPanelNew] 已关联 UserToolPanel");
        }

        protected override void OnBindViewModel(UserToolPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);

            // 绑定命令
            BindCommands(vm);
            
            // 绑定配置变化
            BindConfigChanges(vm);

            // 初始化 UI 状态
            InitializeUI(vm);
            
            // 根据当前配置更新显示
            UpdateVisibility(vm.CurrentConfig.Value);
        }
        
        private void BindConfigChanges(UserToolPanelViewModel vm)
        {
            // 订阅配置变化
            mCurrentConfigChangedHandler = (oldVal, newVal) =>
            {
                UpdateVisibility(newVal);
            };
            vm.CurrentConfig.OnValueChanged += mCurrentConfigChangedHandler;
            
            // 订阅Add按钮显示列表变化
            mShowAddButtonItemIdsChangedHandler = (oldVal, newVal) =>
            {
                // 刷新所有Item的Add按钮状态
                foreach (var item in mDynamicResourceItems)
                {
                    bool showAdd = newVal?.Contains(item.GetItemId()) ?? false;
                    item.ShowAddButton(showAdd);
                }
            };
            vm.ShowAddButtonItemIds.OnValueChanged += mShowAddButtonItemIdsChangedHandler;
            
            // 订阅资源变化事件
            vm.OnResourceChanged += OnResourceChanged;
            vm.OnConfigChanged += OnConfigChanged;
        }
        
        // 货币滚动动画时长（秒），比 RollingNumberManager 默认值更短，保证领奖后及时刷新
        private const float ResourceRollDuration = 0.25f;

        /// <summary>
        /// 资源数量变化回调
        /// </summary>
        private void OnResourceChanged(int itemId)
        {
            // 找到对应的 ResourceItem 并更新显示
            var resourceItem = mDynamicResourceItems.Find(x => x.GetItemId() == itemId);
            if (resourceItem != null)
            {
                var newNum = GameServiceManager.GetOrCreate<GameMainService>().GetResourceCount(itemId);
                resourceItem.PlayNumAnimation(newNum, ResourceRollDuration);
                LogTool.Log($"[UserToolPanelNew] 更新资源显示: ItemId={itemId}, Num={newNum}");
            }
        }

        /// <summary>
        /// 从数据源重新读取所有货币值并刷新显示（用于 OnShow 等需要兜底刷新的场景，
        /// 避免隐藏期间漏掉事件导致显示旧值）
        /// </summary>
        private void RefreshResourceItems()
        {
            var gameMainService = GameServiceManager.GetOrCreate<GameMainService>();
            foreach (var item in mDynamicResourceItems)
            {
                if (item == null) continue;
                var num = gameMainService.GetResourceCount(item.GetItemId());
                item.SetNum(num);
            }
        }
        
        /// <summary>
        /// 根据配置更新显示
        /// </summary>
        private void UpdateVisibility(UserToolData config)
        {
            if (config == null) return;
            
            // 头像区域
            if (mOldPanel.AvatarBtn != null)
                mOldPanel.AvatarBtn.gameObject.SetActive(config.ShowAvatar);
            if (mOldPanel.UserName != null)
                mOldPanel.UserName.gameObject.SetActive(config.ShowAvatar);
            if (mOldPanel.UserLevel != null)
                mOldPanel.UserLevel.gameObject.SetActive(config.ShowAvatar);
            mOldPanel.MoneyList.childAlignment = config.ShowAvatar ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            // 更新资源Item显示
            UpdateResourceItems(config.ItemIds, VM.ShowAddButtonItemIds.Value);
            
            LogTool.Log($"[UserToolPanelNew] 更新显示: ShowAvatar={config.ShowAvatar}, ItemCount={config.ItemIds?.Count ?? 0}");
        }
        
        /// <summary>
        /// 更新资源Item列表
        /// </summary>
        private void UpdateResourceItems(List<int> itemIds, List<int> showAddButtonItemIds)
        {
            if (itemIds == null) itemIds = new List<int>();
            if (showAddButtonItemIds == null) showAddButtonItemIds = new List<int>();
            
            // 1. 移除不需要的Item
            for (int i = mDynamicResourceItems.Count - 1; i >= 0; i--)
            {
                var item = mDynamicResourceItems[i];
                if (!itemIds.Contains(item.GetItemId()))
                {
                    mDynamicResourceItems.RemoveAt(i);
                    Destroy(item.gameObject);
                }
            }
            
            // 2. 添加新的Item 或更新现有Item的Add按钮状态
            foreach (var itemId in itemIds)
            {
                var existingItem = mDynamicResourceItems.Find(x => x.GetItemId() == itemId);
                if (existingItem == null)
                {
                    // 创建新Item，根据配置决定是否显示Add按钮
                    bool showAdd = showAddButtonItemIds.Contains(itemId);
                    CreateResourceItem(itemId, showAdd);
                }
                else
                {
                    // 更新现有Item的Add按钮状态
                    bool showAdd = showAddButtonItemIds.Contains(itemId);
                    existingItem.ShowAddButton(showAdd);
                }
            }
        }
        
        /// <summary>
        /// 创建资源Item
        /// </summary>
        private void CreateResourceItem(int itemId, bool showAddButton)
        {
            if (mOldPanel.ResourceItem == null)
            {
                LogTool.LogError("[UserToolPanelNew] ResourceItem模板未设置");
                return;
            }
            
            // 获取父节点
            if (mResourceItemParent == null)
            {
                mResourceItemParent = mOldPanel.ResourceItem.transform.parent;
            }
            
            // 克隆ResourceItem
            var go = Instantiate(mOldPanel.ResourceItem.gameObject, mResourceItemParent);
            var resourceItem = go.GetComponent<ResourceItem>();
            
            if (resourceItem != null)
            {
                // 初始化
                var itemConfig = DataConfigManager.Tables.TbItemConfig.Get(itemId);
                var itemNum = GameServiceManager.GetOrCreate<GameMainService>().GetResourceCount(itemId);
                resourceItem.Initialize(itemId, Global.ItemIconSpriteAtlas.GetSpriteExt(itemConfig.Icon), itemNum, GetResourceNumFormatter(itemConfig.Type));
                
                // 根据配置显示/隐藏Add按钮
                resourceItem.ShowAddButton(showAddButton);
                
                // 设置回调
                resourceItem.SetClickCallback(OnResourceItemClick);
                resourceItem.SetAddClickCallback(OnResourceItemAddClick);
                
                mDynamicResourceItems.Add(resourceItem);
                
                LogTool.Log($"[UserToolPanelNew] 创建资源Item: {itemId}, ShowAdd={showAddButton}");
            }
            go.SetActive(true);
        }

        private System.Func<long, string> GetResourceNumFormatter(Config.ItemType itemType)
        {
            if (itemType == Config.ItemType.Stamina)
            {
                return FormatStaminaNum;
            }

            return null;
        }

        private string FormatStaminaNum(long num)
        {
            return $"{GlobalFunc.FormatNumber(num)}/{DataConfigManager.Tables.TbPlayerConst.MaxStamina}";
        }
        
        /// <summary>
        /// 资源Item点击
        /// </summary>
        private void OnResourceItemClick(int itemId)
        {
            LogTool.Log($"[UserToolPanelNew] 资源Item点击: {itemId}");
            // TODO: 打开资源详情界面
        }
        
        /// <summary>
        /// 资源Item Add按钮点击
        /// </summary>
        private void OnResourceItemAddClick(int itemId)
        {
            LogTool.Log($"[UserToolPanelNew] 资源Item Add点击: {itemId}");
            // TODO: 打开资源获取界面
        }

        protected override void OnUnbindViewModel(UserToolPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnAvatarClickRequested -= OnAvatarClick;
            vm.OnConfigChanged -= OnConfigChanged;
            vm.OnResourceChanged -= OnResourceChanged;

            if (mCurrentConfigChangedHandler != null)
            {
                vm.CurrentConfig.OnValueChanged -= mCurrentConfigChangedHandler;
                mCurrentConfigChangedHandler = null;
            }

            if (mShowAddButtonItemIdsChangedHandler != null)
            {
                vm.ShowAddButtonItemIds.OnValueChanged -= mShowAddButtonItemIdsChangedHandler;
                mShowAddButtonItemIdsChangedHandler = null;
            }

            if (mUserNameChangedHandler != null)
            {
                vm.UserName.OnValueChanged -= mUserNameChangedHandler;
                mUserNameChangedHandler = null;
            }

            if (mAvatarIdChangedHandler != null)
            {
                vm.AvatarId.OnValueChanged -= mAvatarIdChangedHandler;
                mAvatarIdChangedHandler = null;
            }

            if (mUserLevelChangedHandler != null)
            {
                vm.UserLevel.OnValueChanged -= mUserLevelChangedHandler;
                mUserLevelChangedHandler = null;
            }

            if (mExpProgressChangedHandler != null)
            {
                vm.ExpProgress.OnValueChanged -= mExpProgressChangedHandler;
                mExpProgressChangedHandler = null;
            }

            if (mAvatarPubHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_AVATAR, mAvatarPubHandler);
                mAvatarPubHandler = null;
            }
            if (mAvatarFramePubHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_AVATAR_FRAME, mAvatarFramePubHandler);
                mAvatarFramePubHandler = null;
            }

            // 清理按钮监听
            if (mOldPanel?.AvatarBtn != null && mAvatarButtonClickHandler != null)
            {
                mOldPanel.AvatarBtn.onClick.RemoveListener(mAvatarButtonClickHandler);
                mAvatarButtonClickHandler = null;
            }
            
            // 清理动态创建的资源Item
            foreach (var item in mDynamicResourceItems)
            {
                if (item != null)
                {
                    item.SetClickCallback(null);
                    item.SetAddClickCallback(null);
                }
            }
            mDynamicResourceItems.Clear();
        }

        #region 属性绑定

        private void BindProperties(UserToolPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 用户信息
            mUserNameChangedHandler = (oldVal, newVal) =>
            {
                if (mOldPanel.UserName != null)
                    mOldPanel.UserName.text = newVal;
            };
            vm.UserName.OnValueChanged += mUserNameChangedHandler;

            mAvatarIdChangedHandler = (oldVal, newVal) =>
            {
                RefreshAvatar();
                // if (mOldPanel.Avatar != null)
                //     mOldPanel.Avatar.sprite = Global.AvatarIconSpriteAtlas.GetSprite("Head_" + newVal);
            };
            vm.AvatarId.OnValueChanged += mAvatarIdChangedHandler;

            mUserLevelChangedHandler = OnUserLevelChanged;
            vm.UserLevel.OnValueChanged += mUserLevelChangedHandler;

            mExpProgressChangedHandler = OnExpProgressChanged;
            vm.ExpProgress.OnValueChanged += mExpProgressChangedHandler;

            // 订阅 ViewModel 事件
            vm.OnAvatarClickRequested += OnAvatarClick;

            mAvatarPubHandler = OnUserAvatarChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_AVATAR, mAvatarPubHandler);
            mAvatarFramePubHandler = OnUserAvatarFrameChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_AVATAR_FRAME, mAvatarFramePubHandler);
        }

        #endregion

        #region 命令绑定

        private void BindCommands(UserToolPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 头像点击
            if (mOldPanel.AvatarBtn != null)
            {
                mAvatarButtonClickHandler = () => vm.OnAvatarClickCommand.Execute();
                mOldPanel.AvatarBtn.onClick.AddListener(mAvatarButtonClickHandler);
            }
        }

        #endregion

        #region 初始化 UI

        private void InitializeUI(UserToolPanelViewModel vm)
        {
            if (mOldPanel == null || vm == null) return;

            // 用户信息
            if (mOldPanel.UserName != null)
                mOldPanel.UserName.text = vm.UserName.Value;
            // if (mOldPanel.Avatar != null)
            //     mOldPanel.Avatar.sprite = Global.AvatarIconSpriteAtlas.GetSprite("Head_" + vm.AvatarId.Value);
            if (mOldPanel.UserLevelText != null)
                mOldPanel.UserLevelText.text = vm.UserLevel.Value.ToString();
            RefreshUserExpUI(vm);
            RefreshAvatar();
        }

        private void OnUserLevelChanged(int oldVal, int newVal)
        {
            if (mOldPanel.UserLevelText != null)
            {
                mOldPanel.UserLevelText.text = newVal.ToString();
            }

            RefreshUserExpUI(VM);
        }

        private void OnExpProgressChanged(float oldVal, float newVal)
        {
            RefreshUserExpUI(VM);
        }

        private void RefreshUserExpUI(UserToolPanelViewModel vm)
        {
            if (mOldPanel == null || vm == null)
            {
                return;
            }

            var maxLevelCfg = DataConfigManager.Tables.TbPlayerLevelConfig.DataList[
                DataConfigManager.Tables.TbPlayerLevelConfig.DataList.Count - 1];
            bool isMaxLevel = vm.UserLevel.Value >= maxLevelCfg.Lv;

            if (mOldPanel.LevelExpText != null)
            {
                mOldPanel.LevelExpText.text = isMaxLevel
                    ? "MAX"
                    : $"{vm.UserExp.Value}/{vm.MaxExp.Value}";
            }

            if (mOldPanel.LevelExpProgressBar != null)
            {
                mOldPanel.LevelExpProgressBar.fillAmount = isMaxLevel ? 1f : vm.ExpProgress.Value;
            }
        }

        void RefreshAvatar()
        {
            if (Global.User == null || Global.User.UserId == 0)
            {
                return;
            }
            this.mOldPanel.PlayerHead.SetData(Global.User);
        }

        #endregion

        #region 事件处理

        private void OnAvatarClick()
        {
            LogTool.Log("[UserToolPanelNew] 头像点击");
            // TODO: 打开个人信息界面
        }
        
        private void OnConfigChanged(UserToolData config)
        {
            LogTool.Log($"[UserToolPanelNew] 配置变化");
            UpdateVisibility(config);
        }

        private void OnUserAvatarChanged(object avatar)
        {
            VM?.RefreshUserData();
            RefreshAvatar();
        }

        private void OnUserAvatarFrameChanged(object avatarFrame)
        {
            VM?.RefreshUserData();
            RefreshAvatar();
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 面板打开时强制刷新一次用户数据（避免首次创建时 Global.User 还未就绪导致显示为空）
            VM?.RefreshUserData();
            InitializeUI(VM);
        }

        protected override void OnShow()
        {
            // 再次显示时也刷新一次，保证用户名/头像/等级及时更新
            VM?.RefreshUserData();
            InitializeUI(VM);
            // 兜底刷新货币显示，避免隐藏期间漏掉事件导致显示旧值
            RefreshResourceItems();
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }
        
        protected override void OnClose()
        {
            // 面板关闭时的逻辑
        }
        
        /// <summary>
        /// 全局配置变化回调
        /// </summary>
    }
}
