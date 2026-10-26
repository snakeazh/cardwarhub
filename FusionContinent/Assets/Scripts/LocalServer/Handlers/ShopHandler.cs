using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 商店 Handler，逐行对照 Server/app/api/application/shopapplication/application_shop.go。
    /// 存档读写对照 Server/app/model/ShopModel（内存 Archive.Shops/ShopGoods 替代 MySQL）。
    /// 日限购次数清零与商品刷新由 DailyResetRunner.ResetShop 负责，本 Handler 只做购买。
    /// </summary>
    public class ShopHandler
    {
        // 对照 application_shop.go:30-101 BuyGoodsHandler
        public BuyGoodsResponse BuyGoods(LocalServerContext ctx, BuyGoodsRequest req)
        {
            // 对照 :31-35
            ShopGoodsConfig goodsCfg = DataConfigManager.Tables.TbShopGoodsConfig.GetOrDefault(req.GoodsId);
            if (goodsCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 商品[{req.GoodsId}]，配置不存在");
                throw new LocalServerException($"商品[{req.GoodsId}]，配置不存在");
            }

            // 对照 :37-43 ShopModel.GetGoodsByGoodsId：查不到返回零值行（Id=0），补齐字段后待 Save 插入
            PlayerShopGoods goods = FindGoods(ctx, goodsCfg.Id);
            bool isNew = goods == null;
            if (isNew)
            {
                goods = new PlayerShopGoods
                {
                    Id = NextShopGoodsId(ctx),
                    UserId = ctx.UserId,
                    ShopId = goodsCfg.ShopId,
                    GoodsId = goodsCfg.Id,
                };
            }

            // 对照 :45-48（日限购商品的 BuyNum 清零在 DailyResetRunner.ResetShop，与本处口径一致）
            if (goodsCfg.LimitNum > 0 && goods.BuyNum + req.Count > goodsCfg.LimitNum)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 超出购买次数");
                throw new LocalServerException("超出购买次数");
            }

            // 对照 :50：buyItemNum 仅用于钻石商店充值计数
            // 注意 Go 的 :77 下发道具数量用 goodsCfg.BuyItemNum（未乘 Count），保留 Go 原口径
            int buyItemNum = goodsCfg.BuyItemNum * req.Count;

            // 对照 :52-59：钻石商店购买累计每日/累计充值
            // Go 此处 Get 缺配会 nil 解引用 panic，本地 fail fast 同样直接抛
            ShopConfig shopCfg = DataConfigManager.Tables.TbShopConfig.Get(goodsCfg.ShopId);
            PlayerActivity activity = null;
            if (shopCfg.Type == ShopType.Gem)
            {
                activity = GetOrCreateActivity(ctx);
                activity.DailyCharge += buyItemNum;
                activity.AccumulateCharge += buyItemNum;
                ctx.Store.MarkDirty(PlayerArchive.ActivityFieldNumber);
            }

            // 对照 :61-74 计算消耗道具（含用户商店折扣率）
            var items = new Dictionary<int, long>();
            if (goodsCfg.BuyConsumes.Count > 0)
            {
                double rideNum = req.Count;
                PlayerShop userShop = FindShop(ctx, goodsCfg.ShopId);
                if (userShop != null && userShop.Id != 0 && userShop.DiscountRate > 0)
                {
                    rideNum = userShop.DiscountRate / 100.0 * rideNum;
                }
                items = ctx.ItemEntrance.RewardToItemsRide(goodsCfg.BuyConsumes, rideNum, true);
            }

            // 对照 :77 购买的道具（见上：Go 未乘 Count，保留原口径）
            items[goodsCfg.BuyItemId] = goodsCfg.BuyItemNum;

            // 对照 :82-89：事务内统一道具入口；单机同步执行，Execute 失败等价于事务回滚（下方存档字段不写）
            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(items);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 购买商品失败: {e.Message}");
                throw new LocalServerException("购买商品失败");
            }

            // 对照 :92-93 保存购买次数
            goods.BuyNum += req.Count;
            if (isNew)
            {
                ctx.Store.Archive.ShopGoods.Add(goods);
            }
            ctx.Store.MarkDirty(PlayerArchive.ShopGoodsFieldNumber);

            // 对照 shop_converter.go toBuyGoodsResponse：Rewards = rsItems.ExpandedItems（展开后正向道具）
            var resp = new BuyGoodsResponse { Goods = goods, Items = rsItems };
            foreach (var kv in ctx.ItemEntrance.ExpandedItems)
            {
                resp.Rewards[kv.Key] = kv.Value;
            }
            if (activity != null)
            {
                resp.Activity = activity;
            }
            return resp;
        }

        // 对照 ShopModel.GetGoodsByGoodsId：查不到返回 null（Go 返回零值 struct，调用方按 Id==0 处理）
        private PlayerShopGoods FindGoods(LocalServerContext ctx, int goodsId)
        {
            foreach (PlayerShopGoods goods in ctx.Store.Archive.ShopGoods)
            {
                if (goods.GoodsId == goodsId)
                {
                    return goods;
                }
            }
            return null;
        }

        // 对照 ShopModel.GetByShopId：查不到返回 null
        private PlayerShop FindShop(LocalServerContext ctx, int shopId)
        {
            foreach (PlayerShop shop in ctx.Store.Archive.Shops)
            {
                if (shop.ShopId == shopId)
                {
                    return shop;
                }
            }
            return null;
        }

        // 对照 activityModel.GetActivity；无存档块则按当前 UserId 懒建（同 ItemEntrance.GetOrCreateActivity 口径）
        private PlayerActivity GetOrCreateActivity(LocalServerContext ctx)
        {
            if (ctx.Store.Archive.Activity == null)
            {
                ctx.Store.Archive.Activity = new PlayerActivity { Id = ctx.UserId };
            }
            return ctx.Store.Archive.Activity;
        }

        // proto Id（原 MySQL 自增主键）在集合内递增分配，同 NewPlayerInitializer 口径
        private int NextShopGoodsId(LocalServerContext ctx)
        {
            int maxId = 0;
            foreach (PlayerShopGoods goods in ctx.Store.Archive.ShopGoods)
            {
                if (goods.Id > maxId)
                {
                    maxId = goods.Id;
                }
            }
            return maxId + 1;
        }
    }
}
