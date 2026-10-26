using System.Collections.Generic;
using Game.Shop;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UIFramework;
using Config;
using ScrollView;
using FusionContinent.Model;

namespace FusionContinent
{
    /// <summary>
    /// 秘境挑战商店列表 - 新框架版本 (MVVM)
    /// 复用原 ChallengeShopList 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("ChallengeShopList")]
    public class ChallengeShopListNew : UIFramework.UIPanel<ChallengeShopListViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private ChallengeShopList mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<ChallengeShopList>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ChallengeShopListNew] 找不到 ChallengeShopList 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            // 旧 Panel 的 OnInit 会设置 ScrollView 的 UpdateFunc，这里由 New 接管，停用旧逻辑
            mOldPanel.enabled = false;

            LogTool.Log("[ChallengeShopListNew] 已关联 ChallengeShopList");
        }

        protected override void OnBindViewModel(ChallengeShopListViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭入口：Mask 遮罩 + Exit 按钮（原代码被注释，这里补齐关闭逻辑）
            mOldPanel.Mask.onClick.AddListener(OnCloseClicked);
            mOldPanel.Exit.onClick.AddListener(OnCloseClicked);

            // 商品列表变化 -> 刷新 ScrollView
            vm.GoodsList.OnValueChanged += OnGoodsListChanged;

            // 接管 ScrollView 更新回调
            mOldPanel.ShopListScroll.SetUpdateFunc(OnUpdateShopItem);

            // ShopId 已设置时立即刷新一次
            if (vm.ShopId.Value > 0)
            {
                ShowList(vm);
            }
        }

        protected override void OnUnbindViewModel(ChallengeShopListViewModel vm)
        {
            if (mOldPanel != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(OnCloseClicked);
                mOldPanel.Exit.onClick.RemoveListener(OnCloseClicked);

                if (mOldPanel.ShopListScroll != null)
                {
                    mOldPanel.ShopListScroll.SetUpdateFunc(null);
                }
            }

            vm.GoodsList.OnValueChanged -= OnGoodsListChanged;
        }

        #region 关闭

        private void OnCloseClicked()
        {
            CloseSelf();
        }

        #endregion

        #region 商品列表刷新

        private void OnGoodsListChanged(List<ShopGoodsConfig> oldVal, List<ShopGoodsConfig> newVal)
        {
            ShowList(VM);
        }

        private void ShowList(ChallengeShopListViewModel vm)
        {
            if (mOldPanel == null || vm.GoodsList.Value == null) return;
            mOldPanel.ShopListScroll.ShowList(vm.GoodsList.Value.Count);
        }

        #endregion

        #region ScrollView 项更新

        private void OnUpdateShopItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM == null || VM.GoodsList.Value == null || dataIndex >= VM.GoodsList.Value.Count) return;

            var referenceItem = scrollItem.GetComponent<ShopItem>();
            var goods = VM.GoodsList.Value[dataIndex];
            var goodsInfo = VM.GetPlayerGoods(goods.Id);

            // 消耗品 / 免费显示
            UpdateConsumeUI(referenceItem, goods);

            referenceItem.shopnametxt.text = goods.Name;
            referenceItem.shopicon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            // referenceItem.ShopNums.text = goods.BuyItemNum.ToString();

            // 限购
            bool isDailyLimit = goods.LimitType == GoodsLimitType.Daily|| goods.LimitType == GoodsLimitType.Weekly;
            referenceItem.buylimitnum.gameObject.SetActive(isDailyLimit);
            if (isDailyLimit)
            {
                int buyNum = goodsInfo != null ? goodsInfo.BuyNum : 0;
                referenceItem.buylimitnum.text = "限购" + $"{goods.LimitNum - buyNum}/{goods.LimitNum}";
            }

            bool isLimitReached = goods.LimitNum - (goodsInfo != null ? goodsInfo.BuyNum : 0) <= 0;

            var bagItem = referenceItem.GetComponent<BagScrollItem>();
            bagItem.OnSpawn();
            bagItem.OnClick = () => OnShopItemClicked(goods, goodsInfo, isLimitReached);
        }

        /// <summary>
        /// 更新消耗品显示
        /// </summary>
        private void UpdateConsumeUI(ShopItem referenceItem, ShopGoodsConfig goods)
        {
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0]; // 只取第一个
                var itemCfg = VM.GetItemConfig(consume.Id);
                if (itemCfg != null)
                {
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    referenceItem.consumebtn.transform.Find("consume").gameObject.SetActive(true);
                    referenceItem.consumeicon.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
                    referenceItem.consumenum.color = playerItemCount < consume.Num ? Color.red : Color.white;
                    referenceItem.consumenum.text = consume.Num.ToString();
                }
            }
            else
            {
                // 不收费：隐藏消耗，显示免费
                referenceItem.consumebtn.transform.Find("consume").transform.parent.gameObject.SetActive(false);
                referenceItem.consumebtn.transform.Find("freetxt").gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 商店项购买点击回调（由 BagScrollItem.OnClick 触发）
        /// </summary>
        private void OnShopItemClicked(ShopGoodsConfig goods, PlayerGoods goodsInfo, bool isLimitReached)
        {
            if (goods == null || goodsInfo == null) return;

            if (isLimitReached)
            {
                FusionContinent.Toast.Show("购买次数已用完");
                return;
            }

            // 消耗品检查
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0];
                long playerItemCount = Global.GetItemNumById(consume.Id);
                if (playerItemCount < consume.Num)
                {
                    string itemName = VM.GetItemConfig(consume.Id)?.Name ?? "物品";
                    FusionContinent.Toast.Show(itemName + "不足");
                    return;
                }

                var msg = $"是否消耗{Items.GetRewardsStr(goods.BuyConsumes)}";
                GameMainPanel.Default.ShowConfirm("购买确认", msg, () =>
                {
                    VM.OnBuyGoodsCommand.Execute(goods.Id);
                });
            }
            else
            {
                VM.OnBuyGoodsCommand.Execute(goods.Id);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            if (VM != null && VM.ShopId.Value > 0)
            {
                ShowList(VM);
            }
        }

        protected override void OnShow()
        {
            if (VM != null && VM.GoodsList.Value != null)
            {
                ShowList(VM);
            }
        }

        protected override void OnHide()
        {
        }

        #endregion
    }
}
