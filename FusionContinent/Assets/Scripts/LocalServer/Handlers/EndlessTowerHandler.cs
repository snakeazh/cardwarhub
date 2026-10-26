using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 无尽塔 Handler，逐行对照 Server/app/api/application/endlesstowerapplication/application_endless_tower.go。
    /// 存档读写对照 Server/app/model/EndlessTowerModel/PlayerEndlessTowerModel.go（内存 Archive.EndlessTowers 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案。
    /// 注意：Go 端奖励发放放在 gorm 事务里，UniteAddItemsEntranceLogic 业务失败是 panic，
    /// gorm 事务对 panic 回滚后直接 re-panic（不走 err 分支），因此 Go 的
    /// panic("领取爬塔宝箱失败")/panic("领取无尽塔每日奖励失败") 包装分支实际不可达，
    /// 本地不包 try/catch，让内层 LocalServerException 原文案直接上抛（与线上行为一致）。
    /// </summary>
    public class EndlessTowerHandler
    {
        // 对照 application_endless_tower.go:18-56 NextFloor
        public NextFloorResponse NextFloor(LocalServerContext ctx, NextFloorRequest req)
        {
            EndlessTowerConfig towerCfg = DataConfigManager.Tables.TbEndlessTowerConfig.GetOrDefault(req.TowerId);
            if (towerCfg == null || towerCfg.Status == ActivityStatusType.Close)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔[{req.TowerId}]，暂未开放");
                throw new LocalServerException($"无尽塔[{req.TowerId}]，暂未开放");
            }

            // 对照 EndlessTowerModel.GetById（:100-108）：查不到 tower.Id==0
            PlayerEndlessTower tower = GetById(ctx, req.TowerId);
            if (tower == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            EndlessTowerMapConfig mapCfg = DataConfigManager.Tables.TbEndlessTowerMapConfig.GetOrDefault(tower.MapId);
            if (mapCfg == null || req.NodeId > mapCfg.NodeList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔地图节点[{req.NodeId}]，错误");
                throw new LocalServerException($"无尽塔地图节点[{req.NodeId}]，错误");
            }

            // Go 端 nodeId<=0 时 NodeList[nodeId-1] 越界 panic（运行时错误），本地同样 fail fast
            MapNodeListStruct nodeCfg = mapCfg.NodeList[req.NodeId - 1];
            if (nodeCfg.NodeType != MapNodeType.NextFloor)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔地图节点[{req.NodeId}], 非法");
                throw new LocalServerException($"无尽塔地图节点[{req.NodeId}], 非法");
            }

            // 判断通关节点与地图节点（:43-49）
            Dictionary<int, int> clearNodes = ParseClearNodes(tower.ClearNodes);
            if (!CheckPreNodes(clearNodes, nodeCfg.PreNone))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未通过地图节点");
                throw new LocalServerException("未通过地图节点");
            }

            // 进入下一层&保存（:51-53 → PlayerEndlessTowerModel.NextFloor:56-63）
            // 偏差：客户端 proto PlayerEndlessTower 无 UpdateTime 字段，跳过
            tower.Floor++;
            tower.MapId = RandomTowerMapId(ctx);
            tower.NodeId = 0;
            tower.ClearNodes = "{}";
            tower.FloorRewards = "[]";
            ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);

            return new NextFloorResponse { Tower = tower };
        }

        // 对照 application_endless_tower.go:68-123 ReceiveChestReward
        public ReceiveTowerChestRewardResponse ReceiveChestReward(LocalServerContext ctx, ReceiveTowerChestRewardRequest req)
        {
            EndlessTowerConfig towerCfg = DataConfigManager.Tables.TbEndlessTowerConfig.GetOrDefault(req.TowerId);
            if (towerCfg == null || towerCfg.Status == ActivityStatusType.Close)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔[{req.TowerId}]，暂未开放");
                throw new LocalServerException($"无尽塔[{req.TowerId}]，暂未开放");
            }

            PlayerEndlessTower tower = GetById(ctx, req.TowerId);
            if (tower == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            EndlessTowerMapConfig mapCfg = DataConfigManager.Tables.TbEndlessTowerMapConfig.GetOrDefault(tower.MapId);
            if (mapCfg == null || req.NodeId > mapCfg.NodeList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔地图节点[{req.NodeId}]，错误");
                throw new LocalServerException($"无尽塔地图节点[{req.NodeId}]，错误");
            }

            Dictionary<int, int> clearNodes = ParseClearNodes(tower.ClearNodes);

            MapNodeListStruct nodeCfg = mapCfg.NodeList[req.NodeId - 1];
            if (!clearNodes.TryGetValue(nodeCfg.NodeId, out int nodeState) || nodeState == (int)MapNodeState.Init)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未达成");
                throw new LocalServerException("未达成");
            }
            if (nodeState == (int)MapNodeState.Receive)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                throw new LocalServerException("已领取");
            }

            // 保存修改（:101-104）：Go 在奖励事务之前单独 Save，奖励失败时宝箱仍保持已领取状态，本地保持一致
            clearNodes[nodeCfg.NodeId] = (int)MapNodeState.Receive;
            tower.ClearNodes = JsonConvert.SerializeObject(clearNodes);
            ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);

            // 奖励（:106-116）
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(nodeCfg.Rewards);
            ResultItems rsItems = ctx.ItemEntrance.Execute(rewards);

            var resp = new ReceiveTowerChestRewardResponse { Tower = tower, Items = rsItems };
            resp.Rewards.Add(rewards);
            return resp;
        }

        // 对照 application_endless_tower.go:129-185 ReceiveDailyReward
        public ReceiveDailyRewardResponse ReceiveDailyReward(LocalServerContext ctx, ReceiveDailyRewardRequest req)
        {
            EndlessTowerConfig towerCfg = DataConfigManager.Tables.TbEndlessTowerConfig.GetOrDefault(req.TowerId);
            if (towerCfg == null || towerCfg.Status == ActivityStatusType.Close)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 无尽塔[{req.TowerId}]，暂未开放");
                throw new LocalServerException($"无尽塔[{req.TowerId}]，暂未开放");
            }

            PlayerEndlessTower tower = GetById(ctx, req.TowerId);
            if (tower == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            if (tower.DailyChallengeNum == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未达成");
                throw new LocalServerException("未达成");
            }

            List<int> dailyRewards = ParseIntList(tower.DailyRewards, "解析每日奖励数据失败");
            if (dailyRewards.Count > 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                throw new LocalServerException("已领取");
            }

            // 保存修改（:158-166）：Go 同样在奖励事务之前单独 Save
            dailyRewards.Add(1);
            tower.DailyRewards = JsonConvert.SerializeObject(dailyRewards);
            ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);

            // 奖励（:168-178）
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(towerCfg.DailyChallengeRewards);
            ResultItems rsItems = ctx.ItemEntrance.Execute(rewards);

            var resp = new ReceiveDailyRewardResponse { Tower = tower, Items = rsItems };
            resp.Rewards.Add(rewards);
            return resp;
        }

        // 对照 EndlessTowerModel.GetById（:100-108）：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerEndlessTower GetById(LocalServerContext ctx, int towerId)
        {
            foreach (PlayerEndlessTower tower in ctx.Store.Archive.EndlessTowers)
            {
                if (tower.TowerId == towerId)
                {
                    return tower;
                }
            }
            return null;
        }

        // 对照 PlayerEndlessTowerModel.CheckPreNodes（:65-80）：前置节点任一已通关（state >= Clear）即可
        private bool CheckPreNodes(Dictionary<int, int> clearNodes, List<int> preNodes)
        {
            if (preNodes.Count == 0)
            {
                return true;
            }
            foreach (int preNodeId in preNodes)
            {
                if (clearNodes.TryGetValue(preNodeId, out int nodeState) && nodeState >= (int)MapNodeState.Clear)
                {
                    return true;
                }
            }
            return false;
        }

        // 对照 EndlessTowerModel.RandomTowerMapId（:135-144）
        private int RandomTowerMapId(LocalServerContext ctx)
        {
            List<EndlessTowerMapConfig> mapList = DataConfigManager.Tables.TbEndlessTowerMapConfig.DataList;
            if (mapList.Count == 0)
            {
                return 1;
            }
            return mapList[ctx.Rng.Next(mapList.Count)].Id;
        }

        // 对照 ClearNodes.FromJson（PlayerEndlessTowerModel.go:155-161）
        // 偏差：Go 反序列化 "null" 得到 nil map、写入时 panic；本地按空字典处理
        private Dictionary<int, int> ParseClearNodes(string json)
        {
            try
            {
                Dictionary<int, int> nodes = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);
                return nodes != null ? nodes : new Dictionary<int, int>();
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"json转换失败: {e.Message}");
                throw new LocalServerException("json转换失败: " + e.Message);
            }
        }

        // 对照 json.Unmarshal(dailyRewards)（application_endless_tower.go:148-151）
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
