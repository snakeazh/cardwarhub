using Game.GameMain;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.UI.Components;

namespace FusionContinent
{
    /// <summary>
    /// 游戏主界面 - 新框架版本 (MVVM)
    /// 复用原 GameMainPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("GameMainPanel")]
    public class GameMainPanelNew : UIFramework.UIPanel<GameMainPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private GameMainPanel mOldPanel;
        public static GameMainPanelNew Default;

        // 按钮映射
        private Dictionary<int, UIPointerClickAndLongPress> mButtonMap = new Dictionary<int, UIPointerClickAndLongPress>();
        public Dictionary<GameMainButtonType, UIPointerClickAndLongPress> mButtonTypeMap = new Dictionary<GameMainButtonType, UIPointerClickAndLongPress>();
        
        // 动画变量已移除

        // 配置
        private float mClickCooldown = 0.3f;
        
        // 当前显示的子界面
        private UIFramework.UIPanel mCurrentSubPanel;

        // 当前激活的按钮索引（-1 表示无激活按钮，即停留在主界面）
        private int mActiveButtonIndex = -1;

        private System.Action<object> mFootBtnEnabledHandler;
        private System.Action<object> mUserLevelChangedHandler;
        
        // 子界面类型映射
        private Dictionary<string, System.Type> mSubPanelTypes = new Dictionary<string, System.Type>
        {
            { "Battle", typeof(MainInterfacePanelNew) },
            { "Player", null }, // 暂未迁移
            { "Shop", typeof(ShopPanel01New) },
            { "Card", typeof(SoldierMainPanelNew) },
            { "Challenge", typeof(BigWorldPanelNew) },
            { "GeneralSkill", typeof(GeneralSkillPanelNew) }
        };
        
        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<GameMainPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GameMainPanelNew] 找不到 GameMainPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            Default = this;
            LogTool.Log("[GameMainPanelNew] 已关联 GameMainPanel");
        }

        protected override void OnBindViewModel(GameMainPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            vm.OnPanelChangeRequested += OnPanelChangeRequested;
            vm.OnCloseRequested += OnCloseRequested;

            // 订阅全局底部按钮显隐事件（活动面板返回时需要恢复）
            mFootBtnEnabledHandler = OnFootBtnEnabledChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, mFootBtnEnabledHandler);

            mUserLevelChangedHandler = OnUserLevelChanged;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, mUserLevelChangedHandler);

            // 初始化底部按钮
            InitBottomButtons(vm);
            
            // 初始化顶部资源（UserToolPanel 相关逻辑已移到 UserToolPanelNew）
            // InitResourceButtons(vm); // 注释掉，资源显示由 UserToolPanel 处理
            
            // 初始化用户信息（已移到 UserToolPanelNew）
            // InitUserInfo(vm); // 注释掉，用户信息由 UserToolPanel 处理
        }

        protected override void OnUnbindViewModel(GameMainPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnPanelChangeRequested -= OnPanelChangeRequested;
            vm.OnCloseRequested -= OnCloseRequested;

            if (mFootBtnEnabledHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, mFootBtnEnabledHandler);
                mFootBtnEnabledHandler = null;
            }

            if (mUserLevelChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, mUserLevelChangedHandler);
                mUserLevelChangedHandler = null;
            }
            
            // 清理按钮监听
            foreach (var btn in mButtonMap.Values)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }
            mButtonMap.Clear();
            mButtonTypeMap.Clear();
        }

        #region 属性绑定

        private void BindProperties(GameMainPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 用户信息（已移到 UserToolPanelNew，这里不再绑定）
            // vm.UserName.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.UserName != null)
            //         mOldPanel.UserName.text = newVal;
            // };

            // vm.AvatarId.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.Avatar != null)
            //         mOldPanel.Avatar.sprite = Global.AvatarIconSpriteAtlas.GetSprite("Head_" + newVal);
            // };

            // vm.UserLevel.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.UserLevelText != null)
            //         mOldPanel.UserLevelText.text = newVal.ToString();
            // };

            // 资源（已移到 UserToolPanelNew，这里不再绑定）
            // vm.Gold.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.GoldText != null)
            //         mOldPanel.GoldText.GetComponent<RollingNumberManager>()?.Play(newVal);
            // };

            // vm.Diamond.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.DiamondText != null)
            //         mOldPanel.DiamondText.GetComponent<RollingNumberManager>()?.Play(newVal);
            // };

            // vm.Stamina.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.PhysicalStrengthText != null)
            //         mOldPanel.PhysicalStrengthText.GetComponent<RollingNumberManager>()?.Play(newVal);
            // };

            // vm.MaxStamina.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.PhysicalMaxLimitText != null)
            //         mOldPanel.PhysicalMaxLimitText.text = $"/{newVal}";
            // };

            // vm.PointCoupon.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.PointCouponText != null)
            //         mOldPanel.PointCouponText.GetComponent<RollingNumberManager>()?.Play(newVal);
            // };

            // 底部按钮可见性
            vm.IsBottomVisible.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Bottom != null)
                    mOldPanel.Bottom.gameObject.SetActive(newVal);
            };
        }

        /// <summary>
        /// 刷新所有UI状态（用于初始化）
        /// </summary>
        private void RefreshAllUIState()
        {
            if (VM == null || mOldPanel == null) return;

            // 用户信息（已移到 UserToolPanelNew）
            // if (mOldPanel.UserName != null)
            //     mOldPanel.UserName.text = VM.UserName.Value;
            // if (mOldPanel.Avatar != null)
            //     mOldPanel.Avatar.sprite = Global.AvatarIconSpriteAtlas.GetSprite("Head_" + VM.AvatarId.Value);
            // if (mOldPanel.UserLevelText != null)
            //     mOldPanel.UserLevelText.text = VM.UserLevel.Value.ToString();

            // 资源（已移到 UserToolPanelNew）
            // if (mOldPanel.GoldText != null)
            //     mOldPanel.GoldText.GetComponent<RollingNumberManager>()?.Play(VM.Gold.Value);
            // if (mOldPanel.DiamondText != null)
            //     mOldPanel.DiamondText.GetComponent<RollingNumberManager>()?.Play(VM.Diamond.Value);
            // if (mOldPanel.PhysicalStrengthText != null)
            //     mOldPanel.PhysicalStrengthText.GetComponent<RollingNumberManager>()?.Play(VM.Stamina.Value);
            // if (mOldPanel.PhysicalMaxLimitText != null)
            //     mOldPanel.PhysicalMaxLimitText.text = $"/{VM.MaxStamina.Value}";
            // if (mOldPanel.PointCouponText != null)
            //     mOldPanel.PointCouponText.GetComponent<RollingNumberManager>()?.Play(VM.PointCoupon.Value);

            // 底部按钮可见性
            if (mOldPanel.Bottom != null)
                mOldPanel.Bottom.gameObject.SetActive(VM.IsBottomVisible.Value);
        }

        #endregion

        #region 初始化

        private void InitBottomButtons(GameMainPanelViewModel vm)
        {
            if (mOldPanel?.Bottom == null) return;

            var buttonList = DataConfigManager.Tables.TbGameConst.GameMainButtonList;
            
            for (int i = 0; i < mOldPanel.Bottom.childCount; i++)
            {
                int index = i;
                if (index >= buttonList.Count) break;

                var buttonConfig = buttonList[index];
                Transform changeBtn = mOldPanel.Bottom.GetChild(index);
                UIPointerClickAndLongPress btn = changeBtn.GetComponent<UIPointerClickAndLongPress>();
                
                if (btn == null) continue;

                // 设置按钮文字
                // var textTrans = changeBtn.Find("Layout/Canvas/Text");
                // if (textTrans != null)
                // {
                //     var textComponent = textTrans.GetComponent<TextMeshProUGUI>();
                //     if (textComponent != null)
                //     {
                //         textComponent.text = buttonConfig.Name;
                //     }
                // }

                // 清除旧监听
                btn.onClick.RemoveAllListeners();
                
                // 添加新监听
                btn.onClick.AddListener(() =>
                {
                    OnBottomButtonClick(index, buttonConfig.Type, vm);
                });

                mButtonMap[index] = btn;
                mButtonTypeMap[buttonConfig.Type] = btn;

                // 更新按钮锁定图标
                UpdateButtonLockState(changeBtn, buttonConfig.Type);

                // 默认选中
                // if (buttonConfig.Type == )
                {
                    // 记录默认按钮，在 OnOpen 中触发
                }
                
                
            }
        }

        void RefreshGuideRef()
        {
            // 注意：UnlockGuideManager.Instance 仅在其 Awake() 中赋值，而该组件当前未挂载到任何场景/预制体，
            // 因此 Instance 为 null。引导功能开发接入时，需先把 UnlockGuideManager 挂到常驻 GameObject 上，
            // 届时可移除此判空。在此之前若去掉判空，主界面打开会抛 NullReferenceException（见本方法历史）。
            if (UnlockGuideManager.Instance == null) return;

            foreach (var item in mButtonTypeMap)
            {
                UnlockGuideManager.Instance.SetNavigation(item.Key,item.Value.GetComponent<RectTransform>());

            }

            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Equip,1,GameMainButtonType.Player);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipPhase,1,GameMainButtonType.Player);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.GeneralSkill,1,GameMainButtonType.GeneralSkill);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose,1,GameMainButtonType.GeneralSkill);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardResonance,1,GameMainButtonType.GeneralSkill);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EndlessTower,1,GameMainButtonType.World);

            
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SoldierAtlas,1,GameMainButtonType.Hero);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SoldierSkillAtlas,1,GameMainButtonType.Hero);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot,1,GameMainButtonType.Player);

        }
        
        private void UpdateButtonLockState(Transform btnTrans, GameMainButtonType buttonType)
        {
            var requireType = DataConfigManager.Tables.TbGameConst.GameMainButtonList.Find(ele=>ele.Type == buttonType);
            if (requireType == null) return;

            var cfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList
                .Find(ele => ele.ModuleType == requireType.UnLockType);
            if (cfg == null) return;

            bool isUnlocked = ModuleFunction.CheckOpen(cfg.ModuleType, false);
            btnTrans.Find("Layout/Locked")?.gameObject.SetActive(!isUnlocked);
            btnTrans.Find("Layout/Img")?.gameObject.SetActive(isUnlocked);
        }

        private void RefreshAllButtonLockStates()
        {
            if (mOldPanel?.Bottom == null) return;

            var buttonList = DataConfigManager.Tables.TbGameConst.GameMainButtonList;
            for (int i = 0; i < mOldPanel.Bottom.childCount; i++)
            {
                if (i >= buttonList.Count) break;
                Transform changeBtn = mOldPanel.Bottom.GetChild(i);
                UpdateButtonLockState(changeBtn, buttonList[i].Type);
            }
        }

        private void InitResourceButtons(GameMainPanelViewModel vm)
        {
            if (mOldPanel == null) return;

        }

        /// <summary>
        /// 初始化用户信息（已移到 UserToolPanelNew，保留空方法供兼容）
        /// </summary>
        private void InitUserInfo(GameMainPanelViewModel vm)
        {
            // 用户信息相关逻辑已迁移到 UserToolPanelNew
            // 此方法保留以保持代码兼容性
        }

        /// <summary>
        /// 头像点击处理（已移到 UserToolPanelNew）
        /// </summary>
        private void OnAvatarClickRequested()
        {
            // 头像点击逻辑已迁移到 UserToolPanelNew
            LogTool.Log("[GameMainPanelNew] 头像点击 - 请使用 UserToolPanelNew 处理");
        }

        #endregion

        #region 全局事件

        private void OnFootBtnEnabledChanged(object enabledBtn)
        {
            if (this == null || mOldPanel == null || VM == null)
            {
                return;
            }

            bool show = (bool)enabledBtn;
            VM.IsBottomVisible.Value = show;
            if (mOldPanel.Bottom != null)
            {
                mOldPanel.Bottom.gameObject.SetActive(show);
            }
        }

        private void OnUserLevelChanged(object message)
        {
            RefreshAllButtonLockStates();
        }

        #endregion

        #region 按钮点击处理

        private void OnBottomButtonClick(int index, GameMainButtonType buttonType, GameMainPanelViewModel vm)
        {
            // 检查是否可以点击
            if (!vm.CanClick.Value || vm.IsAnimating.Value) return;

            UnlockGuideManager.SendNavigation(1);
            // 如果点击的是当前激活按钮 → 关闭子界面，回到主界面
            if (index == mActiveButtonIndex && mCurrentSubPanel != null)
            {
                ReturnToMainInterface();
                return;
            }

            // 等级检查（在激活按钮之前，避免等级不足时按钮状态异常）
            if (!CheckButtonModuleOpen(buttonType)) return;

            // 激活按钮（显示退出样式）
            ActivateButton(index);

            // 绕过 ViewModel 命令直接切换面板，避免 ViewModel.OnSelectButton 重复触发
            ChangePanel(buttonType);

            // 更新 ViewModel 中的选中状态（引导等功能依赖此状态）
            vm.SelectedButtonIndex.Value = index;
        }
        
        

        private bool CheckButtonModuleOpen(GameMainButtonType buttonType)
        {
            var requireType = DataConfigManager.Tables.TbGameConst.GameMainButtonList.Find(ele=>ele.Type == buttonType); 
            if (requireType == null) return true;
            return ModuleFunction.CheckOpen(requireType.UnLockType);
        }

        /// <summary>
        /// 激活按钮：显示退出样式，隐藏默认文字
        /// </summary>
        private void ActivateButton(int buttonIndex)
        {
            if (mOldPanel?.Bottom == null) return;

            // 恢复所有按钮为默认样式
            for (int j = 0; j < mOldPanel.Bottom.childCount; j++)
            {
                var child = mOldPanel.Bottom.GetChild(j);
                child.Find("Bg")?.gameObject.SetActive(false);
                child.Find("Layout")?.gameObject.SetActive(true);
            }

            // 当前按钮激活：显示退出样式
            var activeBtn = mOldPanel.Bottom.GetChild(buttonIndex);
            var bgObj = activeBtn.Find("Bg")?.gameObject;
            if (bgObj != null)
            {
                // 重置缩放，清除残留 Tween
                bgObj.transform.localScale = Vector3.zero;
                bgObj.SetActive(true);
                PlayJellyAnimation(bgObj);
            }
            activeBtn.Find("Layout")?.gameObject.SetActive(false);

            mActiveButtonIndex = buttonIndex;
        }

        /// <summary>
        /// 恢复按钮为默认样式（显示默认文字）
        /// </summary>
        private void ResetAllButtons()
        {
            if (mOldPanel?.Bottom == null) return;

            for (int j = 0; j < mOldPanel.Bottom.childCount; j++)
            {
                var child = mOldPanel.Bottom.GetChild(j);
                child.Find("Bg")?.gameObject.SetActive(false);
                child.Find("Layout")?.gameObject.SetActive(true);
            }

            mActiveButtonIndex = -1;
        }

        #endregion

        #region 事件处理

        private void OnPanelChangeRequested(GameMainButtonType panelType)
        {
            ChangePanel(panelType);
        }

        private void OnCloseRequested()
        {
            Close();
        }

        #endregion

        #region 面板切换

        /// <summary>
        /// 隐藏当前子界面
        /// </summary>
        private void HideCurrentSubPanel()
        {
            if (mCurrentSubPanel != null)
            {
                LogTool.Log($"[GameMainPanelNew] 隐藏当前子界面: {mCurrentSubPanel.GetType().Name}");
                mCurrentSubPanel.Hide();
                mCurrentSubPanel = null;
            }
            // 不再重置按钮样式，重置由 ReturnToMainInterface 统一处理
        }

        /// <summary>
        /// 返回主界面（关闭当前子界面并恢复按钮状态）
        /// </summary>
        private void ReturnToMainInterface()
        {
            HideCurrentSubPanel();
            ResetAllButtons();
            OpenMainInterfacePanel();
        }

        /// <summary>
        /// 切换面板 - 支持复用已存在的面板
        /// </summary>
        private void ChangePanel(GameMainButtonType type)
        {
            switch (type)
            {
                case GameMainButtonType.Player:
                    OpenSpeakWhellPanel();
                    break;
                case GameMainButtonType.Shop:
                    GotoShop();
                    break;
                case GameMainButtonType.Hero:
                    OpenSoldierPanel();
                    break;
                case GameMainButtonType.World:
                    if (!ModuleFunction.CheckOpen(ModuleFunctionType.WorldBig)) 
                        return;
                    OpenBigWorldPanel();
                    break;
                case GameMainButtonType.GeneralSkill:
                    OpenGeneralSkillPanel();
                    break;
            }
            // 不再调用 OpenMainInterfacePanel，主界面始终在底层保持显示
        }

        /// <summary>
        /// 打开士兵界面（支持复用）
        /// </summary>
        private void OpenSoldierPanel()
        {
            // 创建新的面板
            var vm = new Game.Soldier.SoldierMainPanelViewModel();
            vm.OnCreate();
            
            UIFramework.UIManager.OpenPanelAsync<SoldierMainPanelNew, Game.Soldier.SoldierMainPanelViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                    mCurrentSubPanel = panel;
                }).Forget();
        }

        
        /// <summary>
        /// 打开大世界界面（支持复用）
        /// </summary>
        private void OpenBigWorldPanel()
        {
            // 创建新的面板
            var vm = new Game.Challenge.BigWorldPanelViewModel();
            vm.OnCreate();
            
            UIFramework.UIManager.OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                    mCurrentSubPanel = panel;
                }).Forget();
        }

        /// <summary>
        /// 打开通用技能界面（支持复用）
        /// </summary>
        private void OpenGeneralSkillPanel()
        {
            // 创建新的面板
            var vm = new Game.Soldier.GeneralSkillPanelViewModel();
            vm.OnCreate();
            
            UIFramework.UIManager.OpenPanelAsync<GeneralSkillPanelNew, Game.Soldier.GeneralSkillPanelViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                    mCurrentSubPanel = panel;
                });
        }

        /// <summary>
        /// 打开轮语者界面（支持复用）
        /// </summary>
        private void OpenSpeakWhellPanel()
        {
            // 创建新的面板
            var vm = new Game.Player.SpeakWhellPanelViewModel();
            vm.OnCreate();

            UIFramework.UIManager.OpenPanelAsync<SpeakWhellPanelNew, Game.Player.SpeakWhellPanelViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                    mCurrentSubPanel = panel;
                }).Forget();
        }

        public void GotoShop(ShopType shopType = ShopType.ItemMaterial, int param = 0)
        {
            // 先隐藏当前子界面
            HideCurrentSubPanel();
            var vm = new Game.Shop.ShopPanel01ViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<ShopPanel01New, Game.Shop.ShopPanel01ViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                    mCurrentSubPanel = panel;
                    panel.ChangeShopPage(shopType, param);
                }).Forget();
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            // 刷新所有UI状态（初始化显示）
            RefreshAllUIState();

            // 默认打开主界面（Battle）
          
            DOVirtual.DelayedCall(0.1f, () =>
            {
                OpenMainInterfacePanel();
            });
            RefreshGuideRef();
            RefreshAllButtonLockStates();
        }

        protected override void OnShow()
        {
            // 刷新缩放
            RefreshScale();

            // 刷新按钮锁定图标（玩家可能已升级）
            RefreshAllButtonLockStates();
            RefreshGuideRef();
        }

        protected override void OnHide()
        {
            base.OnHide();
            // 隐藏当前子界面
            HideCurrentSubPanel();
        }

        /// <summary>
        /// 打开主界面（支持复用）
        /// </summary>
        private void OpenMainInterfacePanel()
        {
            // 创建新的面板
            var vm = new Game.MainInterface.MainInterfacePanelViewModel();
            vm.OnCreate();

            UIFramework.UIManager.OpenPanelAsync<MainInterfacePanelNew, Game.MainInterface.MainInterfacePanelViewModel>(
                vm,
                UIFramework.UILevel.Common,
                onComplete: panel =>
                {
                }).Forget();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 播放果冻弹性动画（Bounce Out 效果）
        /// </summary>
        private void PlayJellyAnimation(GameObject target)
        {
            if (target == null) return;

            target.transform.localScale = Vector3.zero;
            target.transform.DOScale(1f, 0.35f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void RefreshScale()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer) return;
            
            if (mOldPanel?.Bottom != null)
            {
                // var rate = 2460f / Screen.height;
                mOldPanel.Bottom.localScale = Vector3.one;
            }
        }

        #endregion
    }
}
