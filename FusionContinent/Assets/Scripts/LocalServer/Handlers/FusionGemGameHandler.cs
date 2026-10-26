using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 融合石小游戏 Handler，逐行对照 Server/app/api/application/challengeapplication/application_challenge.go。
    /// 存档读写对照 Server/app/model/ChallengeModel/PlayerChallengeModel.go（内存 Archive.Challenge 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案。
    /// 注意：Go 端结算的道具发放在事务内，panic 回滚后 gorm re-panic，
    /// panic("结算融合石游戏失败") 包装分支实际不可达，本地让内层 LocalServerException 原文案直接上抛。
    /// </summary>
    public class FusionGemGameHandler
    {
        // 对照 application_challenge.go:19-64 StartFusionGemGame
        public StartFusionGemGameResponse StartFusionGemGame(LocalServerContext ctx, StartFusionGemGameRequest req)
        {
            // 对照 ChallengeModel.GetChallengeInfo（:42-50）：查不到 challenge.Id==0
            PlayerChallenge challenge = ctx.Store.Archive.Challenge;
            if (challenge == null || challenge.DailyGemFusionNum >= DataConfigManager.Tables.TbGameConst.GemFuinGameDailyNum)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 挑战次数不足");
                throw new LocalServerException("挑战次数不足");
            }

            // 随机奖励（:26-39）：按品质分组全部融合石道具
            var gemList = new Dictionary<int, List<int>>();
            foreach (ItemConfig v in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                if (v.Type == ItemType.FusionStone)
                {
                    if (!gemList.TryGetValue((int)v.Quality, out List<int> qualityList))
                    {
                        qualityList = new List<int>();
                        gemList[(int)v.Quality] = qualityList;
                    }
                    qualityList.Add(v.Id);
                }
            }

            if (gemList.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 没有配置可随机的融合石");
                throw new LocalServerException("没有配置可随机的融合石");
            }

            // 对照 :41-50：按配置的品质列表各随机一个融合石（rand.Intn(len(qualityList))）
            var rewList = new List<int>();
            List<QualityType> rewardQualityTypeList = DataConfigManager.Tables.TbGameConst.GemFuinGameRewardQualityTypeList;
            foreach (QualityType quality in rewardQualityTypeList)
            {
                if (!gemList.TryGetValue((int)quality, out List<int> qualityList))
                {
                    continue;
                }
                int randomIdx = ctx.Rng.Next(qualityList.Count);
                rewList.Add(qualityList[randomIdx]);
            }

            // 保存（:52-61）。偏差：客户端 proto PlayerChallenge 无 UpdateTime 字段，跳过
            challenge.DailyGemFusionNum++;
            challenge.GemFusionRewards = JsonConvert.SerializeObject(rewList);
            ctx.Store.MarkDirty(PlayerArchive.ChallengeFieldNumber);

            return new StartFusionGemGameResponse { Challenge = challenge };
        }

        // 对照 application_challenge.go:76-131 SettleFusionGemGame
        public SettleFusionGemGameResponse SettleFusionGemGame(LocalServerContext ctx, SettleFusionGemGameRequest req)
        {
            // 达成档位个数（:77-88）
            int achieveNum = 0;
            foreach (int num in DataConfigManager.Tables.TbGameConst.GemFuinGameRewardProgress)
            {
                if (req.Progress >= num)
                {
                    achieveNum++;
                }
            }
            if (achieveNum == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未达成");
                throw new LocalServerException("未达成");
            }

            PlayerChallenge challenge = ctx.Store.Archive.Challenge;
            if (challenge == null || challenge.GemFusionRewards == "" || challenge.GemFusionRewards == "[]")
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未开始游戏");
                throw new LocalServerException("未开始游戏");
            }

            List<int> rewList;
            try
            {
                rewList = JsonConvert.DeserializeObject<List<int>>(challenge.GemFusionRewards);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} json转换失败: {e.Message}");
                throw new LocalServerException("json转换失败: " + e.Message);
            }

            // 前 achieveNum 个奖励各 +1（:102-109；Go 的 rewards[itemId]==0 归零是 no-op，不迁移）
            var rewards = new Dictionary<int, long>();
            for (int i = 0; i < achieveNum; i++)
            {
                int itemId = rewList[i];
                rewards.TryGetValue(itemId, out long cur);
                rewards[itemId] = cur + 1;
            }

            // 添加道具（:112-120）：Go 在事务内发放、失败时 GemFusionRewards 保留不清空，本地同序
            ResultItems rsItems = ctx.ItemEntrance.Execute(rewards);

            // 清空待领取奖励（:122-124）。偏差：客户端 proto 无 UpdateTime 字段，跳过
            challenge.GemFusionRewards = "[]";
            ctx.Store.MarkDirty(PlayerArchive.ChallengeFieldNumber);

            var resp = new SettleFusionGemGameResponse { Items = rsItems, Challenge = challenge };
            resp.Rewards.Add(rewards);
            return resp;
        }
    }
}
