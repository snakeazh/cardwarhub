using UIFramework;
using System.Collections.Generic;
using System.Linq;
using Config;
using UIFramework.Services;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Shop
{
    /// <summary>
    /// 秘境挑战商店列表 ViewModel
    /// </summary>
    public class ChallengeShopListViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> ShopId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<ShopGoodsConfig>> GoodsList { get; } = new BindableProperty<List<ShopGoodsConfig>>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<int> OnBuyGoodsCommand { get; private set; }
        #endregion

        #region 依赖
        private ShopService mShopService;
        #endregion

        #region 事件
        public System.Action<int> OnBuySuccess;
        public System.Action<string> OnBuyFailed;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mShopService = GameServiceManager.Get<ShopService>();

            OnCloseCommand = new Command(OnCloseRequested);
            OnBuyGoodsCommand = new Command<int>(OnBuyGoods);

            ShopId.OnValueChanged += OnShopIdChanged;

            LogTool.Log("[ChallengeShopListViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ShopId.OnValueChanged -= OnShopIdChanged;
            LogTool.Log("[ChallengeShopListViewModel] 销毁");
        }

        /// <summary>
        /// 初始化商店
        /// </summary>
        public void Initialize(int shopId)
        {
            ShopId.Value = shopId;
        }

        private void OnShopIdChanged(int oldVal, int newVal)
        {
            if (newVal > 0)
            {
                RefreshGoodsList();
            }
        }

        private void OnCloseRequested()
        {
            // 关闭由 View 通过 CloseSelf 处理，VM 不直接调用 Unity API
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

        /// <summary>
        /// 刷新商品列表
        /// </summary>
        public void RefreshGoodsList()
        {
            if (ShopId.Value <= 0) return;
            GoodsList.Value = mShopService.GetGoodsList(ShopId.Value)
                .OrderBy(x => x.Id)
                .ToList();
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

        public string GetConsumeString(List<RewardAndNum> consumes)
        {
            return mShopService.GetConsumeString(consumes);
        }

        #endregion
    }
}
