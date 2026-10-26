using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using Config;
using System.Linq;
using UIFramework.Services;
using FusionContinent;
using Cysharp.Threading.Tasks;
using System;
using FusionContinent.Model;
using Game.UserTool;

namespace Game.Shop
{
    /// <summary>
    /// 每日商店列表 ViewModel
    /// 数据来源：Global.PlayerGoods（服务器动态分配）+ Global.PlayerShops（折扣率）
    /// </summary>
    public class DailyShopListViewModel : ViewModel, IUserToolConfigurable
    {
        #region 属性

        public BindableProperty<int> CurrentShopId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<ShopGoodsConfig>> GoodsList { get; } = new BindableProperty<List<ShopGoodsConfig>>();
        public BindableProperty<Dictionary<int, int>> LocalBuyCounts { get; } = new BindableProperty<Dictionary<int, int>>();
        public BindableProperty<int> DiscountRate { get; } = new BindableProperty<int>(0);

        #endregion

        #region 命令

        public Command<int> OnBuyGoodsCommand { get; private set; }

        #endregion

        #region 依赖

        private ShopService mShopService;
        private UserToolData userToolData;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mShopService = GameServiceManager.Get<ShopService>();

            OnBuyGoodsCommand = new Command<int>(OnBuyGoods);
            LocalBuyCounts.Value = new Dictionary<int, int>();

            LogTool.Log("[DailyShopListViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[DailyShopListViewModel] 销毁");
        }

        /// <summary>
        /// 初始化商店
        /// </summary>
        public void Initialize(int shopId)
        {
            CurrentShopId.Value = shopId;
            InitUserToolData();
            RefreshGoodsList();
            RefreshDiscountRate();
        }

        void InitUserToolData()
        {
            userToolData = new Game.UserTool.UserToolData
            {
                ShowAvatar = false,
                ItemIds = new List<int> { 1010002, 1010001, 1010007 }  // 钻石、金币、点券
            };
        }

        private async void OnBuyGoods(int goodsId)
        {
            var result = await mShopService.BuyGoodsAsync(goodsId, 1);
            if (result.Success)
            {
                if (LocalBuyCounts.Value.ContainsKey(goodsId))
                    LocalBuyCounts.Value[goodsId]++;
                else
                    LocalBuyCounts.Value[goodsId] = 1;

                OnBuySuccess?.Invoke(goodsId);
                RefreshGoodsList();
            }
            else
            {
                OnBuyFailed?.Invoke(result.ErrorMessage);
            }
        }

        /// <summary>
        /// 刷新商品列表 —— 从 Global.PlayerGoods 中筛选属于本商店的商品
        /// </summary>
        public void RefreshGoodsList()
        {
            if (CurrentShopId.Value <= 0) return;

            var goodsList = new List<ShopGoodsConfig>();

            foreach (var kv in Global.PlayerGoods)
            {
                var playerGoods = kv.Value;
                if (playerGoods.ShopId != CurrentShopId.Value) continue;

                var goodsCfg = mShopService.GetGoodsConfig(playerGoods.GoodsId);
                if (goodsCfg != null)
                {
                    goodsList.Add(goodsCfg);
                }
            }

            GoodsList.Value = goodsList;
        }

        /// <summary>
        /// 刷新折扣率 —— 从 Global.PlayerShops 读取
        /// </summary>
        public void RefreshDiscountRate()
        {
            if (CurrentShopId.Value <= 0) return;

            var playerShop = Global.PlayerShops.TryGetValue(CurrentShopId.Value, out var shop) ? shop : null;
            DiscountRate.Value = playerShop?.DiscountRate ?? 0;
        }

        #region IUserToolConfigurable 实现

        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(Config.PageType.ShopPage);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 计算折后价格（向上取整）
        /// </summary>
        public int GetDiscountedPrice(int originalPrice, int discountRate)
        {
            return Mathf.CeilToInt(originalPrice * discountRate / 100f);
        }

        public ShopGoodsConfig GetGoodsConfig(int goodsId)
        {
            return mShopService.GetGoodsConfig(goodsId);
        }

        public PlayerGoods GetPlayerGoods(int goodsId)
        {
            return Global.GetGoodsById(goodsId);
        }

        public Color32 GetQualityColor(QualityType quality)
        {
            return mShopService.GetQualityColor(quality);
        }

        public bool CanBuy(int goodsId)
        {
            return mShopService.CanBuy(goodsId, out _);
        }

        public string GetConsumeString(List<RewardAndNum> consumes)
        {
            return mShopService.GetConsumeString(consumes);
        }

        #endregion

        #region 事件

        public System.Action<int> OnBuySuccess;
        public System.Action<string> OnBuyFailed;

        #endregion
    }
}
