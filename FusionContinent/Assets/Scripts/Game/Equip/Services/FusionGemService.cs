using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework.Services;
using PlayerFusionGem = FusionContinent.Model.PlayerFusionGem;

namespace Game.Equip
{
    /// <summary>
    /// 融合石服务：统一管理玩家融合石数据（Global.PlayerFusionGems）并提供变更事件。
    /// </summary>
    public class FusionGemService : GameServiceBase
    {
        
        public class CompoundResult
        {
            public int SecondaryId { get; set; }
            public bool Success { get; set; }
            public PlayerFusionGem PrimaryGem { get; set; } //变更的融合石
        }
        
        public bool IsInitialized { get; private set; }

        private TbGemFusionConfig mTbGemFusionConfig;
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            mTbGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
            IsInitialized = true;
            LogTool.Log("[FusionGemService] 初始化完成");
        }

        public async UniTask<CompoundResult> FusionGemCompound(int mainGemId,int secondaryGemId)
        {
            var request = new FusionGemCompoundRequest { PrimaryId = mainGemId, SecondaryId = secondaryGemId };
            var proto = await WebDataFun.SendProtoPostRequest<FusionGemCompoundResponse>(ApiKeys.ApiFusionGemCompound, request);
            if (proto == null  || proto.PrimaryGem == null)
            {
                return null;
            }

            var primaryGem = new PlayerFusionGem(new PlayerFusionGem
            {
                Id = proto.PrimaryGem.Id,
                GemId = proto.PrimaryGem.GemId,
                WearerId = proto.PrimaryGem.WearerId,
                Level = proto.PrimaryGem.Level
            });

            Global.PlayerFusionGems.Remove(proto.SecondaryId);
            Global.PlayerFusionGems[primaryGem.Id] = primaryGem;
            Items.Update(proto.Items);
            return new CompoundResult
            {
                Success = proto.Success,
                SecondaryId = proto.SecondaryId,
                PrimaryGem = primaryGem
            };
        }
        

        /// <summary>
        /// 获取融合石品阶本地化Key（参数：等级 + 品质）。
        /// 注意：当前本地化Key只与品质有关，等级用于统一接口签名，便于外部格式化显示（如“{品质} Lv.{等级}”）。
        /// </summary>
        public string GetGemLevelText(int level)
        {
            return $"{level}阶";
            // level 目前不参与Key映射，但保留入参便于统一调用
            // return GetQualityTextKey(quality);
        }

        public List<PlayerFusionGem> GetAllSorted()
        {
            if (Global.PlayerFusionGems == null || Global.PlayerFusionGems.Count == 0)
            {
                return new List<PlayerFusionGem>();
            }

            EnsureConfig(Global.PlayerFusionGems.Values);

            return Global.PlayerFusionGems.Values
                .Where(v => v != null && v.Config != null)
                .OrderByDescending(v => v.Quality)
                .ThenByDescending(v => v.Level)
                .ThenByDescending(v => v.Id)
                .ToList();
        }

        public bool TryGet(int gemOnlyId, out PlayerFusionGem gem)
        {
            gem = null;
            return Global.PlayerFusionGems != null && Global.PlayerFusionGems.TryGetValue(gemOnlyId, out gem);
        }

        private void EnsureConfig(IEnumerable<PlayerFusionGem> gems)
        {
            if (gems == null) return;
            if (mTbGemFusionConfig == null)
            {
                mTbGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
            }

            foreach (var gem in gems)
            {
                if (gem == null) continue;
                if (gem.Config == null && mTbGemFusionConfig != null && gem.GemId > 0)
                {
                    gem.Config = mTbGemFusionConfig.GetOrDefault(gem.GemId);
                }
                if (gem.Config != null)
                {
                    gem.Quality = gem.Config.Quality;
                }
            }
        }
    }
}

