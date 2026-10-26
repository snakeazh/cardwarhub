using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using FusionContinent.Manager;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 每日重置。对照 Server/app/service/ArchiveService/PlayerArchiveService.go:18-51 DailyResetData
    /// 及其调用的各 service DailyReset 逻辑，登录时（读档成功后、更新登录时间前）由 LoginHandler 调用。
    /// 跳过的模块：邮件/好友/聊天/PvP/充值活动（正被移除）。
    /// 任务的日重置本身不在这里做——由 TaskEngine.SetProgress→TryRefreshDaily 懒刷新自然触发（Go 口径）。
    /// 判定口径偏差：Go 按自然日比较（utils.TimeToDate），单机按每日重置点（凌晨 TbGameConst.GameResetHour 点）
    /// 判定，复用 ServerTimeManager.GetDayInterval 的跨天计算。
    /// </summary>
    public class DailyResetRunner
    {
        private readonly LocalServerContext _ctx;

        public DailyResetRunner(LocalServerContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 对照 DailyResetData（PlayerArchiveService.go:18-51）。返回是否执行了重置。
        /// </summary>
        public bool Run(bool isCreateUser)
        {
            // 对照 :19：同一重置日内不重复重置；新建档（isCreateUser）必跑一次
            if (!isCreateUser && !NeedReset())
            {
                return false;
            }

            ResetActivity();
            ResetCreateActivity();
            ResetShop();
            ResetBattle();
            ResetChallenge();
            ResetEndlessTower();
            ResetChallengeBoss();
            ResetHiddenRealmTower();

            // 设置任务进度 - 每日登录（对照 PlayerArchiveService.go:48）
            // 注意 Go 新档当日会 +2（user.go:139 建档一次 + DailyResetData:48 强制跑一次），此处保留 Go 原口径
            _ctx.TaskEngine.SetProgress(TaskTargetType.DailyLoginToGames, 1, 0, false);

            LogTool.Log($"[DailyResetRunner] 每日重置完成 isCreateUser={isCreateUser}");
            return true;
        }

        /// <summary>
        /// 上次登录时间与当前时间是否跨过每日重置点（凌晨 GameResetHour 点）。
        /// 对照 utils.TimeToDate 比较（PlayerArchiveService.go:19），口径改为 5 点重置点。
        /// </summary>
        private bool NeedReset()
        {
            string lastLoginTime = _ctx.Store.User.LoginTime;
            if (string.IsNullOrEmpty(lastLoginTime) ||
                !DateTimeOffset.TryParse(lastLoginTime, out DateTimeOffset last))
            {
                // 登录时间缺失/解析失败：重置幂等，按需要重置处理并暴露日志
                LogTool.LogWarning($"[DailyResetRunner] 上次登录时间无法解析: {lastLoginTime}，按跨天处理");
                return true;
            }
            long lastTs = last.ToUnixTimeSeconds();
            long nowTs = DateTimeOffset.Now.ToUnixTimeSeconds();
            return ServerTimeManager.GetDayInterval(lastTs, nowTs) > 0;
        }

        // 对照 ActivityService.DailyResetActivityData（PlayerActivityService.go:10-19）：仅清 DailyCharge
        private void ResetActivity()
        {
            if (_ctx.Store.Archive.Activity == null)
            {
                // 对照 :12-15 记录不存在时懒建（CreateSinginDate=昨天）
                _ctx.Store.Archive.Activity = new PlayerActivity
                {
                    Id = _ctx.UserId,
                    CreateSinginDate = FormatTime(DateTime.Now.AddDays(-1)),
                };
            }
            _ctx.Store.Archive.Activity.DailyCharge = 0;
            _ctx.Store.MarkDirty(PlayerArchive.ActivityFieldNumber);
        }

        // 对照 ActivityService.DailyResetCreateActivityData（CreatePlayerActivityService.go:10-35）
        // + ResetCreateActivityData（UserCreatePlayerActivityModel.go:128-139）：PartReceives 归 "{}"、ResetTime=now
        private void ResetCreateActivity()
        {
            bool changed = false;
            foreach (UserCreatePlayerActivity activity in _ctx.Store.Archive.CreatePlayerActivitys)
            {
                if (activity.PartReceives == "{}")
                {
                    continue;
                }
                CreatePlayerActivityConfig cfg = DataConfigManager.Tables.TbCreatePlayerActivityConfig.GetOrDefault(activity.ActivityId);
                if (cfg == null)
                {
                    continue; // Go 端此处会 nil 解引用 panic，单机按配置缺失跳过
                }

                bool needReset = false;
                switch (cfg.ResetType)
                {
                    case ResetType.Daily:
                        needReset = true;
                        break;
                    case ResetType.Week:
                        // 对照 UserCreatePlayerActivity.IsNewWeek（:81-84）：ResetTime 是周一即重置（保留 Go 原口径）
                        if (DateTimeOffset.TryParse(activity.ResetTime, out DateTimeOffset resetTime)
                            && resetTime.DayOfWeek == DayOfWeek.Monday)
                        {
                            needReset = true;
                        }
                        break;
                }
                if (!needReset)
                {
                    continue;
                }
                activity.PartReceives = "{}";
                activity.ResetTime = FormatTime(DateTime.Now);
                changed = true;
            }
            if (changed)
            {
                _ctx.Store.MarkDirty(PlayerArchive.CreatePlayerActivitysFieldNumber);
            }
        }

        // 对照 ShopService.DailyResetShop（PlayerShopService.go:12-56）
        private void ResetShop()
        {
            // 重置商品限购次数（:14-24 → ResetGoodsData：日限购商品 BuyNum=0）
            bool goodsChanged = false;
            foreach (PlayerShopGoods goods in _ctx.Store.Archive.ShopGoods)
            {
                ShopGoodsConfig goodsCfg = DataConfigManager.Tables.TbShopGoodsConfig.GetOrDefault(goods.GoodsId);
                if (goodsCfg != null && goodsCfg.LimitType == GoodsLimitType.Daily && goods.BuyNum != 0)
                {
                    goods.BuyNum = 0;
                    goodsChanged = true;
                }
            }

            // 商店刷新（:26-55）
            bool shopChanged = false;
            foreach (ShopConfig cfg in DataConfigManager.Tables.TbShopConfig.DataList)
            {
                // 对照 :48-49 其它刷新类型跳过（Go 中懒建的商店对象也不落库，此处同样不建档）
                if (cfg.RefreshType != ShopRefreshType.Daily && cfg.RefreshType != ShopRefreshType.Countdown)
                {
                    continue;
                }

                PlayerShop shop = FindShop(cfg.Id);
                if (shop == null)
                {
                    // 对照 :31-34 记录不存在时懒建
                    shop = new PlayerShop
                    {
                        Id = NextShopId(),
                        UserId = _ctx.UserId,
                        ShopId = cfg.Id,
                    };
                    _ctx.Store.Archive.Shops.Add(shop);
                }

                if (cfg.RefreshType == ShopRefreshType.Daily)
                {
                    RefreshShopGoods(cfg.Id);
                    goodsChanged = true;
                    // 每日商店需要设置折扣率（:40-42）
                    if (cfg.Type == ShopType.DailyShop)
                    {
                        shop.DiscountRate = GetRandomDiscount() * 10;
                    }
                }
                else if (CheckRefreshTime(shop.RefreshTime, cfg.RefreshValue))
                {
                    // Countdown：对照 :43-46 + shop.CheckRefreshTime（PlayerShopModel.go:51-53）
                    RefreshShopGoods(cfg.Id);
                    goodsChanged = true;
                }

                // 对照 :52-54：Daily/Countdown 两类每日重置刷新次数与刷新时间
                shop.RefreshNum = 0;
                shop.RefreshTime = FormatTime(DateTime.Now);
                shopChanged = true;
            }

            if (shopChanged)
            {
                _ctx.Store.MarkDirty(PlayerArchive.ShopsFieldNumber);
            }
            if (goodsChanged)
            {
                _ctx.Store.MarkDirty(PlayerArchive.ShopGoodsFieldNumber);
            }
        }

        // 对照 ShopService.refreshShopGoods（PlayerShopService.go:80-137）：按权重重抽商店货物
        // 偏差 1：客户端 proto PlayerShopGoods 无 CreateTime 字段，锁定商品（LockStatus>0）一律保留不过期
        // 偏差 2：Go 遍历 map 顺序随机，此处按配置表顺序（权重随机抽取语义不变）
        private void RefreshShopGoods(int shopId)
        {
            int totalWeight = 0;
            var weightGroups = new List<KeyValuePair<int, int>>();
            foreach (ShopGoodsConfig cfg in DataConfigManager.Tables.TbShopGoodsConfig.DataList)
            {
                if (cfg.ShopId == shopId)
                {
                    weightGroups.Add(new KeyValuePair<int, int>(cfg.Id, cfg.RefreshWeight));
                    totalWeight += cfg.RefreshWeight;
                }
            }

            ShopConfig shopCfg = DataConfigManager.Tables.TbShopConfig.GetOrDefault(shopId);
            int refreshCount = shopCfg.RefreshCount;
            if (refreshCount <= 0)
            {
                refreshCount = weightGroups.Count;
            }

            // 删除未锁定商品（:97-107）；锁定商品保留且占用一个刷新位
            for (int i = _ctx.Store.Archive.ShopGoods.Count - 1; i >= 0; i--)
            {
                PlayerShopGoods goods = _ctx.Store.Archive.ShopGoods[i];
                if (goods.ShopId != shopId)
                {
                    continue;
                }
                if (goods.LockStatus > 0)
                {
                    refreshCount--;
                    continue;
                }
                _ctx.Store.Archive.ShopGoods.RemoveAt(i);
            }

            // 按权重抽取新商品（:109-131）
            var pickedIds = new HashSet<int>();
            for (int i = 0; i < refreshCount; i++)
            {
                int accumulatedWeight = 1;
                int randomNum = 0;
                if (totalWeight > 0)
                {
                    accumulatedWeight = 0;
                    randomNum = _ctx.Rng.Next(totalWeight);
                }
                foreach (KeyValuePair<int, int> group in weightGroups)
                {
                    accumulatedWeight += group.Value;
                    if (randomNum < accumulatedWeight && !pickedIds.Contains(group.Key))
                    {
                        pickedIds.Add(group.Key);
                        _ctx.Store.Archive.ShopGoods.Add(new PlayerShopGoods
                        {
                            Id = NextShopGoodsId(),
                            UserId = _ctx.UserId,
                            ShopId = shopId,
                            GoodsId = group.Key,
                        });
                        break;
                    }
                }
            }
        }

        // 对照 ShopService.GetRandomDiscount（PlayerShopService.go:58-78）：按权重随机折扣率
        private int GetRandomDiscount()
        {
            List<DailyShopDiscountStruct> discountList = DataConfigManager.Tables.TbGameConst.DailyShopDiscountList;
            if (discountList.Count == 0)
            {
                return 50;
            }

            int total = 0;
            foreach (DailyShopDiscountStruct d in discountList)
            {
                total += d.Percent;
            }
            int randNum = _ctx.Rng.Next(total);
            int cum = 0;
            foreach (DailyShopDiscountStruct d in discountList)
            {
                cum += d.Percent;
                if (randNum < cum)
                {
                    return d.Discount;
                }
            }
            return discountList[0].Discount;
        }

        // 对照 BattleService.DailyResetData（PlayerBattleService.go:9-20）
        // 偏差：客户端 proto PlayerBattle 无 UpdateTime/CreateTime 字段，跳过时间更新
        private void ResetBattle()
        {
            PlayerBattle battle = _ctx.Store.Archive.Battle;
            if (battle == null)
            {
                return; // 对照 :11-13 记录不存在直接返回
            }
            battle.DailyDoubleRewardNum = 0;
            battle.DailyStageSweepingNum = 0;
            _ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);
        }

        // 对照 ChallengeService.DailyResetChallenge（PlayerChallengeService.go:8-18）
        private void ResetChallenge()
        {
            if (_ctx.Store.Archive.Challenge == null)
            {
                // 对照 :10-12 记录不存在时懒建
                _ctx.Store.Archive.Challenge = new PlayerChallenge { Id = _ctx.UserId };
            }
            _ctx.Store.Archive.Challenge.DailyGemFusionNum = 0;
            _ctx.Store.Archive.Challenge.GemFusionRewards = "[]";
            _ctx.Store.MarkDirty(PlayerArchive.ChallengeFieldNumber);
        }

        // 对照 EndlessTowerService.DailyResetEndlessTower（PlayerEndlessTowerService.go:54-69）
        // 偏差：客户端 proto PlayerEndlessTower 无 UpdateTime 字段，每周重置判断（IsNewWeek）跳过
        private void ResetEndlessTower()
        {
            foreach (PlayerEndlessTower tower in _ctx.Store.Archive.EndlessTowers)
            {
                tower.DailyRewards = "[]";
                tower.DailyChallengeNum = 0;
            }
            _ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);
        }

        // 对照 ChallengeBossService.DailyResetChallengeBoss（PlayerChallengeBossService.go:50-69）
        // 偏差：客户端 proto PlayerChallengeBoss 无 UpdateTime 字段，每周重置段（:56-64 清 TotalScore/TotalDamage/
        // ScoreRewards/TotalChallengeNum）无法实现，跳过
        private void ResetChallengeBoss()
        {
            foreach (PlayerChallengeBoss boss in _ctx.Store.Archive.ChallengeBoss)
            {
                boss.DailyRewards = "[]";
                boss.DailyChallengeNum = 0;
            }
            _ctx.Store.MarkDirty(PlayerArchive.ChallengeBossFieldNumber);
        }

        // 对照 HiddenRealmTowerService.DailyResetHiddenRealmTower（PlayerHiddenRealmTowerService.go:47-71）
        // 偏差：Go 端层数据是 JSON 字符串，客户端 proto 已是 repeated HiddenRealmFloor，直接改写；
        // 每周重置段同样因无 UpdateTime 跳过
        private void ResetHiddenRealmTower()
        {
            if (_ctx.Store.Archive.HiddenRealmTowers.Count == 0)
            {
                return;
            }
            PlayerHiddenRealmTower tower = _ctx.Store.Archive.HiddenRealmTowers[0];
            tower.DailyRewards = "[]";
            tower.DailyChallengeNum = 0;
            tower.DailyStageSweepingNum = 0;

            // 刷新未通关层的词条（:58-61 refreshDailyEntries:235-251）；已通关层冻结不动
            foreach (HiddenRealmFloor floor in tower.Floors)
            {
                if (floor.Cleared)
                {
                    continue;
                }
                HiddenRealmTowerConfig floorCfg = DataConfigManager.Tables.TbHiddenRealmTowerConfig.GetOrDefault(floor.Floor);
                if (floorCfg == null)
                {
                    continue;
                }
                floor.Entries.Clear();
                GenFloorEntries(floorCfg, floor.Entries);
            }

            _ctx.Store.MarkDirty(PlayerArchive.HiddenRealmTowersFieldNumber);
        }

        // 对照 GenFloorEntries（PlayerHiddenRealmTowerService.go:310-335）：
        // count=MonsterEntryNum，类型互不相同（从配置类型池取），level=MonsterEntryLevel（固定值）
        private void GenFloorEntries(HiddenRealmTowerConfig floorCfg, Google.Protobuf.Collections.RepeatedField<HiddenRealmFloorEntry> entries)
        {
            if (floorCfg.MonsterEntryNum <= 0)
            {
                return;
            }
            // 对照 monsterEntryTypePool（:337-347）：排除 0=ALL
            var pool = new List<int>();
            foreach (MonsterEntryConfig cfg in DataConfigManager.Tables.TbMonsterEntryConfig.DataList)
            {
                if ((int)cfg.MonsterEntryType != 0)
                {
                    pool.Add((int)cfg.MonsterEntryType);
                }
            }
            if (pool.Count == 0)
            {
                return;
            }

            var used = new HashSet<int>();
            for (int i = 0; i < floorCfg.MonsterEntryNum; i++)
            {
                // 对照 pickEntryType（:351-365）：从池中未用类型随机取，保证不重复
                var candidates = new List<int>();
                foreach (int t in pool)
                {
                    if (!used.Contains(t))
                    {
                        candidates.Add(t);
                    }
                }
                if (candidates.Count == 0)
                {
                    break;
                }
                int type = candidates[_ctx.Rng.Next(candidates.Count)];
                used.Add(type);
                entries.Add(new HiddenRealmFloorEntry { EntryType = type, Level = floorCfg.MonsterEntryLevel });
            }
        }

        private PlayerShop FindShop(int shopId)
        {
            foreach (PlayerShop shop in _ctx.Store.Archive.Shops)
            {
                if (shop.ShopId == shopId)
                {
                    return shop;
                }
            }
            return null;
        }

        // 对照 PlayerShopModel.CheckRefreshTime（:51-53）→ utils.CheckBeginAndEndDiffSecond：now-refreshTime >= refreshValue 秒
        // 刷新时间为空/解析失败等同 Go 零值时间：必然过期
        private bool CheckRefreshTime(string refreshTime, int refreshValue)
        {
            if (string.IsNullOrEmpty(refreshTime) ||
                !DateTimeOffset.TryParse(refreshTime, out DateTimeOffset last))
            {
                return true;
            }
            return DateTimeOffset.Now.ToUnixTimeSeconds() - last.ToUnixTimeSeconds() >= refreshValue;
        }

        // proto Id（原 MySQL 自增主键）在集合内递增分配，同 NewPlayerInitializer 口径
        private int NextShopId()
        {
            int maxId = 0;
            foreach (PlayerShop shop in _ctx.Store.Archive.Shops)
            {
                if (shop.Id > maxId)
                {
                    maxId = shop.Id;
                }
            }
            return maxId + 1;
        }

        private int NextShopGoodsId()
        {
            int maxId = 0;
            foreach (PlayerShopGoods goods in _ctx.Store.Archive.ShopGoods)
            {
                if (goods.Id > maxId)
                {
                    maxId = goods.Id;
                }
            }
            return maxId + 1;
        }

        // 时间格式同 NewPlayerInitializer：RFC3339，客户端 mapper 用 DateTime.TryParse 解析
        private string FormatTime(DateTime t)
        {
            return t.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
    }
}
