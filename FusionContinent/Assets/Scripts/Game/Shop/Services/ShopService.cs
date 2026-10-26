using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using RewardAndNum = Config.RewardAndNum;

namespace Game.Shop
{
    /// <summary>
    /// 商店服务 - 管理商店数据和购买逻辑
    /// </summary>
    public class ShopService : GameServiceBase
    {
        #region 配置数据
        private TbShopConfig mShopConfig;
        private TbShopGoodsConfig mGoodsConfig;
        private TbItemConfig mItemConfig;
        private Dictionary<QualityType, string> mQualityBgs = new Dictionary<QualityType, string>();
        #endregion

        #region 属性
        public bool IsInitialized { get; private set; }
        private Dictionary<int, ShopItemData> mShopItems = new Dictionary<int, ShopItemData>();
        #endregion

        #region 事件
        public event System.Action<int> OnItemPurchased;
        public event System.Action<string> OnPurchaseError;
        public event System.Action<int> OnGoodsChanged; // 商品ID变化
        public event System.Action<int> OnChapterGiftPackPurchased; // 章节ID
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            // 加载配置
            mShopConfig = DataConfigManager.Tables.TbShopConfig;
            mGoodsConfig = DataConfigManager.Tables.TbShopGoodsConfig;
            mItemConfig = DataConfigManager.Tables.TbItemConfig;

            // 加载品质背景配置
            var shopBg = DataConfigManager.Tables.TbGameBattleConst.ShopQualityType;
            foreach (var v in shopBg)
            {
                mQualityBgs[v.Type] = v.ShopBg;
            }
            
            // 加载商店数据
            LoadShopData();
            
            IsInitialized = true;
            LogTool.Log("[ShopService] 初始化完成");
        }

        #region 数据加载

