using System;
using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 门票 Handler，逐行对照 Server/app/api/application/ticketapplication/application_ticket.go
    /// + Server/app/model/TicketModel/PlayerTicketModel.go。
    /// proto PlayerTicket.NextClaimTime 为 Unix 秒，0 = 立即可领取（对照 Go ClaimableSentinel=epoch 0）。
    /// </summary>
    public class TicketHandler
    {
        // 对照 application_ticket.go:19-56 ClaimTicketLogic
        public ClaimTicketResponse Claim(LocalServerContext ctx, ClaimTicketRequest req)
        {
            // 对照 :20-24
            TicketConfig cfg = DataConfigManager.Tables.TbTicketConfig.GetOrDefault(req.TicketId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 门票配置不存在 ticketId={req.TicketId}");
                throw new LocalServerException("参数错误");
            }

            // 对照 :26-30 TicketModel.GetByUserIdAndTicketId：查不到（Go Id==0） panic 参数错误
            PlayerTicket ticket = FindTicket(ctx, req.TicketId);
            if (ticket == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 门票数据不存在 ticketId={req.TicketId}");
                throw new LocalServerException("参数错误");
            }

            long nowUnix = DateTimeOffset.Now.ToUnixTimeSeconds();

            // 对照 :33-35 CheckNeedReset（PlayerTicketModel.go:45-48）：已过可领取时间则重置为立即可领取
            if (ticket.NextClaimTime != 0 && nowUnix > ticket.NextClaimTime)
            {
                ticket.NextClaimTime = 0;
            }

            // 对照 :37-40 IsClaimable（PlayerTicketModel.go:51-54）
            if (ticket.NextClaimTime != 0 && nowUnix <= ticket.NextClaimTime)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 门票已领取 ticketId={req.TicketId}");
                throw new LocalServerException("已领取");
            }

            // 对照 :42-53：事务内统一道具入口 + 计算下次领取时间
            Dictionary<int, long> items = ctx.ItemEntrance.RewardsToItems(cfg.DailyRefreshItem);
            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(items);
                ticket.NextClaimTime = CalcNextClaimTime();
            }
            catch (Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 领取门票失败 ticketId={req.TicketId}: {e.Message}");
                throw new LocalServerException("领取失败");
            }
            ctx.Store.MarkDirty(PlayerArchive.TicketsFieldNumber);

            return new ClaimTicketResponse { Ticket = ticket, Items = rsItems };
        }

        // 对照 TicketModel.GetByUserIdAndTicketId：查不到返回 null（Go 返回零值 struct，调用方按 Id==0 处理）
        private PlayerTicket FindTicket(LocalServerContext ctx, int ticketId)
        {
            foreach (PlayerTicket ticket in ctx.Store.Archive.Tickets)
            {
                if (ticket.TicketId == ticketId)
                {
                    return ticket;
                }
            }
            return null;
        }

        // 对照 TicketModel.CalcNextClaimTime（PlayerTicketModel.go:39-48）：
        // 今日每日刷新点（DailyServerRefreshTime 点）；已过则取明日刷新点
        private long CalcNextClaimTime()
        {
            int refreshHour = DataConfigManager.Tables.TbGameConst.DailyServerRefreshTime;
            DateTime now = DateTime.Now;
            var todayRefresh = new DateTime(now.Year, now.Month, now.Day, refreshHour, 0, 0, now.Kind);
            if (now < todayRefresh)
            {
                return new DateTimeOffset(todayRefresh).ToUnixTimeSeconds();
            }
            return new DateTimeOffset(todayRefresh.AddDays(1)).ToUnixTimeSeconds();
        }
    }
}
