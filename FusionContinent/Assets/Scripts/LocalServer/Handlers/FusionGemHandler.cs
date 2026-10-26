using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 融合石 Handler，逐行对照 Server/app/api/application/application_fusion_gem.go 的 V2 接口。
    /// 存档读写对照 Server/app/model/fusionGemModelV2/PlayerFusionGemModel.go（内存 Archive.FusionGemsV2 替代 MySQL）。
    /// 注：ComposeProbJSON（application_fusion_gem.go:25-37）在 Go 端已被注释、不参与运行，
    /// 实际概率来自 TbGemFusionConfig.LevelList[].SuccessProbability（GetSwallowProbCfg :263-275），本文件按运行路径翻译。
    /// </summary>
    public class FusionGemHandler
    {
        // 对照 application_fusion_gem.go:44-104 WearFusionGemV2Handler
        public WearFusionGemV2Response WearFusionGemV2(LocalServerContext ctx, WearFusionGemV2Request req)
        {
            // 对照 fusionGemModelV2.GetById，查不到等价 Id==0
            PlayerFusionGemToC gem = GetGemById(ctx, req.Id);
            if (gem == null || gem.UserId != ctx.UserId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 融合石不存在");
                throw new LocalServerException("融合石不存在");
            }

            GemFusionConfig gemCfg = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(gem.GemId);
            if (gemCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 融合石[{gem.GemId}]，配置不存在");
                throw new LocalServerException($"融合石[{gem.GemId}]，配置不存在");
            }

            // 验证装备（:57-63）
            if (req.WearerId > 0)
            {
                PlayerEquip equip = null;
                foreach (PlayerEquip e in ctx.Store.Archive.Equips)
                {
                    if (e.Id == req.WearerId)
                    {
                        equip = e;
                        break;
                    }
                }
                if (equip == null || equip.UserId != ctx.UserId)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 装备[{req.WearerId}], 未解锁");
                    throw new LocalServerException($"装备[{req.WearerId}], 未解锁");
                }
            }

            var rs = new List<PlayerFusionGemToC>();
            if (req.WearerId > 0)
            {
                // 被替换融合石去除佩戴装备（:70-75），对照 fusionGemModelV2.TxGetByWearId
                foreach (PlayerFusionGemToC other in ctx.Store.Archive.FusionGemsV2)
                {
                    if (other.UserId == ctx.UserId && other.WearerId == req.WearerId)
                    {
                        other.WearerId = 0;
                        rs.Add(other);
                        break;
                    }
                }
                gem.WearerId = req.WearerId;
            }
            else
            {
                gem.WearerId = 0;
            }

            rs.Add(gem);
            ctx.Store.MarkDirty(PlayerArchive.FusionGemsV2FieldNumber);

            // 计算战力（:102-103）
            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleEquip);

            // 对照 fusiongem_converter.go:55-60
            var resp = new WearFusionGemV2Response
            {
                Items = new ResultItems { CombatPower = new CombatPower { CombatPower_ = cp } },
            };
            resp.Gems.AddRange(rs);
            return resp;
        }

        // 对照 application_fusion_gem.go:106-164 FusionGemCompoundHandler
        public FusionGemCompoundResponse FusionGemCompound(LocalServerContext ctx, FusionGemCompoundRequest req)
        {
            // 对照 :116-119 TxPlayerSpeakerFusionGemByIds + lo.KeyBy
            var fusionMap = new Dictionary<int, PlayerFusionGemToC>();
            foreach (PlayerFusionGemToC gem in ctx.Store.Archive.FusionGemsV2)
            {
                if (gem.Id == req.PrimaryId || gem.Id == req.SecondaryId)
                {
                    fusionMap[gem.Id] = gem;
                }
            }

            // 检测融合石条件（:122 → checkFusion :195-239）
            CheckFusion(ctx, req.PrimaryId, req.SecondaryId, fusionMap);

            PlayerFusionGemToC sourceFusionGem = fusionMap[req.PrimaryId];
            PlayerFusionGemToC swallowFusionGem = fusionMap[req.SecondaryId];

            // 吞噬（:131 → doSwallow :242-246）：r < prob + sourceProgress
            double prob = GetProb(ctx, sourceFusionGem.GemId, sourceFusionGem.Level, swallowFusionGem.Level);
            bool swallowResult = ctx.Rng.NextDouble() < prob + sourceFusionGem.Progress;

            if (swallowResult)
            {
                // source卡升级（:136-137）
                sourceFusionGem.Level += 1;
                sourceFusionGem.Progress = 0.00;
            }
            else
            {
                // source卡进度增加（:140 → increaseProgress :278-292）
                sourceFusionGem.Progress = IncreaseProgress(ctx, prob, sourceFusionGem.Progress);
            }

            // swallow卡不管怎么样都是要删除的（:147）
            ctx.Store.Archive.FusionGemsV2.Remove(swallowFusionGem);
            ctx.Store.MarkDirty(PlayerArchive.FusionGemsV2FieldNumber);

            // 计算战力（:156）
            long combatPower = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleEquip);

            // 对照 fusiongem_converter.go:36-53
            return new FusionGemCompoundResponse
            {
                Success = swallowResult,
                PrimaryGem = new PlayerFusionGem
                {
                    Id = sourceFusionGem.Id,
                    UserId = sourceFusionGem.UserId,
                    GemId = sourceFusionGem.GemId,
                    WearerId = sourceFusionGem.WearerId,
                    Level = sourceFusionGem.Level,
                },
                SecondaryId = req.SecondaryId,
                Items = new ResultItems { CombatPower = new CombatPower { CombatPower_ = combatPower } },
            };
        }

        // 对照 checkFusion（application_fusion_gem.go:195-239）
        private void CheckFusion(LocalServerContext ctx, int sourceId, int swallowId, Dictionary<int, PlayerFusionGemToC> fusionMap)
        {
            // 拥有检测
            if (!fusionMap.TryGetValue(sourceId, out PlayerFusionGemToC sourceFusionGem))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 不存在此融合石");
                throw new LocalServerException("不存在此融合石");
            }
            if (!fusionMap.TryGetValue(swallowId, out PlayerFusionGemToC swallowFusionGem))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 不存在此融合石");
                throw new LocalServerException("不存在此融合石");
            }
            foreach (var kv in fusionMap)
            {
                if (kv.Value.UserId != ctx.UserId)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 不是用户的融合石");
                    throw new LocalServerException("不是用户的融合石");
                }
            }
            // 只能是同种融合石
            if (sourceFusionGem.GemId != swallowFusionGem.GemId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 只能吞噬同种融合石");
                throw new LocalServerException("只能吞噬同种融合石");
            }
            // 融合石已经是满阶位了（:219-223）
            // Go 端此处 gemCfg 为 nil 会解引用 panic，本地 Get 缺 key 直接抛（fail fast）
            GemFusionConfig gemCfg = DataConfigManager.Tables.TbGemFusionConfig.Get(sourceFusionGem.GemId);
            if (sourceFusionGem.Level >= gemCfg.LevelList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 原融合石已满阶位");
                throw new LocalServerException("原融合石已满阶位");
            }
            // 阶位检测
            if (sourceFusionGem.Level < swallowFusionGem.Level)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 被吞噬的融合石头阶位必须小于等于吞噬融合石");
                throw new LocalServerException("被吞噬的融合石头阶位必须小于等于吞噬融合石");
            }
            // 阶位检测
            if (sourceFusionGem.Level - swallowFusionGem.Level > 3)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 被吞噬的融合石头阶位不能相差大于3阶");
                throw new LocalServerException("被吞噬的融合石头阶位不能相差大于3阶");
            }
            // 如果融合石绑定了装备，先提示用户需要卸载融合石
            if (swallowFusionGem.WearerId > 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 请先卸载被吞噬融合石");
                throw new LocalServerException("请先卸载被吞噬融合石");
            }
        }

        // 对照 GetProb（application_fusion_gem.go:249-259）
        private double GetProb(LocalServerContext ctx, int gemId, int sourceLevel, int swallowLevel)
        {
            Dictionary<int, Dictionary<int, double>> composeCfg = GetSwallowProbCfg(ctx, gemId);
            if (composeCfg == null)
            {
                LogTool.LogWarning("融合概率配置不存在");
                throw new LocalServerException("融合概率配置不存在");
            }
            if (composeCfg.TryGetValue(sourceLevel, out Dictionary<int, double> m))
            {
                // Go map 缺 key 返回零值
                m.TryGetValue(swallowLevel, out double prob);
                return prob;
            }
            return 0;
        }

        // 对照 GetSwallowProbCfg（application_fusion_gem.go:263-275）：
        // 每层 LevelList 的 SuccessProbability 从本阶往前逐阶递减映射（startLevel--）。
        // Go 端此处 gemCfg 为 nil 会解引用 panic，本地 Get 缺 key 直接抛（fail fast）。
        private Dictionary<int, Dictionary<int, double>> GetSwallowProbCfg(LocalServerContext ctx, int gemId)
        {
            GemFusionConfig gemCfg = DataConfigManager.Tables.TbGemFusionConfig.Get(gemId);
            var cfg = new Dictionary<int, Dictionary<int, double>>();
            foreach (GemFusionLevelListStruct levelCfg in gemCfg.LevelList)
            {
                cfg[levelCfg.Level] = new Dictionary<int, double>();
                int startLevel = levelCfg.Level;
                foreach (float probCfg in levelCfg.SuccessProbability)
                {
                    cfg[levelCfg.Level][startLevel] = probCfg;
                    startLevel--;
                }
            }
            return cfg;
        }

        // 对照 increaseProgress（application_fusion_gem.go:278-292）：基础概率 + 0~1% 随机浮动，保留4位小数
        private double IncreaseProgress(LocalServerContext ctx, double baseProb, double current)
        {
            double delta = baseProb + ctx.Rng.NextDouble() * 0.01;

            current += delta;
            if (current > 1)
            {
                current = 1;
            }

            // Go: float64(int(current*10000+0.5)) / 10000（正数截断即四舍五入）
            current = (int)(current * 10000 + 0.5) / 10000.0;

            return current;
        }

        // 对照 fusionGemModelV2.GetById：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerFusionGemToC GetGemById(LocalServerContext ctx, int id)
        {
            foreach (PlayerFusionGemToC gem in ctx.Store.Archive.FusionGemsV2)
            {
                if (gem.Id == id)
                {
                    return gem;
                }
            }
            return null;
        }
    }
}
