using System;

namespace FusionContinent.Model
{
    public enum ConditionStatus
    {
        Normal = 0,//空闲
        Achieve = 1,//完成
        Collect = 2,//领取奖励
    }
    // 关卡
    public class PlayerStage
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户Id
        public int StageId { get; set; } // 当前的关卡Id
        public int Score { get; set; } // 分数
        public int Status { get; set; } // 状态
        public int PlayCount { get; set; } // 通关次数
        public int Condition1 { get; set; } // 完成条件1
        public int Condition2 { get; set; } // 完成条件2
        public int Condition3 { get; set; } // 完成条件3
        public int ClearStageTime { get; set; } // 通关时间
    }
    
    public class PlayerChapter
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户Id
        public int ChapterId { get; set; } 
        public int UnlockStageId { get; set; } 
        public int Status { get; set; } 
    }
}