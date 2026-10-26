using DG.Tweening;
using Game.Shop;
using Game.UserTool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using UIFramework.Services;
using Config;
using FusionContinent.Helper;
using Assets.Scripts.Localization;
using System.Collections.Generic;
using System.Collections;

namespace FusionContinent
{
    /// <summary>
    /// 商店主界面 - 新框架版本 (MVVM)
    /// 使用 SubPanelManager 管理子界面，支持复用
    /// </summary>
    [PrefabPath("ShopPanel01")]
    public class ShopPanel01New : SubPanelManager<ShopPanel01ViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private ShopPanel01 mOldPanel;
        
        // 按钮映射（复用原Panel的）
        private Dictionary<ShopType, Button> mShopBtnMap = new Dictionary<ShopType, Button>();

        // 选中指示图片
        private RectTransform mSelectedImage;
        private bool mIsFirstSelection = true;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<ShopPanel01>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ShopPanel01New] 找不到 ShopPanel01 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            // 设置子界面容器
            mSubPanelContainer = mOldPanel.Body;

            // 获取 Selected 指示图片 (ChangeBg/Selected)
            if (mOldPanel.Change != null)
            {
                mSelectedImage = mOldPanel.Change.parent?.Find("Selected") as RectTransform;
            }

            LogTool.Log("[ShopPanel01New] 已关联 ShopPanel01");
        }

        protected override void OnBindViewModel(ShopPanel01ViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            vm.OnShopChanged += OnShopChanged;
            vm.OnUpMoneyListChanged += OnUpMoneyListChanged;
            vm.OnClosePanel += OnClosePanel;

            // 初始化商店按钮
            InitShopButtons(vm);
        }

        protected override void OnUnbindViewModel(ShopPanel01ViewModel vm)
        {
            // 取消事件订阅
            vm.OnShopChanged -= OnShopChanged;
            vm.OnUpMoneyListChanged -= OnUpMoneyListChanged;
            vm.OnClosePanel -= OnClosePanel;
            
            // 清理按钮监听
            foreach (var kvp in mShopBtnMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.onClick.RemoveAllListeners();
                }
            }
            mShopBtnMap.Clear();
        }

        #region 属性绑定

        private void BindProperties(ShopPanel01ViewModel vm)
        {
            if (mOldPanel == null) return;

            // Banner背景
            // vm.BannerBgPath.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (!string.IsNullOrEmpty(newVal) && mOldPanel.BannerBg != null)
            //     {
            //         SpriteHelper.TryLoadTexture(newVal, mOldPanel.BannerBg);
            //     }
            // };

            // 商店名称
            vm.CurrentShopName.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Banner != null)
                {
                    var titleText = mOldPanel.Banner.Find("TitleText")?.GetComponent<LocalizationText>();
                    if (titleText != null)
                    {
                        titleText.SetData(LocalizationModule.UITexts, newVal);
                    }
                }
            };

            // Banner可见性
            vm.IsBannerVisible.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.BannerBg != null)
                    mOldPanel.BannerBg.gameObject.SetActive(newVal);
                if (mOldPanel.TitleCanvas != null)
                    mOldPanel.TitleCanvas.gameObject.SetActive(newVal);
            };

            // Foot可见性
            vm.IsFootVisible.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Foot != null)
                    mOldPanel.Foot.gameObject.SetActive(newVal);
            };
        }

        #endregion

        #region 商店按钮初始化

        private void InitShopButtons(ShopPanel01ViewModel vm)
        {
            if (mOldPanel?.Change == null) return;

            mShopBtnMap.Clear();
            var shopConfigs = UIFramework.Services.GameServiceManager.Get<ShopService>().GetShopConfigs();
            
            if (shopConfigs == null) return;

            for (int i = 0; i < mOldPanel.Change.childCount; i++)
            {
                int idx = i;
                Transform changeBtn = mOldPanel.Change.GetChild(idx);
                
                if (idx >= shopConfigs.Count)
                {
                    changeBtn.gameObject.SetActive(false);
                    continue;
                }

                var shopCfg = shopConfigs[idx];
                
                // 单机化：抽卡（卡包）与点券页签已移除，对应页签直接隐藏
                if (shopCfg.Type == ShopType.Cardgroups || shopCfg.Type == ShopType.Gem)
                {
                    changeBtn.gameObject.SetActive(false);
                    continue;
                }

                // 设置按钮文本
                var text = changeBtn.Find("Text")?.GetComponent<LocalizationText>();
                if (text != null)
                {
                    text.SetData(LocalizationModule.UITexts, shopCfg.Name);
                }

                // 绑定点击事件
                var button = changeBtn.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        // 调用ViewModel的命令
                        vm.OnSelectShopCommand.Execute(idx);
                    });
                }

                mShopBtnMap[shopCfg.Type] = button;
                changeBtn.gameObject.SetActive(true);
            }
        }

        private void SelectFirstShop()
        {
            if (VM != null && mOldPanel?.Change != null && mOldPanel.Change.childCount > 0)
            {
                var firstBtn = mOldPanel.Change.GetChild(0).GetComponent<Button>();
                firstBtn?.onClick.Invoke();
            }
        }

        #endregion

        #region 商店切换处理

        private void OnShopChanged(ShopType shopType, int shopId)
        {
            // 更新按钮选中状态
            UpdateButtonSelection(shopType);

            // 切换子Panel（使用旧框架）
            ChangeShopBody(shopType, shopId);
        }

        private void UpdateButtonSelection(ShopType selectedType)
        {
            if (mOldPanel?.Change == null) return;

            // 仅首次需要强制刷新布局，后续按钮位置不会变
            if (mIsFirstSelection)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mOldPanel.Change);
                mIsFirstSelection = false;
            }

            for (int i = 0; i < mOldPanel.Change.childCount; i++)
            {
                var child = mOldPanel.Change.GetChild(i);
                var isSelected = false;

                // 根据索引判断是否选中
                if (VM != null)
                {
                    var shopCfg = VM.GetShopConfig(i);
                    if (shopCfg != null)
                    {
                        isSelected = (shopCfg.Type == selectedType);
                    }
                }

                // 移动 Selected 指示图片到选中按钮的坐标（带动画）
                if (isSelected && mSelectedImage != null)
                {
                    var btnRect = child as RectTransform;
                    if (btnRect != null)
                    {
                        // 将按钮在 ChangeBg 坐标系下的 anchoredPosition 转换给 Selected 使用
                        Vector2 targetAnchoredPos;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            mSelectedImage.parent as RectTransform,
                            btnRect.position,
                            null,
                            out targetAnchoredPos);

                        mSelectedImage.DOAnchorPos(targetAnchoredPos, 0.25f)
                            .SetEase(Ease.OutBack);
                    }
                }
            }
        }

        /// <summary>
        /// 切换商店内容（使用新框架的Panel复用机制）
        /// </summary>
        private void ChangeShopBody(ShopType shopType, int shopId)
        {
            // 只隐藏当前子界面，不销毁（保留缓存用于复用）
            if (mCurrentSubPanel != null)
            {
                if (mCurrentSubPanel.gameObject.activeSelf)
                    mCurrentSubPanel.Hide();
                mCurrentSubPanel = null;
            }

            // 根据商店类型打开对应的子Panel
            switch (shopType)
            {
                case ShopType.ItemMaterial:
                {
                    var normalShopVm = new Game.Shop.NormalShopList01ViewModel();
                    normalShopVm.OnCreate();
                    normalShopVm.Initialize(shopId, true);

                    OpenOrReuseSubPanel<NormalShopList01New, Game.Shop.NormalShopList01ViewModel>(
                        normalShopVm,
                        (panel) =>
                        {
                            if (panel != null)
                            {
                                panel.RefreshShopItems(panel.VM);
                            }
                        },
                        (vm) =>
                        {
                            vm.Initialize(shopId, true);
                        }
                    );
                    break;
                }
                case ShopType.Soldier:
                {
                    var soldierVm = new Game.Shop.SoldierShopListViewModel();
                    soldierVm.OnCreate();
                    soldierVm.Initialize(shopId);

                    OpenOrReuseSubPanel<SoldierShopListNew, Game.Shop.SoldierShopListViewModel>(
                        soldierVm,
                        (panel) =>
                        {
                        },
                        (vm) =>
                        {
                            vm.Initialize(shopId);
                        }
                    );
                    break;
                }
                case ShopType.DailyShop:
                {
                    var dailyShopVm = new Game.Shop.DailyShopListViewModel();
                    dailyShopVm.OnCreate();
                    dailyShopVm.Initialize(shopId);

                    OpenOrReuseSubPanel<DailyShopListNew, Game.Shop.DailyShopListViewModel>(
                        dailyShopVm,
                        (panel) =>
                        {
                            if (panel != null)
                            {
                                panel.RefreshShopItems(panel.VM);
                            }
                        },
                        (vm) =>
                        {
                            vm.Initialize(shopId);
                        }
                    );
                    break;
                }
            }
        }

        #endregion

        #region 资源列表更新

        private void OnUpMoneyListChanged(Dictionary<string, float> upMoneyList)
        {
            // 调用 GameMainPanel 设置资源列表
            if (GameMainPanel.Default != null)
            {
                GameMainPanel.Default.SetCanvasUpMoneyList(upMoneyList);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>o
        /// 切换商店页面（供外部调用）
        /// </summary>
        public void ChangeShopPage(ShopType shopType, int param = 0)
        {
            VM?.OnChangeShopPageCommand.Execute(shopType);
        }

        private void OnClosePanel()
        {
            // 隐藏当前子界面（保留在内存中用于复用）
            HideCurrentSubPanel();
            
            CloseSelf();
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            // 初次打开时默认选中第一个商店
            if (VM != null)
            {
                VM.LastSelectedShopIndex = 0;
                VM.SelectedShopIndex.Value = -1;
            }
            
            if (mOldPanel?.Change != null && mOldPanel.Change.childCount > 0)
            {
                // 默认选中第一个可见页签（抽卡/点券页签已隐藏）
                for (int i = 0; i < mOldPanel.Change.childCount; i++)
                {
                    var child = mOldPanel.Change.GetChild(i);
                    if (!child.gameObject.activeSelf) continue;
                    var firstBtn = child.GetComponent<Button>();
                    if (firstBtn != null)
                    {
                        firstBtn.onClick.Invoke();
                        break;
                    }
                }
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (VM != null)
            {
                VM.UpdateUpMoneyList(VM.CurrentShopType.Value);

                var restoreIndex = VM.LastSelectedShopIndex;
                if (restoreIndex >= 0 && mOldPanel?.Change != null && restoreIndex < mOldPanel.Change.childCount)
                {
                    VM.OnSelectShopCommand.Execute(restoreIndex);
                }
            }
        }

        protected override void OnHide()
        {
            if (VM != null && VM.SelectedShopIndex.Value >= 0)
            {
                VM.LastSelectedShopIndex = VM.SelectedShopIndex.Value;
                VM.SelectedShopIndex.Value = -1;
            }
      
            base.OnHide();
        }

        #endregion
    }
}