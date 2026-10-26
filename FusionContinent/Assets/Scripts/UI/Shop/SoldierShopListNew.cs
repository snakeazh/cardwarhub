using Game.Shop;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using ScrollView;
using System.Collections.Generic;
using FusionContinent.Utils;

namespace FusionContinent
{
    /// <summary>
    /// 士兵商店列表 - 新框架版本 (MVVM)
    /// 复用原 SoldierShopList 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("SoldierShopList")]
    public class SoldierShopListNew : UIFramework.UIPanel<SoldierShopListViewModel>
    {
        /// <summary>
        /// 作为商店子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;
        // 引用原 Panel，复用其 UI 组件
        private SoldierShopList mOldPanel;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<SoldierShopList>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SoldierShopListNew] 找不到 SoldierShopList 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[SoldierShopListNew] 已关联 SoldierShopList");
        }

        protected override void OnBindViewModel(SoldierShopListViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);

            // 设置滚动视图更新函数
            SetupScrollView(vm);

            // 初始化商店显示
            if (vm.ShopId.Value > 0)
            {
                RefreshShopList(vm);
            }
        }

        protected override void OnUnbindViewModel(SoldierShopListViewModel vm)
        {
            // 清理
            if (mOldPanel?.ShopListScroll != null)
            {
                mOldPanel.ShopListScroll.SetUpdateFunc(null);
            }
        }

        #region 属性绑定

        private void BindProperties(SoldierShopListViewModel vm)
        {
            if (mOldPanel == null) return;

            // 商品列表变化
            vm.GoodsList.OnValueChanged += (oldVal, newVal) =>
            {
                RefreshShopList(vm);
            };
        }

        #endregion

        #region 滚动视图设置

        private void SetupScrollView(SoldierShopListViewModel vm)
        {
            if (mOldPanel?.ShopListScroll == null) return;

            // 设置更新函数
            mOldPanel.ShopListScroll.SetUpdateFunc((dataIndex, scrollItem) =>
            {
                OnUpdateShopItem(dataIndex, scrollItem as SoldierShopItem, vm);
            });
        }

        #endregion

        #region 商店项更新

        /// <summary>
        /// 更新商店项
        /// </summary>
        private void OnUpdateShopItem(int dataIndex, SoldierShopItem referenceItem, SoldierShopListViewModel vm)
        {
            if (referenceItem == null || vm.GoodsList.Value == null) return;
            if (dataIndex >= vm.GoodsList.Value.Count) return;

            var goods = vm.GoodsList.Value[dataIndex];
            var itemConfig = vm.GetItemConfig(goods.BuyItemId);
            int cardId = itemConfig?.EffectValue ?? 0;
            bool isLimitReached = vm.IsCardUnlocked(cardId);

            // 获取玩家商品数据
            var goodsInfo = vm.GetPlayerGoods(goods.Id);

            // 更新消耗显示
            UpdateConsumeUI(referenceItem, goods, vm);

            // 更新商品信息
            referenceItem.shopNameText.text = goods.Name;
            // referenceItem.shopIcon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            TextureLoader.SetIconByMono(
                referenceItem.shopIcon,
                group: "UITextures",
                iconName: goods.Shopgoodsicon,
                setNativeSize: false,
                onCompleted: ok => { }
            );
            // 装备详情按钮
            SetupEquipDetailsButton(referenceItem, goods, vm, cardId);

            // 限购显示
            UpdateLimitUI(referenceItem, goods, vm, isLimitReached);

            // 购买按钮
            SetupBuyButton(referenceItem, goods, vm, isLimitReached);

            // 触发引导
            if (goods.BuyConsumes == null || goods.BuyConsumes.Count == 0)
            {
                GuideFlow.Instance.SetStep(GuideType.BuyHero, 2, referenceItem.clickBtn.transform);
            }
        }

        /// <summary>
        /// 更新消耗UI
        /// </summary>
        private void UpdateConsumeUI(SoldierShopItem referenceItem, ShopGoodsConfig goods, SoldierShopListViewModel vm)
        {
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0];
                var itemCfg = vm.GetItemConfig(consume.Id);
                if (itemCfg != null)
                {
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    referenceItem.consumeImg.transform.parent.gameObject.SetActive(true);
                    referenceItem.consumeImg.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
                    referenceItem.consumeText.color = playerItemCount < consume.Num ? Color.red : Color.white;
                    referenceItem.consumeText.text = consume.Num.ToString();
                }
            }
            else
            {
                referenceItem.consumeImg.transform.parent.gameObject.SetActive(false);
                referenceItem.consumeText.GetComponent<TMP_Text>().text = "免费";
                referenceItem.consumeText.color = Color.white;
            }
        }

        /// <summary>
        /// 设置装备详情按钮
        /// </summary>
        private void SetupEquipDetailsButton(SoldierShopItem referenceItem, ShopGoodsConfig goods, SoldierShopListViewModel vm, int cardId)
        {
            var shopiconbtn = referenceItem.clickBtn;
            shopiconbtn.onClick.RemoveAllListeners();
            shopiconbtn.onClick.AddListener(() =>
            {
                vm.OnShowEquipDetailsCommand.Execute(cardId);
            });
        }

        /// <summary>
        /// 更新限购UI
        /// </summary>
        private void UpdateLimitUI(SoldierShopItem referenceItem, ShopGoodsConfig goods, SoldierShopListViewModel vm, bool isLimitReached)
        {
            // 显示遮罩和按钮
            referenceItem.Mask.gameObject.SetActive(isLimitReached);
            referenceItem.clickBtn.gameObject.SetActive(!isLimitReached);
        }

        /// <summary>
        /// 设置购买按钮
        /// </summary>
        private void SetupBuyButton(SoldierShopItem referenceItem, ShopGoodsConfig goods, SoldierShopListViewModel vm, bool isLimitReached)
        {
            referenceItem.OnSpawn();
            referenceItem.OnClick = () =>
            {
                if (isLimitReached) return;

                var goodsInfo = vm.GetPlayerGoods(goods.Id);

                // 消耗品检查
                if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
                {
                    var consume = goods.BuyConsumes[0];
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    if (playerItemCount < consume.Num)
                    {
                        string itemName = vm.GetItemConfig(consume.Id)?.Name ?? "物品";
                        FusionContinent.Toast.Show(itemName + "不足");
                        return;
                    }

                    var msg = $"是否消耗{vm.GetConsumeString(goods.BuyConsumes)}";
                    GameMainPanel.Default.ShowConfirm("购买确认", msg, () =>
                    {
                        goodsInfo.BuyGoods(1, () =>
                        {
                            vm.OnRefreshCommand.Execute();
                            RefreshShopList(vm);
                        });
                    });
                }
                else
                {
                    goodsInfo.BuyGoods(1, () =>
                    {
                        vm.OnRefreshCommand.Execute();
                        RefreshShopList(vm);
                    });
                }
            };
        }

        #endregion

        #region 商店列表刷新

        /// <summary>
        /// 刷新商店列表
        /// </summary>
        public void RefreshShopList(SoldierShopListViewModel vm)
        {
            if (mOldPanel?.ShopListScroll == null || vm.GoodsList.Value == null) return;

            mOldPanel.ShopListScroll.ShowList(vm.GoodsList.Value.Count);
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
            // 刷新列表
            if (VM != null && VM.ShopId.Value > 0)
            {
                RefreshShopList(VM);
            }
        }

        protected override void OnHide()
        {

        }

        #endregion
    }
}
