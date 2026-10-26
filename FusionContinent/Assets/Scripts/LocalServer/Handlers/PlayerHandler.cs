using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 玩家信息 Handler，逐行对照 Server/app/api/application/playerapplication/application_player.go。
    /// 存档读写对照 Server/app/model/UserModel/UserPlayerModel.go + userModel.go（内存 Archive.Player / User 替代 MySQL）。
    /// </summary>
    public class PlayerHandler
    {
        // 对照 UserModel（UserPlayerModel.go:14-19）引导流程状态
        private const int GuideFlowReady = 0;
        private const int GuideFlowReceive = 1;
        private const int GuideFlowFinished = 2;

        // 对照 application_player.go:33-128 SavePlayer
        public SavePlayerResponse SavePlayer(LocalServerContext ctx, SavePlayerRequest req)
        {
            // 对照 :34-38 敏感词检查（utils/SensitiveWords.go:17-26 SensitiveWordsMatch）
            if (req.UserName != "" && SensitiveWordsMatch(req.UserName))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 名称存在敏感词");
                throw new LocalServerException("名称存在敏感词");
            }

            // 对照 :41-47 解析形象 JSON
            var figures = new List<int>();
            if (req.Figures != "")
            {
                try
                {
                    figures = JsonConvert.DeserializeObject<List<int>>(req.Figures);
                }
                catch (System.Exception e)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} json转换失败: {e.Message}");
                    throw new LocalServerException("json转换失败: " + e.Message);
                }
            }

            // 对照 :49-53 GetPlayerById：玩家记录不存在（Go Id==0）panic
            UserPlayer player = ctx.Store.Archive.Player;
            if (player == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} SYS_ERR_PRARMETER");
                throw new LocalServerException("SYS_ERR_PRARMETER");
            }

            User user = ctx.Store.User;

            // 对照 :58-77 设置玩家形象
            if (req.Figures != "")
            {
                foreach (int figureId in figures)
                {
                    PlayerFigureConfig figureCfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(figureId);
                    if (figureCfg == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 形象Id[{figureId}]，配置不存在");
                        throw new LocalServerException($"形象Id[{figureId}]，配置不存在");
                    }
                }

                player.Figure = JsonConvert.SerializeObject(figures);
                SetGuidFlow(player, GetGuideIdByType(GuideType.SetFigure), GuideFlowFinished);
                ctx.Store.MarkDirty(PlayerArchive.PlayerFieldNumber);
            }

            // 对照 :80-114 设置名称 & 性别
            bool isSave = false;
            if (req.UserName != "" && user.UserName != req.UserName && !req.IsCreate)
            {
                // 对照 :82-85 FindUserByFieldName 重名校验：单机档仅一个本地账号，
                // 前置条件 user.UserName != req.UserName 已排除自己，重名不可能发生，跳过
                user.UserName = req.UserName;
                isSave = true;
            }
            else if (req.IsCreate)
            {
                // 对照 :89-108：IsCreate 时忽略传入 UserName，生成随机名（GET_LOCK 行锁不迁移；
                // 重名判定同上，单机档不可能重名）
                for (int i = 0; i < 20; i++)
                {
                    string candidate = GenerateRandomName(ctx);
                    if (candidate == "")
                    {
                        break;
                    }
                    user.UserName = candidate;
                    isSave = true;
                    LogTool.Log($"userId={ctx.UserId} 新用户随机名称: {candidate}");
                    break;
                }
            }
            if (user.Sex != req.Sex)
            {
                user.Sex = req.Sex;
                isSave = true;
            }
            if (isSave)
            {
                ctx.Store.MarkUserDirty();
                // 对照 :117 PlayerCache.SetPlayerCache：Redis 缓存层不迁移，存档即权威
            }

            return new SavePlayerResponse { User = user, Player = player };
        }

        // 对照 application_player.go:140-183 SaveGuides
        public SaveGuidesResponse SaveGuides(LocalServerContext ctx, SaveGuidesRequest req)
        {
            // 对照 :141-145
            GuideFlowConfig cfg = DataConfigManager.Tables.TbGuideFlowConfig.GetOrDefault(req.Id);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 新手引导配置不存在 guideId={req.Id}");
                throw new LocalServerException("SYS_ERR_PRARMETER");
            }

            // 对照 :147-153
            UserPlayer player = ctx.Store.Archive.Player;
            Dictionary<int, int> guideFlows = UnmarshalGuideFlows(player);
            bool exist = guideFlows.TryGetValue(req.Id, out int status);
            if (status == GuideFlowFinished)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 新手引导已结束 guideId={req.Id}");
                throw new LocalServerException("已结束");
            }

            guideFlows[req.Id] = GuideFlowFinished;

            // 对照 :159-176 完成奖励（Go 在事务内发奖、player.Save 在事务后，发奖失败则引导状态不落库，保持同序）
            ResultItems rsItems = null;
            List<Config.RewardAndNum> rewards = null;
            if (cfg.Rewards.Count > 0)
            {
                if (!exist || status == GuideFlowReady)
                {
                    // 对照 :161-163：首次经过置为可领取，不发奖
                    guideFlows[req.Id] = GuideFlowReceive;
                }
                else
                {
                    try
                    {
                        rsItems = ctx.ItemEntrance.Execute(ctx.ItemEntrance.RewardsToItems(cfg.Rewards));
                    }
                    catch (System.Exception e)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 保存新手引导失败 guideId={req.Id}: {e.Message}");
                        throw new LocalServerException("保存新手引导失败");
                    }
                    rewards = cfg.Rewards;
                }
            }

            // 对照 :178-179 落库引导状态（GuideFlows{}.ToJson 即 {"id":status} 字典 JSON）
            player.GuideFlows = JsonConvert.SerializeObject(guideFlows);
            ctx.Store.MarkDirty(PlayerArchive.PlayerFieldNumber);

            // 对照 player_converter.go toSaveGuidesResponse：Rewards/Items 仅在发奖时下发
            var resp = new SaveGuidesResponse { Player = player };
            if (rewards != null)
            {
                foreach (Config.RewardAndNum v in rewards)
                {
                    resp.Rewards.Add(new Cardwar.RewardAndNum { Id = v.Id, Num = v.Num });
                }
                resp.Items = rsItems;
            }
            return resp;
        }

        // 对照 application_player.go:189-201 SaveModuleUnlockLv
        public SaveModuleUnlockLvResponse SaveModuleUnlockLv(LocalServerContext ctx, SaveModuleUnlockLvRequest req)
        {
            if (req.ModuleUnlockLv > ctx.UserLv)
            {
                LogTool.LogWarning($"userId={ctx.UserId} SYS_ERR_PRARMETER moduleUnlockLv={req.ModuleUnlockLv} level={ctx.UserLv}");
                throw new LocalServerException("SYS_ERR_PRARMETER");
            }

            UserPlayer player = ctx.Store.Archive.Player;
            player.ModuleUnlockLv = req.ModuleUnlockLv;
            ctx.Store.MarkDirty(PlayerArchive.PlayerFieldNumber);
            return new SaveModuleUnlockLvResponse { Player = player };
        }

        // 对照 utils/SensitiveWords.go:17-26 SensitiveWordsMatch
        private bool SensitiveWordsMatch(string input)
        {
            foreach (SensitiveWordsConfig cfg in DataConfigManager.Tables.TbSensitiveWordsConfig.DataList)
            {
                if (input.Contains(cfg.Character))
                {
                    return true;
                }
            }
            return false;
        }

        // 对照 pkg/randomname/random_name.go:12-29 Generate：随机姓 + 男/女名各 50%
        private string GenerateRandomName(LocalServerContext ctx)
        {
            List<RandomUserNameConfig> list = DataConfigManager.Tables.TbRandomUserNameConfig.DataList;
            if (list.Count == 0)
            {
                return "";
            }

            string surname = list[ctx.Rng.Next(list.Count)].Surname;
            string givenName = ctx.Rng.Next(2) == 0
                ? list[ctx.Rng.Next(list.Count)].ManName
                : list[ctx.Rng.Next(list.Count)].GirlName;
            return surname + givenName;
        }

        // 对照 UserModel.GetGuideIdByType（UserPlayerModel.go:124-134）
        private int GetGuideIdByType(GuideType guideType)
        {
            foreach (GuideFlowConfig cfg in DataConfigManager.Tables.TbGuideFlowConfig.DataList)
            {
                if (cfg.Type == guideType)
                {
                    return cfg.Id;
                }
            }

            LogTool.LogWarning($"引导类型[{(int)guideType}]，配置不存在");
            throw new LocalServerException($"引导类型[{(int)guideType}]，配置不存在");
        }

        // 对照 UserModel.UnmarshalGuideFlows（UserPlayerModel.go:101-110）
        private Dictionary<int, int> UnmarshalGuideFlows(UserPlayer player)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<int, int>>(player.GuideFlows);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={player.Id} json转换失败: {e.Message}");
                throw new LocalServerException("json转换失败: " + e.Message);
            }
        }

        // 对照 UserModel.SetGuidFlow（UserPlayerModel.go:112-122）：状态相同返回 false 不落库
        private bool SetGuidFlow(UserPlayer player, int guideId, int status)
        {
            Dictionary<int, int> guideFlows = UnmarshalGuideFlows(player);
            if (guideFlows.TryGetValue(guideId, out int guideStatus) && status == guideStatus)
            {
                return false;
            }

            guideFlows[guideId] = status;
            player.GuideFlows = JsonConvert.SerializeObject(guideFlows);
            return true;
        }
    }
}
