using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Utils;
using UIFramework.Services;

namespace Game.Challenge
{
    /// <summary>
    /// 排行榜数据（三种榜单共用结构：榜单列表 + 自己排名 + 总数）。
    /// 由三种协议响应（Chapter/HiddenRealmTower/CombatPower）归一化而来。
    /// </summary>
    public class RankingData
    {
        public RankingType Type;
        public int Page;
        public int PageSize;
        public int Total;
        public List<RankInfo> List = new List<RankInfo>();
        public RankInfo SelfRank;
    }

    /// <summary>
    /// 排行榜服务：按 RankingType 拉取对应榜单数据。
    ///   MainStoryline（主线）   -> /api/ranking/chapter
    ///   FightNum（战力）         -> /api/ranking/combatPower
    ///   FantasyRealm（奇幻秘境） -> /api/ranking/hiddenRealmTower
    /// 网络请求与数据缓存集中在此，ViewModel 只负责调用并消费结果。
    /// </summary>
    public class RankService : GameServiceBase
    {
        /// <summary>最近一次拉取成功的榜单数据。</summary>
        public RankingData Current { get; private set; }

        /// <summary>
        /// 拉取指定类型排行榜。page 从 1 开始，pageSize 默认 10 最大 100。
        /// 成功返回 RankingData 并缓存到 Current；失败 Toast 并返回 null。
        /// </summary>
        public async UniTask<RankingData> FetchRankingAsync(RankingType type, int page = 1, int pageSize = 10)
        {
            var request = new RankingRequest { Page = page, PageSize = pageSize };

            try
            {
                RankingData data;
                switch (type)
                {
                    case RankingType.MainStoryline:
                    {
                        var resp = await WebDataFun.SendProtoPostRequest<ChapterRankingResponse>(
                            ApiKeys.ApiRankingChapter, request);
                        if (resp == null)
                        {
                            Toast.Show("获取排行榜失败");
                            return null;
                        }
                        data = BuildData(type, page, pageSize, resp.List, resp.SelfRank, resp.Total);
                        break;
                    }
                    case RankingType.FightNum:
                    {
                        var resp = await WebDataFun.SendProtoPostRequest<CombatPowerRankingResponse>(
                            ApiKeys.ApiRankingCombatPower, request);
                        if (resp == null)
                        {
                            Toast.Show("获取排行榜失败");
                            return null;
                        }
                        data = BuildData(type, page, pageSize, resp.List, resp.SelfRank, resp.Total);
                        break;
                    }
                    case RankingType.FantasyRealm:
                    {
                        var resp = await WebDataFun.SendProtoPostRequest<HiddenRealmTowerRankingResponse>(
                            ApiKeys.ApiRankingHiddenRealmTower, request);
                        if (resp == null)
                        {
                            Toast.Show("获取排行榜失败");
                            return null;
                        }
                        data = BuildData(type, page, pageSize, resp.List, resp.SelfRank, resp.Total);
                        break;
                    }
                    default:
                        LogTool.LogError("[RankService] 未知的排行榜类型: " + type);
                        return null;
                }

                Current = data;
                NotifyDataChanged();
                return data;
            }
            catch (Exception ex)
            {
                LogTool.LogError("[RankService] FetchRanking failed: " + ex.Message);
                Toast.Show(ex.Message);
                return null;
            }
        }

        private static RankingData BuildData(RankingType type, int page, int pageSize,
            IEnumerable<RankInfo> list, RankInfo selfRank, int total)
        {
            var data = new RankingData
            {
                Type = type,
                Page = page,
                PageSize = pageSize,
                Total = total,
                SelfRank = selfRank,
            };
            if (list != null)
            {
                data.List.AddRange(list);
            }
            return data;
        }

        public override void Reset()
        {
            Current = null;
            base.Reset();
        }
    }
}
