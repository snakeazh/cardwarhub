using System;
using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 体力驿站 Handler，逐行对照 Server/app/api/application/staminaapplication/application_stamina.go
    /// + Server/app/service/StaminaService/PlayerStaminaService.go。
    /// Go 的 LastClaimTime 存 player_stamina 表；单机版并入 PlayerResource.StationLastClaimTime（proto 新增字段），随存档持久化。
    /// 注意：api/stamina/info 与 api/stamina/claim 客户端请求均为 null，req 参数不参与逻辑。
    /// </summary>
    public class StaminaHandler
    {
        // 驿站奖励槽（对照 StaminaService.SlotInfo，带生成时间用于排序截断）
        private class TimedSlot
        {
            public DateTime GenTime;
            public int ItemId;
            public long Num;
        }

        // 对照 application_stamina.go:16-19 GetStationStaminaInfoLogic
        public GetStationStaminaInfoResponse Info(LocalServerContext ctx, GetStationStaminaInfoRequest req)
        {
            DateTime lastClaimTime = GetOrCreateLastClaimTime(ctx);
            return new GetStationStaminaInfoResponse
            {
                Stamina = BuildStationStamina(lastClaimTime, DateTime.Now),
            };
        }

        // 对照 application_stamina.go:28-60 ClaimStaminaLogic
        public ClaimStaminaResponse Claim(LocalServerContext ctx, ClaimStaminaRequest req)
        {
            DateTime now = DateTime.Now;
            DateTime lastClaimTime = GetOrCreateLastClaimTime(ctx);

            StationStamina stamina = BuildStationStamina(lastClaimTime, now);
            if (stamina.Slots.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 暂无可领取的体力");
                throw new LocalServerException("暂无可领取的体力");
            }

            // 对照 :39-42 合并槽位道具
            var items = new Dictionary<int, long>();
            foreach (StationStaminaSlot slot in stamina.Slots)
            {
                if (!items.ContainsKey(slot.ItemId))
                {
                    items[slot.ItemId] = 0;
                }
                items[slot.ItemId] += slot.Num;
            }

            // 对照 :44-54：事务内统一道具入口 + 更新领取时间。
            // Go 的 err!=nil 分支不可达（业务失败是 panic，回滚后 re-panic 原文案），本地直接上抛原文案
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);
            ctx.Store.Archive.Resource.StationLastClaimTime = new DateTimeOffset(now).ToUnixTimeSeconds();
            ctx.Store.MarkDirty(PlayerArchive.ResourceFieldNumber);

            // 对照 :56-59：领取后重建（此刻无新槽位）
            return new ClaimStaminaResponse
            {
                Stamina = BuildStationStamina(now, now),
                Items = rsItems,
            };
        }

        // 对照 StaminaService.GetOrCreate（PlayerStaminaService.go:21-25）：无记录按 now 建档
        private DateTime GetOrCreateLastClaimTime(LocalServerContext ctx)
        {
            // 旧存档可能缺 Resource 块，按 Go GetResource 语义懒建
            if (ctx.Store.Archive.Resource == null)
            {
                ctx.Store.Archive.Resource = new PlayerResource { Id = ctx.UserId };
            }

            long ts = ctx.Store.Archive.Resource.StationLastClaimTime;
            if (ts == 0)
            {
                DateTime now = DateTime.Now;
                ctx.Store.Archive.Resource.StationLastClaimTime = new DateTimeOffset(now).ToUnixTimeSeconds();
                ctx.Store.MarkDirty(PlayerArchive.ResourceFieldNumber);
                return now;
            }
            return DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
        }

        // 对照 StaminaService.BuildStationStamina（PlayerStaminaService.go:27-41）
        private StationStamina BuildStationStamina(DateTime lastClaimTime, DateTime now)
        {
            TbGameConst constCfg = DataConfigManager.Tables.TbGameConst;
            List<int> hours = constCfg.PostStationDailyTakeTime;
            // RewardAndNum 需限定 Config 命名空间（Cardwar 下也有同名 proto 类）
            Config.RewardAndNum reward = constCfg.PostStationPhysicalStrengthNum;
            int maxSlots = constCfg.PostStationMaxPhysicalStrengthNum;

            var info = new StationStamina { MaxSlots = maxSlots };
            if (reward == null || hours.Count == 0)
            {
                return info;
            }

            foreach (TimedSlot slot in CalcAccumulatedSlots(hours, lastClaimTime, now, reward.Id, reward.Num, maxSlots))
            {
                info.Slots.Add(new StationStaminaSlot { ItemId = slot.ItemId, Num = slot.Num });
            }
            return info;
        }

        // 对照 StaminaService.calcAccumulatedSlots（PlayerStaminaService.go:43-75）：
        // 每个每日整点从最近 occurrence 起按 24h 回推，凡晚于 lastClaimTime 的各记一个槽位；
        // 按生成时间倒序后截断到 maxSlots
        private List<TimedSlot> CalcAccumulatedSlots(List<int> hours, DateTime lastClaimTime, DateTime now, int itemId, long num, int maxSlots)
        {
            var all = new List<TimedSlot>();
            foreach (int hour in hours)
            {
                DateTime t = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, now.Kind);
                if (now < t)
                {
                    t = t.AddDays(-1);
                }
                while (t > lastClaimTime)
                {
                    all.Add(new TimedSlot { GenTime = t, ItemId = itemId, Num = num });
                    t = t.AddDays(-1);
                }
            }

            // 对照 sort.Slice 倒序
            all.Sort((a, b) => b.GenTime.CompareTo(a.GenTime));
            if (all.Count > maxSlots)
            {
                all.RemoveRange(maxSlots, all.Count - maxSlots);
            }
            return all;
        }
    }
}
