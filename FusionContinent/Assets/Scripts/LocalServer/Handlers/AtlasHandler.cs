using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 图鉴 Handler，逐行对照 Server/app/api/application/atlasapplication/application_atlas.go。
    /// 偏差：Go 的 MonsterAtlas 是 JSON 字符串，客户端 proto 已是 map<int32,int32>，直接读写。
    /// </summary>
    public class AtlasHandler
    {
        // 对照 configFiles/RewardStatus.go
        private const int RewardStatusReceived = 2;

        // 对照 application_atlas.go:21-63 ReceiveMonsterReward
        public ReceiveMonsterRewardResponse ReceiveMonsterReward(LocalServerContext ctx, ReceiveMonsterRewardRequest req)
        {
            // 对照 :22-26
            MonsterBattleConfig monsterCfg = DataConfigManager.Tables.TbMonsterBattleConfig.GetOrDefault(req.MonsterId);
            if (monsterCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} SYS_ERR_CONFIG_NOT_EXIST");
                throw new LocalServerException("SYS_ERR_CONFIG_NOT_EXIST");
            }

            // 对照 :28-31
            if (monsterCfg.AtlasRewards.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} SYS_ERR_CONFIG_NOT_REWARD");
                throw new LocalServerException("SYS_ERR_CONFIG_NOT_REWARD");
            }

            // 对照 :33-40：图鉴记录不存在或未记录该怪物（Go map 查不到）或状态非法
            // 注意 Go 判定为 state > Received 才拒绝，state == Received（已领取）仍可重复领取，保留 Go 原口径
            PlayerAtlas atlas = ctx.Store.Archive.Atlas;
            if (!atlas.MonsterAtlas.TryGetValue(req.MonsterId, out int state) || state > RewardStatusReceived)
            {
                LogTool.LogWarning($"userId={ctx.UserId} SYS_ERR_REWARD_NOT_AVAILABLE");
                throw new LocalServerException("SYS_ERR_REWARD_NOT_AVAILABLE");
            }

            // 对照 :42-44：先落图鉴状态（Go 在发奖事务前 Save，保持一致顺序）
            atlas.MonsterAtlas[req.MonsterId] = RewardStatusReceived;
            ctx.Store.MarkDirty(PlayerArchive.AtlasFieldNumber);

            // 对照 :47-56 发放奖励
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(monsterCfg.AtlasRewards);
            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 添加奖励失败: {e.Message}");
                throw new LocalServerException("添加奖励失败");
            }

            // 对照 atlas_converter.go toReceiveMonsterRewardResponse：Rewards 为展开前的奖励表
            var resp = new ReceiveMonsterRewardResponse { PlayerAtlas = atlas, Items = rsItems };
            foreach (var kv in rewards)
            {
                resp.Rewards[kv.Key] = kv.Value;
            }
            return resp;
        }
    }
}
