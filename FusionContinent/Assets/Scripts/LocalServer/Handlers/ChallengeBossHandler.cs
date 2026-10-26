using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 挑战BOSS Handler，逐行对照 Server/app/api/application/challengebossapplication/application_challenge_boss.go。
    /// 存档读写对照 Server/app/model/ChallengeBossModel/PlayerChallengeBossModel.go（内存 Archive.ChallengeBoss 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案。
    /// 注意：Go 端 boss.Save() 与道具发放在同一事务内，业务失败（panic）回滚后 gorm re-panic，
    /// err 分支的 panic("升级失败")/panic("领取挑战Boss积分奖励") 实际不可达，本地让内层
    /// LocalServerException 原文案直接上抛，同时恢复 boss 字段模拟事务回滚。
    /// </summary>
    public class ChallengeBossHandler
    {
        // 对照 application_challenge_boss.go:25-81 ReceiveDailyReward
        public ReceiveChallengeBossDailyRewardResponse ReceiveDailyReward(LocalServerContext ctx, ReceiveChallengeBossDailyRewardRequest req)
        {
            ChallengeBossConfig bossCfg = DataConfigManager.Tables.TbChallengeBossConfig.GetOrDefault(req.BossId);
            if (bossCfg == null || bossCfg.Status == ActivityStatusType.Close)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 挑战BOSS[{req.BossId}]，暂未开放");
                throw new LocalServerException($"挑战BOSS[{req.BossId}]，暂未开放");
            }

            // 对照 ChallengeBossModel.GetById：查不到 boss.Id==0
            PlayerChallengeBoss boss = GetById(ctx, req.BossId);
            if (boss == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            if (boss.DailyChallengeNum == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未达成");
                throw new LocalServerException("未达成");
            }

            List<int> dailyRewards = ParseIntList(boss.DailyRewards, "解析每日奖励数据失败");
            if (dailyRewards.Count > 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                throw new LocalServerException("已领取");
            }

            // 保存修改（:54-61）；Go 中 boss.Save() 在事务内（:68），失败回滚 → 本地先记旧值
            dailyRewards.Add(1);
            string oldDailyRewards = boss.DailyRewards;
            boss.DailyRewards = JsonConvert.SerializeObject(dailyRewards);

            // 奖励（:63-70）
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(bossCfg.DailyChallengeRewards);
            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
            }
            catch
            {
                // 模拟事务回滚：boss 字段恢复，异常原文案继续上抛
                boss.DailyRewards = oldDailyRewards;
                throw;
            }
            ctx.Store.MarkDirty(PlayerArchive.ChallengeBossFieldNumber);

            var resp = new ReceiveChallengeBossDailyRewardResponse { Boss = boss, Items = rsItems };
            resp.Rewards.Add(rewards);
            return resp;
        }

        // 对照 application_challenge_boss.go:87-156 ReceiveScoreReward
        public ReceiveChallengeBossScoreRewardResponse ReceiveScoreReward(LocalServerContext ctx, ReceiveChallengeBossScoreRewardRequest req)
        {
            ChallengeBossConfig bossCfg = DataConfigManager.Tables.TbChallengeBossConfig.GetOrDefault(req.BossId);
            if (bossCfg == null || bossCfg.Status == ActivityStatusType.Close)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 挑战BOSS[{req.BossId}]，暂未开放");
                throw new LocalServerException($"挑战BOSS[{req.BossId}]，暂未开放");
            }

            ScoreRewardListStruct scoreCfg = null;
            foreach (ScoreRewardListStruct v in bossCfg.ScoreRewardList)
            {
                if (v.Score == req.Score)
                {
                    scoreCfg = v;
                    break;
                }
            }
            if (scoreCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 积分[{req.Score}]，档位不存在");
                throw new LocalServerException($"积分[{req.Score}]，档位不存在");
            }

            PlayerChallengeBoss boss = GetById(ctx, req.BossId);
            if (boss == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            if (scoreCfg.Score > boss.TotalScore)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未达成");
                throw new LocalServerException("未达成");
            }

            List<int> scoreRewards = ParseIntList(boss.ScoreRewards, "解析积分奖励数据失败");
            // 对照 :124 utils.ContainsArr（len>0 且已含该积分档）
            if (scoreRewards.Count > 0 && scoreRewards.Contains(scoreCfg.Score))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                throw new LocalServerException("已领取");
            }

            // 保存修改（:129-136）；Go 中 boss.Save() 在事务内（:143），失败回滚 → 本地先记旧值
            scoreRewards.Add(scoreCfg.Score);
            string oldScoreRewards = boss.ScoreRewards;
            boss.ScoreRewards = JsonConvert.SerializeObject(scoreRewards);

            // 奖励（:138-145）
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(scoreCfg.Rewards);
            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
            }
            catch
            {
                boss.ScoreRewards = oldScoreRewards;
                throw;
            }
            ctx.Store.MarkDirty(PlayerArchive.ChallengeBossFieldNumber);

            var resp = new ReceiveChallengeBossScoreRewardResponse { Boss = boss, Items = rsItems };
            resp.Rewards.Add(rewards);
            return resp;
        }

        // 对照 ChallengeBossModel.GetById：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerChallengeBoss GetById(LocalServerContext ctx, int bossId)
        {
            foreach (PlayerChallengeBoss boss in ctx.Store.Archive.ChallengeBoss)
            {
                if (boss.BossId == bossId)
                {
                    return boss;
                }
            }
            return null;
        }

        // 对照 json.Unmarshal(dailyRewards/scoreRewards)（:43-47 / :118-122）
        // 偏差：Go 反序列化 "null" 得到空切片；本地 null 按空列表处理
        private List<int> ParseIntList(string json, string errPrefix)
        {
            try
            {
                List<int> list = JsonConvert.DeserializeObject<List<int>>(json);
                return list != null ? list : new List<int>();
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"{errPrefix}: {e.Message}");
                throw new LocalServerException(errPrefix + ": " + e.Message);
            }
        }
    }
}