        private void LoadShopData()
        {
            // 从配置表加载商店数据
            var shopConfigs = mShopConfig?.DataList;
            if (shopConfigs != null)
            {
                LogTool.Log($"[ShopService] 加载了 {shopConfigs.Count} 个商店配置");
            }
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 根据商店类型获取商店ID
        /// </summary>
        public int GetShopIdByType(ShopType shopType)
        {
            var shop = mShopConfig.DataList.FirstOrDefault(x => x.Type == shopType);
            return shop?.Id ?? 0;
        }

        /// <summary>
        /// 获取商店配置列表
        /// </summary>
        public List<ShopConfig> GetShopConfigs()
        {
            return mShopConfig?.DataList;
        }

        /// <summary>
        /// 获取商店商品列表
        /// </summary>
        public List<ShopGoodsConfig> GetShopItems(int shopId)
        {
            return GetGoodsList(shopId);
        }

        /// <summary>
        /// 获取商品列表
        /// </summary>
        public List<ShopGoodsConfig> GetGoodsList(int shopId)
        {
            var list = mGoodsConfig.DataList
                .Where(x => x.ShopId == shopId)
                .OrderBy(x => x.Id)
                .ToList();

            // 特殊处理：10005 商店随机显示9个
            if (shopId == 10005)
            {
                System.Random random = new System.Random();
                list = list.OrderBy(x => random.Next()).Take(9).ToList();
            }

            return list;
        }

        /// <summary>
        /// 获取商品配置
        /// </summary>
        public ShopGoodsConfig GetGoodsConfig(int goodsId)
        {
            return mGoodsConfig.Get(goodsId);
        }

        /// <summary>
        /// 获取玩家商品数据
        /// </summary>
        public PlayerGoods GetPlayerGoods(int goodsId)
        {
            return Global.GetGoodsById(goodsId);
        }

        /// <summary>
        /// 获取品质颜色
        /// </summary>
        public Color32 GetQualityColor(QualityType quality)
        {
            return GlobalFunc.GetQualityStyle(quality).MainColor;
        }

        /// <summary>
        /// 获取品质背景
        /// </summary>
        public string GetQualityBg(QualityType quality)
        {
            return mQualityBgs.TryGetValue(quality, out var bg) ? bg : "";
        }

        /// <summary>
        /// 检查是否可以购买
        /// </summary>
        public bool CanBuy(int goodsId, out string errorMessage)
        {
            errorMessage = "";
            var goods = GetGoodsConfig(goodsId);
            if (goods == null)
            {
                errorMessage = "商品不存在";
                return false;
            }

            var playerGoods = GetPlayerGoods(goodsId);
            int currentBuyNum = playerGoods?.BuyNum ?? 0;
            
            // 限购检查
            if (goods.LimitNum != 0 && currentBuyNum >= goods.LimitNum)
            {
                errorMessage = "购买次数不足";
                return false;
            }

            // 消耗品检查（每日商店应用折扣价）
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0];
                int needNum = consume.Num;
                if (Global.PlayerShops.TryGetValue(goods.ShopId, out var playerShop)
                    && playerShop.DiscountRate > 0
                    && playerShop.DiscountRate < 100)
                {
                    needNum = Mathf.CeilToInt(consume.Num * playerShop.DiscountRate / 100f);
                }

                long playerItemCount = Global.GetItemNumById(consume.Id);
                if (playerItemCount < needNum)
                {
                    var itemCfg = mItemConfig.GetOrDefault(consume.Id);
                    errorMessage = (itemCfg?.Name ?? "物品") + "不足";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取消耗品显示字符串
        /// </summary>
        public string GetConsumeString(List<RewardAndNum> consumes)
        {
            if (consumes == null || consumes.Count == 0) return "";
            return Items.GetRewardsStr(consumes);
        }

        /// <summary>
        /// 获取商店类型
        /// </summary>
        public ShopType GetShopType(int shopId)
        {
            var configs = GetShopConfigs();
            if (configs != null)
            {
                foreach (var config in configs)
                {
                    if (config.Id == shopId)
                    {
                        return config.Type;
                    }
                }
            }
            return ShopType.Cardgroups;
        }

        #endregion

        #region 购买方法

        /// <summary>
        /// 购买商品
        /// </summary>
        public async UniTask<bool> PurchaseItem(int itemId, int count = 1)
        {
            // TODO: 实现购买逻辑
            // 1. 检查货币是否足够
            // 2. 扣除货币
            // 3. 添加物品到背包
            // 4. 触发购买成功事件
            
            LogTool.Log($"[ShopService] 购买商品: {itemId}, 数量: {count}");
            
            // 模拟购买成功
            await UniTask.Delay(500);
            
            OnItemPurchased?.Invoke(itemId);
            return true;
        }

        /// <summary>
        /// 购买商品（新接口）
        /// </summary>
        public async UniTask<BuyResult> BuyGoodsAsync(int goodsId, int count)
        {
            if (!CanBuy(goodsId, out var error))
            {
                return new BuyResult { Success = false, ErrorMessage = error };
            }

            try
            {
                var playerGoods = GetPlayerGoods(goodsId);
                
                // 使用 UniTask 包装回调
                var tcs = new UniTaskCompletionSource<bool>();
                
                playerGoods.BuyGoods(count, () =>
                {
                    tcs.TrySetResult(true);
                });

                bool success = await tcs.Task;
                
                if (success)
                {
                    OnGoodsChanged?.Invoke(goodsId);
                    return new BuyResult { Success = true, GoodsId = goodsId };
                }
                else
                {
                    return new BuyResult { Success = false, ErrorMessage = "购买失败" };
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[ShopService] 购买失败: {ex.Message}");
                return new BuyResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// 检查是否可以购买
        /// </summary>
        public bool CanPurchase(int itemId)
        {
            // TODO: 检查货币、等级等条件
            return true;
        }

        /// <summary>
        /// 关卡礼包是否已购买
        /// </summary>
        public bool IsChapterGiftPackPurchased(int chapterId)
        {
            return Global.ChapterGiftPacks.TryGetValue(chapterId, out var pack) && pack.IsPurchase;
        }

        /// <summary>
        /// 玩家当前到达的章节（按最高通关关卡所属章节）
        /// </summary>
        public int GetPlayerReachChapterId()
        {
            int clearStageId = Global.PlayerBattle.ClearStageId;
            if (clearStageId <= 0) return 0;

            var clearStageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(clearStageId);
            return clearStageCfg?.ChapterId ?? 0;
        }

        /// <summary>
        /// 关卡礼包是否已解锁（到达该章即可）
        /// </summary>
        public bool IsChapterGiftUnlocked(int chapterId)
        {
            return GetPlayerReachChapterId() >= chapterId;
        }

        /// <summary>
        /// 可展示的关卡礼包：已到达章节且未购买
        /// </summary>
        public List<MainLevelGiftConfig> GetAvailableStageGiftPacks()
        {
            return DataConfigManager.Tables.TbMainLevelGiftConfig.DataList
                .Where(cfg => IsChapterGiftUnlocked(cfg.ChapterId))
                .Where(cfg => !IsChapterGiftPackPurchased(cfg.ChapterId))
                .OrderBy(cfg => cfg.ChapterId)
                .ToList();
        }

        /// <summary>
        /// 购买关卡礼包
        /// </summary>
        public async UniTask<bool> BuyStageGiftPackAsync(int chapterId)
        {
            if (IsChapterGiftPackPurchased(chapterId))
            {
                LogTool.LogError("已购买该关卡礼包");
                return false;
            }

            if (!IsChapterGiftUnlocked(chapterId))
            {
                LogTool.LogError("关卡礼包尚未解锁");
                return false;
            }

            var giftCfg = DataConfigManager.Tables.TbMainLevelGiftConfig.DataList
                .FirstOrDefault(c => c.ChapterId == chapterId);
            if (giftCfg == null)
            {
                LogTool.LogError("关卡礼包配置不存在");
                return false;
            }

            try
            {
                LogTool.Log("正在请求服务器购买关卡礼包，请稍候...");
                var req = new BuyStageGiftPackRequest { ChapterId = chapterId };
                var proto = await WebDataFun.SendProtoPostRequest<BuyStageGiftPackResponse>(
                    ApiKeys.ApiBuyStageGiftPack, req);

                if (proto == null)
                {
                    throw new Exception("服务器返回空响应");
                }

                if (proto.ChapterGiftPack != null)
                {
                    var local = ProtoMappers.MapChapterGiftPack(proto.ChapterGiftPack);
                    if (local != null)
                    {
                        Global.ChapterGiftPacks[local.ChapterId] = local;
                    }
                }

                Items.Update(proto.Items);
                ShowStageGiftRewards(proto.Rewards);
                OnChapterGiftPackPurchased?.Invoke(chapterId);
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogError($"购买关卡礼包失败：{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private static void ShowStageGiftRewards(IDictionary<int, long> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }

            var rewardsDict = new Dictionary<string, int>();
            foreach (var kv in rewards)
            {
                rewardsDict[kv.Key.ToString()] = (int)kv.Value;
            }

            foreach (var item in RewardPopManager.FilterCard(rewardsDict))
            {
                var itemId = int.Parse(item.Key);
                RewardPopManager.Instance.PushCardShow(itemId);
            }

            RewardPopManager.Instance.SetRewardShowValue(RewardPopManager.FilterReward(rewardsDict));
            RewardPopManager.Instance.Pop();
        }

        #endregion
    }

    #region 数据类

    public class ShopItemData
    {
        public int ItemId;
        public int Count;
        public int Price;
        public bool IsPurchased;
    }

    public class BuyResult
    {
        public bool Success;
        public string ErrorMessage;
        public int GoodsId;
    }

    #endregion
}
