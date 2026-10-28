using System;
using System.Collections.Generic;
using Config;
using FusionContinent;

namespace Game.Stage
{
    /// <summary>
    /// 每日随机关卡选择器：每个重置日（凌晨 TbGameConst.GameResetHour 点为界）从
    /// “未通关且章节已解锁”的主线关卡中按日期种子确定性随机选出一关，当天只能首通该关。
    /// 纯函数实现，客户端（Global 镜像数据）与本地服务器（存档 proto）共用同一套口径。
    /// 后续“第 N 天指定出什么关”类排期规则，在 GetTodayStageId 内按 GetResetDayIndex 加分支即可。
    /// </summary>
    public static class DailyStageSelector
    {
        /// <summary>
        /// 计算今日关卡。candidatePool 由调用方按自身数据源构建（见 BuildCandidatePool）。
        /// 返回 0 表示今日无可用关卡（候选池为空）。
        /// </summary>
        public static int GetTodayStageId(List<int> candidatePool, long nowTs)
        {
            if (candidatePool == null || candidatePool.Count == 0) return 0;

            // 排序保证候选池顺序与遍历顺序无关，同一种子结果稳定
            candidatePool.Sort();
            var rand = new Random(GetResetDayIndex(nowTs));
            return candidatePool[rand.Next(candidatePool.Count)];
        }

        /// <summary>当前重置日序号（跨重置点才 +1），作为每日随机种子。</summary>
        public static int GetResetDayIndex(long nowTs)
        {
            int resetHour = DataConfigManager.Tables.TbGameConst.GameResetHour;
            DateTime day = DateTimeOffset.FromUnixTimeSeconds(nowTs).LocalDateTime.AddHours(-resetHour).Date;
            return (int)(day - new DateTime(1970, 1, 1)).TotalDays;
        }

        /// <summary>
        /// 构建候选池：未通关（不在 clearedStageIds 中）且章节已解锁的主线关卡。
        /// isUnlocked 由调用方提供（客户端读 Global.PlayerChapters，服务端读存档 Chapters）。
        /// </summary>
        public static List<int> BuildCandidatePool(ISet<int> clearedStageIds, Func<StageBattleConfig, bool> isUnlocked)
        {
            var pool = new List<int>();
            var list = DataConfigManager.Tables.TbStageBattleConfig.DataList;
            for (int i = 0; i < list.Count; i++)
            {
                StageBattleConfig cfg = list[i];
                if (cfg.StageType != StageType.Main) continue;
                if (clearedStageIds.Contains(cfg.Id)) continue;
                if (!isUnlocked(cfg)) continue;
                pool.Add(cfg.Id);
            }
            return pool;
        }
    }
}
