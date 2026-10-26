using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using Config;
using UIFramework.Services;
using FusionContinent;
using FusionContinent.Model;
using Game.UserTool;

namespace Game.Shop
{
    /// <summary>
    /// 士兵商店列表 ViewModel
    /// </summary>
    public class SoldierShopListViewModel : ViewModel, IUserToolConfigurable
    {
        #region 属性
        public BindableProperty<int> ShopId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<ShopGoodsConfig>> GoodsList { get; } = new BindableProperty<List<ShopGoodsConfig>>();
        #endregion

        #region 命令
        public Command OnRefreshCommand { get; private set; }
        public Command<int> OnBuyGoodsCommand { get; private set; }
        public Command<int> OnShowEquipDetailsCommand { get; private set; }
        #endregion

        #region 依赖
        private ShopService mShopService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mShopService = GameServiceManager.Get<ShopService>();
            
            // 初始化命令
            OnRefreshCommand = new Command(OnRefresh);
            OnBuyGoodsCommand = new Command<int>(OnBuyGoods);
            OnShowEquipDetailsCommand = new Command<int>(OnShowEquipDetails);
            
            // 监听商店ID变化，自动刷新数据
            ShopId.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal > 0)
                {
                    RefreshGoodsList();
                }
            };
            
            LogTool.Log("[SoldierShopListViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[SoldierShopListViewModel] 销毁");
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int shopId)
        {
            ShopId.Value = shopId;
        }

        private void OnRefresh()
        {
            RefreshGoodsList();
        }

        private async void OnBuyGoods(int goodsId)
        {
            var result = await mShopService.BuyGoodsAsync(goodsId, 1);
            if (result.Success)
            {
                OnBuySuccess?.Invoke(goodsId);
                RefreshGoodsList();
            }
            else
            {
                OnBuyFailed?.Invoke(result.ErrorMessage);
            }
        }

        private void OnShowEquipDetails(int equipId)
        {
            OnShowEquipDetailsRequested?.Invoke(equipId);
        }

        private void RefreshGoodsList()
        {
            if (ShopId.Value <= 0) return;
            
            var goods = mShopService.GetGoodsList(ShopId.Value);
            GoodsList.Value = goods;
        }

        #region 公共方法

        public ShopGoodsConfig GetGoodsConfig(int goodsId)
        {
            return mShopService.GetGoodsConfig(goodsId);
        }

        public PlayerGoods GetPlayerGoods(int goodsId)
        {
            return mShopService.GetPlayerGoods(goodsId);
        }

        public ItemConfig GetItemConfig(int itemId)
        {
            return DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
        }

        public bool IsCardUnlocked(int cardId)
        {
            return Global.GetCardById(cardId) != null;
        }

        public string GetConsumeString(List<RewardAndNum> consumes)
        {
            return mShopService.GetConsumeString(consumes);
        }

        #endregion

        #region IUserToolConfigurable 实现

        /// <summary>
        /// 士兵商店显示：钻石、英雄碎片
        /// </summary>
        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(Config.PageType.ShopPage);
        }

        #endregion

        #region 事件

        public System.Action<int> OnBuySuccess;
        public System.Action<string> OnBuyFailed;
        public System.Action<int> OnShowEquipDetailsRequested;

        #endregion
    }
}
