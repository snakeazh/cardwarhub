using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using Config;
using System.Linq;
using UIFramework.Services;
using FusionContinent;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using Game.UserTool;

namespace Game.Shop
{
    /// <summary>
    /// 普通商店列表 ViewModel
    /// </summary>
    public class NormalShopList01ViewModel : ViewModel, IUserToolConfigurable
    {
        #region 属性
        public BindableProperty<int> CurrentShopId { get; } = new BindableProperty<int>(0);
        public BindableProperty<ShopType> CurrentShopType { get; } = new BindableProperty<ShopType>();
        public BindableProperty<bool> IsTitleMode { get; } = new BindableProperty<bool>(false);
        public BindableProperty<List<ShopGoodsConfig>> GoodsList { get; } = new BindableProperty<List<ShopGoodsConfig>>();
        public BindableProperty<Dictionary<int, int>> LocalBuyCounts { get; } = new BindableProperty<Dictionary<int, int>>();
        #endregion

        #region 命令
        public Command<int> OnSelectShopCommand { get; private set; }
        public Command<int> OnBuyGoodsCommand { get; private set; }
        public Command OnRefreshCommand { get; private set; }
        #endregion

        #region 依赖
        private ShopService mShopService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mShopService = GameServiceManager.Get<ShopService>();
            
            // 初始化命令
            OnSelectShopCommand = new Command<int>(OnSelectShop);
            OnBuyGoodsCommand = new Command<int>(OnBuyGoods);
            OnRefreshCommand = new Command(OnRefresh);
            
            // 初始化本地购买计数
            LocalBuyCounts.Value = new Dictionary<int, int>();
            
            LogTool.Log("[NormalShopList01ViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[NormalShopList01ViewModel] 销毁");
        }

        private UserToolData userToolData;
        /// <summary>
        /// 初始化商店
        /// </summary>
        public void Initialize(int shopId, bool isTitle = false)
        {
            CurrentShopId.Value = shopId;
            IsTitleMode.Value = isTitle;
            InitUserToolData();
            RefreshGoodsList();
        }

        void InitUserToolData()
        {
            var ShopConfig = DataConfigManager.Tables.TbShopConfig.Get(CurrentShopId.Value);
            switch (ShopConfig.Type)
            {
                case ShopType.Gem:
                    userToolData = new Game.UserTool.UserToolData
                    {
                        ShowAvatar = false,  // 显示头像
                        ItemIds = new List<int> { 1010002, 1010001, 1010007 }  // 依次显示的资源ID
                    };
                    break;
                case ShopType.ItemMaterial:
                    userToolData = new Game.UserTool.UserToolData
                    {
                        ShowAvatar = false,  // 显示头像
                        ItemIds = new List<int> { 1010002, 1010001, 1010003 }  // 依次显示的资源ID
                    };
                    break;
            }
        }

        /// <summary>
        /// 按商店类型初始化
        /// </summary>
        public void InitializeByType(ShopType shopType, bool isTitle = false)
        {
            var shopId = mShopService.GetShopIdByType(shopType);
            if (shopId > 0)
            {
                Initialize(shopId, isTitle);
            }
        }

        private void OnSelectShop(int shopId)
        {
            CurrentShopId.Value = shopId;
            RefreshGoodsList();
        }

        private async void OnBuyGoods(int goodsId)
        {
            var result = await mShopService.BuyGoodsAsync(goodsId, 1);
            if (result.Success)
            {
                // 更新本地购买计数
                if (LocalBuyCounts.Value.ContainsKey(goodsId))
                    LocalBuyCounts.Value[goodsId]++;
                else
                    LocalBuyCounts.Value[goodsId] = 1;
                
                // 触发购买成功事件
                OnBuySuccess?.Invoke(goodsId);
                
                // 刷新商品列表
                RefreshGoodsList();
            }
            else
            {
                OnBuyFailed?.Invoke(result.ErrorMessage);
            }
        }

        private void OnRefresh()
        {
            RefreshGoodsList();
        }

        private void RefreshGoodsList()
        {
            if (CurrentShopId.Value <= 0) return;
            
            var goods = mShopService.GetGoodsList(CurrentShopId.Value);
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

        public Color32 GetQualityColor(QualityType quality)
        {
            return mShopService.GetQualityColor(quality);
        }

        public string GetQualityBg(QualityType quality)
        {
            return mShopService.GetQualityBg(quality);
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

        #region IUserToolConfigurable 实现

        /// <summary>
        /// 获取 UserTool 配置
        /// </summary>
        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(Config.PageType.ShopPage);
        }

        #endregion

        #region 事件

        public System.Action<int> OnBuySuccess;
        public System.Action<string> OnBuyFailed;

        #endregion
    }
}
