using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Attribute;
using UnityEngine;

namespace FusionContinent.Model
{
    
    public class ServerData
    {
        public int Id;
        public string Url;
        public string Name;
    }
    
    [System.Serializable]
    // 属性聚合类
    public class AttributeAggregation
    {
        public AttributeSet Attributes = new AttributeSet();
        public List<MainAttributeStruct> MainAttributes = new List<MainAttributeStruct>();
        public List<AttributeStruct>  SubAttributes = new List<AttributeStruct>();
    }
    
    public class ResultData
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public LoginPlayer Data { get; set; }
    }
    
    public class LoginPlayer
    {
        public string Token { get; set; }
        public UserData User { get; set; }
        public PlayerArchive Archive { get; set; }
    }
    
    // 战斗返回数据
    public class BattleResult
    {
        public PlayerStage Stage { get; set; }
        public PlayerBattle Battle { get; set; }
        public Dictionary<string, int> Rewards { get; set; }
        public ItemGroup Items { get; set; }
        public List<PlayerCard> Cards { get; set; }
        public List<PlayerTeamModel> Teams { get; set; }
        public PlayerEndlessTower EndlessTower { get; set; }
        public PlayerChallengeBoss ChallengeBoss { get; set; }
        public PlayerHiddenRealmTower HiddenRealmTower { get; set; }
    }
    
    
    
    
    // 签到
    public class PlayerSignin
    {
        public int Id { get; set; } // 用户Id
        public int SigninNum { get; set; } // 签到天数
        public DateTime LastSinginDate { get; set; } // 最后签到日期
    }
    
    public class PlayerSinginReward
    {
        public PlayerSignin Signin { get; set; } // 签到
        public List<RewardItem> Rewards { get; set; } // 奖励内容
        public ItemGroup Items { get; set; } // 同步道具
    }
    
    public class PlayerData
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 外键，关联 User 表
        public int Gold { get; set; } // 金币
        public int Diamonds { get; set; } // 钻石
        public int CoreFragments { get; set; } // 核心碎片
        public int Stamina { get; set; } // 体力
        public int CitySkill { get; set; } // 主城技能
        public int CityEnhancementProgress { get; set; } // 强化进度
        public int Difficulty { get; set; } // 当前关卡难度
    }
    
    // public class PlayerCard
    // {
    //     public int Id { get; set; } // 主键
    //     public int UserId { get; set; } // 外键，关联 User 表
    //     public int CardId { get; set; } // 卡牌 ID
    //     public bool UnlockStatus { get; set; } // 解锁状态
    //     public int Level { get; set; } // 等级
    //     public int Fragments { get; set; } // 卡牌碎片
    // }
    
    
    public class PlayerHero
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int HeroId { get; set; } // 角色 ID
        public int Lv { get; set; } // 等级
        public int Exp { get; set; } // 等级经验
    }
    

    public class PlayerGem
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 外键，关联玩家
        public int GemId { get; set; } // 宝石ID
        public int Quality { get; set; } // 宝石品质（如：普通、稀有、史诗）
        public GemType GemType { get; set; } // 宝石类型
        public int Experience { get; set; } // 宝石经验
        public int Level { get; set; } // 宝石等级
        
        public PlayerGem(int gemUid, int experience, int level)
        {
            Id = gemUid;
            Experience = experience;
            Level = level;
        }
    }
    
    public class GemSlots
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 外键，关联玩家
        public int SlotIndex { get; set; } // 镶嵌位置
        public int GemId { get; set; } // 镶嵌宝石id
    }
    
    public class MonthlyCardStatus
    {
        public bool HasValidCard { get; set; } // 是否有有效的月卡
        public DateTime? ExpirationDate { get; set; } // 月卡到期时间，可能为空
    }
    
    // 
    public class PurchaseMonthlyCardResponse
    {
        public string Message { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
    
    public class SignInResult
    {
        public string Message { get; set; }
        public List<RewardItem> Rewards { get; set; }
    }
    
    public class SignInStatus
    {
        public bool CanSignIn { get; set; } // 是否可以签到
        public int SignInCount { get; set; } // 已签到次数
        public int RemainingDays { get; set; } // 剩余签到天数
    }
    
    public class PlayerCity
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 玩家 ID
        public int CityId { get; set; } // 城主 ID（对应策划配置表中的 ID）
        public DateTime AcquiredDate { get; set; } // 获得时间
    }
    
    public class ClaimRewardResult
    {
        public bool Success { get; set; }
        public List<RewardItem> Rewards { get; set; }
    }

    public class FirstRechargeStatus
    {
        public bool HasPurchased { get; set; }
        public int ClaimDays { get; set; }
        public bool CanClaim { get; set; }
    }

    public class BattleStartResult
    {
        
    }
}