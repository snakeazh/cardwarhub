using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;
using UnityEngine;

namespace FusionContinent.Model
{
    public class PlayerFusionGem
    {
        public int Id { get; set; } // 主键
        public int GemId { get; set; } // 宝石id
        public int WearerId { get; set; } // 佩戴者（0=未佩戴；非0通常为装备唯一Id）
        public int Level { get; set; } // 等级
        public QualityType Quality { get; set; } // 稀有度
        public GemFusionConfig Config { get; set; } // 配置


        public float Progress { get; set; }//进度值显示

        public PlayerFusionGem()
        {
        }
        
        public PlayerFusionGem(PlayerFusionGem v)
        {
            Config = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(v.GemId);
            if (Config == null)
            {
                LogTool.LogError($"初始化融合石Id[{v.GemId}]配置不存在");
                return;
            }

            Id = v.Id;
            GemId = v.GemId;
            WearerId = v.WearerId;
            Level = v.Level;
            Progress = v.Progress;
            Quality = Config.Quality;
        }

        public GemFusionLevelListStruct GetLevelCfg()
        {
            return Config.LevelList[this.Level-1];
        }
        
        public GemFusionLevelListStruct GetLevelCfg(int targetLevel)
        {
            return Config.LevelList[targetLevel-1];
        }
        public bool IsMaxLevel()
        {
            return Config.LevelList[^1].Level == this.Level;
        }
        
    }
    
    
    // 返回数据
    public class FusionGemResult
    {
        public List<int> DelIds { get; set; } 
        public List<PlayerFusionGem> FusionGems { get; set; }
        public ItemGroup Items { get; set; } // 同步道具
        public List<RewardItem> Rewards { get; set; } // 奖励内容
    }
    
}