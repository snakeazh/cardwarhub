using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;
using Config;
using FusionContinent;
using Game.UserTool;

namespace Game.Shop
{
    /// <summary>
    /// 商店主界面 ViewModel
    /// 管理商店类型切换，子Panel仍使用旧框架
    /// </summary>
    public class ShopPanel01ViewModel : ViewModel
    {

        #region 属性
        public BindableProperty<int> SelectedShopIndex { get; } = new BindableProperty<int>(0);
        public BindableProperty<ShopType> CurrentShopType { get; } = new BindableProperty<ShopType>(ShopType.ItemMaterial);
        public BindableProperty<string> CurrentShopName { get; } = new BindableProperty<string>("");
        public BindableProperty<string> BannerBgPath { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsBannerVisible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> IsFootVisible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<Dictionary<string, float>> UpMoneyList { get; } = new BindableProperty<Dictionary<string, float>>(new Dictionary<string, float>());
        public int LastSelectedShopIndex { get; set; } = 0;
        #endregion

        #region 命令
        public Command<int> OnSelectShopCommand { get; private set; }
        public Command<ShopType> OnChangeShopPageCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 依赖
        private ShopService mShopService;
        #endregion

        #region 配置数据
        private TbShopConfig mShopConfig;
        private List<ShopConfig> mShopList;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service
            mShopService = GameServiceManager.Get<ShopService>();

            // 加载配置
            mShopConfig = DataConfigManager.Tables.TbShopConfig;
            mShopList = mShopConfig.DataList;

            // 初始化命令
            OnSelectShopCommand = new Command<int>(OnSelectShop);
            OnChangeShopPageCommand = new Command<ShopType>(OnChangeShopPage);
            OnCloseCommand = new Command(OnClose);

            LogTool.Log($"[ShopPanel01ViewModel] 创建完成，加载了 {mShopList.Count} 个商店配置");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[ShopPanel01ViewModel] 销毁");
        }
        

        #region 命令实现

        private void OnSelectShop(int index)
        {
            if (index < 0 || index >= mShopList.Count)
            {
                LogTool.LogWarning($"[ShopPanel01ViewModel] 无效的商店索引: {index}");
                return;
            }

            if (SelectedShopIndex.Value == index)
            {
                LogTool.Log($"[ShopPanel01ViewModel] 重复选择商店索引: {index}");
                return;
            }

            var shopCfg = mShopList[index];

            // 单机化：抽卡（卡包）与点券页签已移除，拒绝切换
            if (shopCfg.Type == ShopType.Cardgroups || shopCfg.Type == ShopType.Gem)
            {
                LogTool.LogWarning($"[ShopPanel01ViewModel] 商店类型已移除: {shopCfg.Type}");
                return;
            }
            
            SelectedShopIndex.Value = index;
            LastSelectedShopIndex = index;
            CurrentShopType.Value = shopCfg.Type;
            CurrentShopName.Value = shopCfg.Name;
            BannerBgPath.Value = shopCfg.BannerBg;

            // 触发商店切换事件，让旧Panel处理子Panel
            OnShopChanged?.Invoke(shopCfg.Type, shopCfg.Id);

            // 更新资源显示
            UpdateUpMoneyList(shopCfg.Type);

            LogTool.Log($"[ShopPanel01ViewModel] 切换到商店: {shopCfg.Name}, 类型: {shopCfg.Type}");
        }

        private void OnChangeShopPage(ShopType shopType)
        {
            // 查找对应的商店索引
            for (int i = 0; i < mShopList.Count; i++)
            {
                if (mShopList[i].Type == shopType)
                {
                    OnSelectShop(i);
                    return;
                }
            }
            
            LogTool.LogWarning($"[ShopPanel01ViewModel] 找不到商店类型: {shopType}");
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #endregion

        #region 公共方法

        public ShopConfig GetShopConfig(int index)
        {
            if (index >= 0 && index < mShopList.Count)
            {
                return mShopList[index];
            }
            return null;
        }

        public int GetShopIndexByType(ShopType shopType)
        {
            for (int i = 0; i < mShopList.Count; i++)
            {
                if (mShopList[i].Type == shopType)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetBannerFootVisibility(bool visible)
        {
            IsBannerVisible.Value = visible;
            IsFootVisible.Value = visible;
        }

        public void UpdateUpMoneyList(ShopType shopType)
        {
            var upMoneyList = new Dictionary<string, float>
            {
                { "StaminaBox", 0 }
            };

            if (shopType == ShopType.Soldier)
            {
                upMoneyList["StaminaBox"] = 1;
            }

            UpMoneyList.Value = upMoneyList;
            OnUpMoneyListChanged?.Invoke(upMoneyList);
        }

        #endregion

        #region 事件

        public System.Action<ShopType, int> OnShopChanged;
        public System.Action<Dictionary<string, float>> OnUpMoneyListChanged;
        public System.Action OnClosePanel;

        #endregion
    }
}
