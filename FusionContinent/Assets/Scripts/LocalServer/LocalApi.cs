using System;
using System.Collections.Generic;
using Cardwar;
using Google.Protobuf;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 本地 API 分发：替代原 HTTP 服务端，是 WebDataFun.SendProtoPostRequest 的唯一出口。
    /// 业务 Handler 抛 LocalServerException（= Go 端 panic）→ Code=500 + 原文案；
    /// 未注册路径同样返回 Code=500「接口未实现」，客户端按原逻辑 Toast。
    /// Handler 执行前对存档做快照，任何失败整体回滚（等价 Go 的 MySQL 事务回滚）。
    /// 意外异常回滚后直接上抛（fail fast）。
    /// </summary>
    public class LocalApi
    {
        private readonly Dictionary<string, Func<LocalServerContext, IMessage, IMessage>> _routes =
            new Dictionary<string, Func<LocalServerContext, IMessage, IMessage>>();

        public LocalApi()
        {
            var loginHandler = new LoginHandler();
            var talentHandler = new TalentHandler();
            var battleHandler = new BattleHandler();
            var cardHandler = new CardHandler();
            var teamHandler = new TeamHandler();
            var equipHandler = new EquipHandler();
            var fusionGemHandler = new FusionGemHandler();
            var cityWallHandler = new CityWallHandler();
            var endlessTowerHandler = new EndlessTowerHandler();
            var challengeBossHandler = new ChallengeBossHandler();
            var fusionGemGameHandler = new FusionGemGameHandler();
            var cycleSpeakerHandler = new CycleSpeakerHandler();
            var shopHandler = new ShopHandler();
            var staminaHandler = new StaminaHandler();
            var ticketHandler = new TicketHandler();
            var atlasHandler = new AtlasHandler();
            var moduleHandler = new ModuleHandler();
            var userHandler = new UserHandler();
            var playerHandler = new PlayerHandler();
            var gmHandler = new GmHandler();
            var taskHandler = new TaskHandler();

            // 登录 / 建档
            _routes[ApiKeys.ApiLogin] = (ctx, req) => loginHandler.Login(ctx, (LoginRequest)req);

            // 天赋
            _routes[ApiKeys.ApiTalentUse] = (ctx, req) => talentHandler.UseTalent(ctx, (UseTalentRequest)req);
            _routes[ApiKeys.ApiUpGradeTalent] = (ctx, req) => talentHandler.UpGradeTalent(ctx, (UpGradeTalentRequest)req);

            // 战斗
            _routes[ApiKeys.ApiBattleStart] = (ctx, req) => battleHandler.Start(ctx, (StartBattleRequest)req);
            _routes[ApiKeys.ApiBattleSettle] = (ctx, req) => battleHandler.Settle(ctx, (SettleBattleRequest)req);
            _routes[ApiKeys.ApiBattleExit] = (ctx, req) => battleHandler.Exit(ctx);
            _routes[ApiKeys.ApiBattleDoubleReward] = (ctx, req) => battleHandler.DoubleReward(ctx, (SettleBattleRequest)req);
            _routes[ApiKeys.ApiBattleStageSweeping] = (ctx, req) => battleHandler.StageSweeping(ctx, (StageSweepingRequest)req);
            _routes[ApiKeys.ApiBattleChapterAfkRewards] = (ctx, req) => battleHandler.ChapterAfkRewards(ctx,
                req != null ? (ChapterAfkRewardsRequest)req : new ChapterAfkRewardsRequest()); // 客户端发 null 请求体
            _routes[ApiKeys.ApiBattleStageFirstRewards] = (ctx, req) => battleHandler.StageReceiveRewards(ctx, (StageReceiveRewardsRequest)req);

            // 卡牌
            _routes[ApiKeys.ApiCardLevelUp] = (ctx, req) => cardHandler.LevelUpCard(ctx, (LevelUpCardRequest)req);
            _routes[ApiKeys.ApiCardSetSkin] = (ctx, req) => cardHandler.SetSkin(ctx, (SetSkinRequest)req);
            _routes[ApiKeys.ApiCardReceiveDevelopReward] = (ctx, req) => cardHandler.ReceiveDevelopReward(ctx, (ReceiveDevelopRewardRequest)req);
            _routes[ApiKeys.ApiCardDecompositionCard] = (ctx, req) => cardHandler.DecompositionCard(ctx, (AsCardFragmentRequest)req);
            _routes[ApiKeys.ApiCardCompositionCard] = (ctx, req) => cardHandler.CompositionCard(ctx, (AsCardFusionRequest)req);
            _routes[ApiKeys.ApiCardSoliderCardActive] = (ctx, req) => cardHandler.SoliderCardActive(ctx, (SoliderCardActiveRequest)req);
            _routes[ApiKeys.ApiCardUnlockSkill] = (ctx, req) => cardHandler.UnlockSkillCard(ctx, (UnlockSkillCardRequest)req);

            // 编队（奥术卡已去佩戴化，saveAsCardTeam 不迁移）
            _routes[ApiKeys.ApiTeamSaveTeams] = (ctx, req) => teamHandler.SaveTeams(ctx, (SaveTeamsRequest)req);
            _routes[ApiKeys.ApiTeamSaveHeroTeams] = (ctx, req) => teamHandler.SaveHeroTeam(ctx, (SaveHeroTeamRequest)req);

            // 装备 / 装备槽 / 融合石 / 城墙
            _routes[ApiKeys.ApiEquipWear] = (ctx, req) => equipHandler.WearEquip(ctx, (WearEquipRequest)req);
            _routes[ApiKeys.ApiEquipUpPhase] = (ctx, req) => equipHandler.UpPhaseEquip(ctx, (UpPhaseEquipRequest)req);
            _routes[ApiKeys.ApiEquipFragment] = (ctx, req) => equipHandler.FragmentEquip(ctx, (FragmentEquipRequest)req);
            _routes[ApiKeys.ApiEquipRefreshEntry] = (ctx, req) => equipHandler.RefreshEntryEquip(ctx, (RefreshEntryEquipRequest)req);
            _routes[ApiKeys.ApiEquipUseRefreshEntry] = (ctx, req) => equipHandler.UseRefreshEntryEquip(ctx, (UseRefreshEntryEquipRequest)req);
            _routes[ApiKeys.ApiEquipSlotUpgrade] = (ctx, req) => equipHandler.UpgradeEquipSlot(ctx, (EquipSlotUpgradeRequest)req);
            _routes[ApiKeys.ApiFusionGemWear] = (ctx, req) => fusionGemHandler.WearFusionGemV2(ctx, (WearFusionGemV2Request)req);
            _routes[ApiKeys.ApiFusionGemCompound] = (ctx, req) => fusionGemHandler.FusionGemCompound(ctx, (FusionGemCompoundRequest)req);
            _routes[ApiKeys.ApiCityWallUpgrade] = (ctx, req) => cityWallHandler.UpgradeCityWall(ctx, (CityWallUpgradeRequest)req);
            _routes[ApiKeys.ApiCityWallSkillActivate] = (ctx, req) => cityWallHandler.ActivateCityWallSkill(ctx, (CityWallSkillActivateRequest)req);
            _routes[ApiKeys.ApiCityWallSkillUpgrade] = (ctx, req) => cityWallHandler.UpgradeCityWallSkill(ctx, (CityWallSkillUpgradeRequest)req);
            _routes[ApiKeys.ApiCityWallSave] = (ctx, req) => cityWallHandler.SaveCityWall(ctx, (CityWallSaveRequest)req);

            // 无尽塔 / 挑战Boss / 融合石小游戏 / 轮语者
            _routes[ApiKeys.ApiEndlessTowerNextFloor] = (ctx, req) => endlessTowerHandler.NextFloor(ctx, (NextFloorRequest)req);
            _routes[ApiKeys.ApiEndlessTowerReceiveDailyReward] = (ctx, req) => endlessTowerHandler.ReceiveDailyReward(ctx, (ReceiveDailyRewardRequest)req);
            _routes[ApiKeys.ApiEndlessTowerReceiveChestReward] = (ctx, req) => endlessTowerHandler.ReceiveChestReward(ctx, (ReceiveTowerChestRewardRequest)req);
            _routes[ApiKeys.ApiChallengeBossReceiveDailyReward] = (ctx, req) => challengeBossHandler.ReceiveDailyReward(ctx, (ReceiveChallengeBossDailyRewardRequest)req);
            _routes[ApiKeys.ApiChallengeBossReceiveScoreReward] = (ctx, req) => challengeBossHandler.ReceiveScoreReward(ctx, (ReceiveChallengeBossScoreRewardRequest)req);
            _routes[ApiKeys.ApiStartFusionGemGame] = (ctx, req) => fusionGemGameHandler.StartFusionGemGame(ctx, (StartFusionGemGameRequest)req);
            _routes[ApiKeys.ApiSettleFusionGemGame] = (ctx, req) => fusionGemGameHandler.SettleFusionGemGame(ctx, (SettleFusionGemGameRequest)req);
            _routes[ApiKeys.ApiSpeakerSetSkin] = (ctx, req) => cycleSpeakerHandler.SetSkin(ctx, (SetCycleSpeakerSkinRequest)req);
            _routes[ApiKeys.ApiSpeakerSkinActivate] = (ctx, req) => cycleSpeakerHandler.SkinActivate(ctx, (SkinActivateRequest)req);
            _routes[ApiKeys.ApiSpeakerSkinUpgrade] = (ctx, req) => cycleSpeakerHandler.SkinUpgrade(ctx, (SkinUpgradeRequest)req);
            _routes[ApiKeys.ApiSpeakerSkinUnequip] = (ctx, req) => cycleSpeakerHandler.SkinUnequip(ctx, (SkinUnequipRequest)req);
            _routes[ApiKeys.ApiSpeakerWeaponActivate] = (ctx, req) => cycleSpeakerHandler.WeaponActivate(ctx, (WeaponActivateRequest)req);
            _routes[ApiKeys.ApiSpeakerWeaponUpgrade] = (ctx, req) => cycleSpeakerHandler.WeaponUpgrade(ctx, (WeaponUpgradeRequest)req);
            _routes[ApiKeys.ApiSpeakerWeaponEquip] = (ctx, req) => cycleSpeakerHandler.WeaponEquip(ctx, (WeaponEquipRequest)req);
            _routes[ApiKeys.ApiSpeakerWeaponUnequip] = (ctx, req) => cycleSpeakerHandler.WeaponUnequip(ctx, (WeaponUnequipRequest)req);

            // 商店 / 体力驿站 / 门票 / 图鉴 / 模块
            _routes[ApiKeys.ApiBuyGoods] = (ctx, req) => shopHandler.BuyGoods(ctx, (BuyGoodsRequest)req);
            _routes[ApiKeys.ApiStaminaInfo] = (ctx, req) => staminaHandler.Info(ctx,
                req != null ? (GetStationStaminaInfoRequest)req : new GetStationStaminaInfoRequest()); // 客户端发 null 请求体
            _routes[ApiKeys.ApiStaminaClaim] = (ctx, req) => staminaHandler.Claim(ctx,
                req != null ? (ClaimStaminaRequest)req : new ClaimStaminaRequest()); // 客户端发 null 请求体
            _routes[ApiKeys.ApiTicketClaim] = (ctx, req) => ticketHandler.Claim(ctx, (ClaimTicketRequest)req);
            _routes[ApiKeys.ApiAtlasReceiveMonsterReward] = (ctx, req) => atlasHandler.ReceiveMonsterReward(ctx, (ReceiveMonsterRewardRequest)req);
            _routes[ApiKeys.ApiModuleSaveStatus] = (ctx, req) => moduleHandler.SaveStatus(ctx, (SaveModuleStatusRequest)req);

            // 用户 / 玩家
            _routes[ApiKeys.ApiUserUpdateAvatar] = (ctx, req) => userHandler.UpdateAvatar(ctx, (UpdateAvatarRequest)req);
            _routes[ApiKeys.ApiUserUpdateAvatarFrame] = (ctx, req) => userHandler.UpdateAvatarFrame(ctx, (UpdateAvatarFrameRequest)req);
            _routes[ApiKeys.ApiUserAddGuides] = (ctx, req) => userHandler.AddGuides(ctx, (SaveGuidesRequest)req);
            _routes[ApiKeys.ApiPlayerSaveGuides] = (ctx, req) => playerHandler.SaveGuides(ctx, (SaveGuidesRequest)req);
            _routes[ApiKeys.ApiPlayerSavePlayer] = (ctx, req) => playerHandler.SavePlayer(ctx, (SavePlayerRequest)req);
            _routes[ApiKeys.ApiPlayerSaveModuleLv] = (ctx, req) => playerHandler.SaveModuleUnlockLv(ctx, (SaveModuleUnlockLvRequest)req);

            // GM 工具
            _routes[ApiKeys.ApiAddItems] = (ctx, req) => gmHandler.AddItems(ctx, (AddItemsRequest)req);
            _routes[ApiKeys.ApiAddItemsAll] = (ctx, req) => gmHandler.AddItemsAll(ctx, (AddItemsAllRequest)req);

            // 任务
            _routes[ApiKeys.ApiReceiveTaskReward] = (ctx, req) => taskHandler.ReceiveTaskReward(ctx, (ReceiveTaskRewardRequest)req);
            _routes[ApiKeys.ApiReceiveTaskActiveReward] = (ctx, req) => taskHandler.ReceiveTaskActiveReward(ctx, (ReceiveTaskActiveRewardRequest)req);

            // 不迁移：排行/好友/聊天/邮件/PvP/兑换码/充值活动（入口已移除）、抽卡 chestCard 与 drawArcCard（抽奖模块已整体移除）、
            // api/team/saveAsCardTeam（去佩戴化废弃）。
            // 未注册路径由 Dispatch 返回「接口未实现」。
        }

        public GameResponse Dispatch(string url, IMessage request)
        {
            EnsureContext();

            if (!_routes.TryGetValue(url, out var handler))
            {
                LogTool.LogWarning($"[LocalApi] 接口未实现: {url}");
                return new GameResponse { Code = 500, Msg = $"接口未实现: {url}", Time = NowUnix() };
            }

            LocalServerContext ctx = Global.LocalServer;
            if (url != ApiKeys.ApiLogin && !ctx.Store.Loaded)
            {
                LogTool.LogWarning($"[LocalApi] 存档未加载，拒绝请求: {url}");
                return new GameResponse { Code = 500, Msg = "登录状态失效，请重新登录", Time = NowUnix() };
            }

            // 事务等价物：仅对已加载存档做快照（login 建档流程除外）
            bool takeSnapshot = ctx.Store.Loaded;
            var snapshot = takeSnapshot ? ctx.Store.Snapshot() : default;

            try
            {
                IMessage data = handler(ctx, request);
                ctx.Store.Flush();
                return new GameResponse
                {
                    Code = 200,
                    Msg = "success",
                    Time = NowUnix(),
                    Data = data != null ? ByteString.CopyFrom(data.ToByteArray()) : ByteString.Empty,
                };
            }
            catch (LocalServerException ex)
            {
                // 业务校验失败 = Go 端 panic：回滚内存存档，Msg 逐字复刻原文案
                if (takeSnapshot)
                {
                    ctx.Store.Restore(snapshot);
                    ctx.TaskEngine.ClearPendingChanges();
                }
                LogTool.LogWarning($"[LocalApi] {url} 业务失败: {ex.Message}");
                return new GameResponse { Code = 500, Msg = ex.Message, Time = NowUnix() };
            }
            catch (Exception)
            {
                // 意外异常：回滚后上抛（fail fast，不掩盖 bug）
                if (takeSnapshot)
                {
                    ctx.Store.Restore(snapshot);
                    ctx.TaskEngine.ClearPendingChanges();
                }
                throw;
            }
        }

        private void EnsureContext()
        {
            if (Global.LocalServer == null)
            {
                Global.LocalServer = new LocalServerContext();
            }
        }

        private long NowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
