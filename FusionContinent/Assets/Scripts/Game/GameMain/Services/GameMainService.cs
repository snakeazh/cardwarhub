using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.GameMain
{
    /// <summary>
    /// 游戏主服务 - 管理主界面数据和全局状态
    /// </summary>
    public class GameMainService : GameServiceBase
    {
        #region 属性
        public bool IsInitialized { get; private set; }
        #endregion

        #region 事件
        public event System.Action<ItemType, int> OnResourceChanged;
        public event System.Action<int> OnLevelChanged;
        public event System.Action<int, int> OnExpChanged;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            // 订阅资源变化事件
            SubscribeResourceEvents();
            
            IsInitialized = true;
            LogTool.Log("[GameMainService] 初始化完成");
        }

        #region 事件订阅

        private void SubscribeResourceEvents()
        {
            // 体力变化
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_STAMINA, (stamina) =>
            {
                OnResourceChanged?.Invoke(ItemType.Stamina, (int)stamina);
            });

            // 金币变化
            var goldId = Global.ItemTypeIds[ItemType.Gold];
            Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{goldId}", (gold) =>
            {
                OnResourceChanged?.Invoke(ItemType.Gold, (int)gold);
            });

            // 钻石变化
            var diamondId = Global.ItemTypeIds[ItemType.Diamond];
            Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{diamondId}", (diamond) =>
            {
                OnResourceChanged?.Invoke(ItemType.Diamond, (int)diamond);
            });

            // 点券变化
            var couponId = Global.ItemTypeIds[ItemType.Coupon];
            Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{couponId}", (coupon) =>
            {
                OnResourceChanged?.Invoke(ItemType.Coupon, (int)coupon);
            });

            // 等级变化
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, (info) =>
            {
                var levelInfo = (UserLevelInfo)info;
                OnLevelChanged?.Invoke(levelInfo.Level);
                
                // 从配置表获取最大经验值
                var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(levelInfo.Level);
                int maxExp = lvCfg?.Exp ?? 100;
                OnExpChanged?.Invoke(levelInfo.Exp, maxExp);
            });
        }

        #endregion

        #region 公共方法

        public long GetResourceCount(int itemId)
        {
            ItemType type = DataConfigManager.Tables.TbItemConfig.Get(itemId).Type;
            switch (type)
            {
                case ItemType.Gold:
                    return Global.GetItemNumById(Global.ItemTypeIds[ItemType.Gold]);
                case ItemType.Diamond:
                    return Global.GetItemNumById(Global.ItemTypeIds[ItemType.Diamond]);
                case ItemType.Stamina:
                    return Global.PlayerResource.Stamina;
                case ItemType.Coupon:
                    return Global.GetItemNumById(Global.ItemTypeIds[ItemType.Coupon]);
                default:
                    return Global.GetItemNumById(itemId);
                    return 0;
            }
        }
        
        

        public UserLevelInfo GetUserLevelInfo()
        {
            return Global.User.GetLevelInfo();
        }

        #endregion
    }
}
