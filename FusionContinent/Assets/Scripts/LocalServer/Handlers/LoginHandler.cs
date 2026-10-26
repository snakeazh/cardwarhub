using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 登录 Handler。对照 user.go LoginHandler（:67-239）单机化：
    /// 只保留「账号不存在则建档」语义——本地账号唯一，有档读档、无档按 req.Account 建档，不校验密码。
    /// </summary>
    public class LoginHandler
    {
        public LoginResponse Login(LocalServerContext ctx, LoginRequest req)
        {
            bool isCreateUser = false;
            if (!ctx.LoadArchive())
            {
                // 对照 user.go:131-141 + application_player_archive.go:148-150
                new NewPlayerInitializer(ctx).CreateNew(req.Account);
                isCreateUser = true;
            }

            // 每日重置（对照 user.go:181，必须在更新 LoginTime 之前用旧登录时间判定）
            new DailyResetRunner(ctx).Run(isCreateUser);

            // 老档迁移（产品改动，无 Go 对照）：技能等级跟随英灵等级，登录时对所有已拥有英灵向上补齐技能卡等级。
            // 幂等，每次登录都跑；有变更则刷技能模块战力
            if (new CardHandler().SyncAllSoldierSkillLevels(ctx))
            {
                ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSkill);
            }

            // 任务准备（对照 application_player_archive.go:256-259 GetTaskAll：日刷新懒触发 + 主线/每日懒补齐，
            // 登录响应带全量 Archive，客户端自动拿到 tasks/task_actives）
            ctx.TaskEngine.EnsureTasksForLogin();

            // 对照 application_player_archive.go:177-187：登录时把未完成的战斗重置为 Idle（中途退出等异常恢复）
            if (ctx.Store.Archive.Battle != null && ctx.Store.Archive.Battle.Status != 0)
            {
                ctx.Store.Archive.Battle.Status = 0; // battleModel.BattleStatusIdle
                ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);
            }

            // 对照 application_player_archive.go:337-341：引导关有记录且未通关（Condition1==Normal）则清空模块解锁
            int guideStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
            PlayerStage guideStage = null;
            foreach (PlayerStage s in ctx.Store.Archive.Stages)
            {
                if (s.StageId == guideStageId)
                {
                    guideStage = s;
                    break;
                }
            }
            if (guideStage != null && guideStage.Condition1 == 0 && ctx.Store.Archive.Modules.Count > 0)
            {
                ctx.Store.Archive.Modules.Clear();
                ctx.Store.MarkDirty(PlayerArchive.ModulesFieldNumber);
            }

            // 对照 user.go:183-186 更新登录时间
            ctx.Store.User.LoginTime = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            ctx.Store.MarkUserDirty();

            // 对照 user.go:202-204 + TicketService.LazyUpdateTicketOnLogin（PlayerTicketService.go:9-26）：
            // 登录时懒更新门票——配置里有而存档没有的补齐（立即可领取），已过期（NextClaimTime 非 0 且早于现在）的重置为可领取
            long nowUnix = System.DateTimeOffset.Now.ToUnixTimeSeconds();
            bool ticketsChanged = false;
            foreach (TicketConfig ticketCfg in DataConfigManager.Tables.TbTicketConfig.DataList)
            {
                PlayerTicket ticket = null;
                foreach (PlayerTicket t in ctx.Store.Archive.Tickets)
                {
                    if (t.TicketId == ticketCfg.Id)
                    {
                        ticket = t;
                        break;
                    }
                }
                if (ticket == null)
                {
                    ctx.Store.Archive.Tickets.Add(new PlayerTicket { TicketId = ticketCfg.Id });
                    ticketsChanged = true;
                }
                else if (ticket.NextClaimTime != 0 && nowUnix > ticket.NextClaimTime) // 对照 TicketModel.CheckNeedReset
                {
                    ticket.NextClaimTime = 0;
                    ticketsChanged = true;
                }
            }
            if (ticketsChanged)
            {
                ctx.Store.MarkDirty(PlayerArchive.TicketsFieldNumber);
            }

            // 对照 user.go:207-209：新档或战力尚未计算（上次登录中途失败）时全量刷新战力
            if (isCreateUser || ctx.Store.User.CombatPower == 0)
            {
                ctx.CombatPower.RefreshAll();
            }

            LogTool.Log($"[LoginHandler] 本地登录成功 userId={ctx.Store.User.UserId} account={ctx.Store.User.Account}");
            return new LoginResponse
            {
                Token = "local",
                User = ctx.Store.User,
                Archive = ctx.Store.Archive,
                GatewayAddress = "", // 单机无网关
            };
        }
    }
}
