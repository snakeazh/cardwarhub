namespace FusionContinent.Model
{
    public class PlayerChestPool
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int PoolId { get; set; } // 卡池ID
        public int PromiseNum { get; set; } 
        public int TotalChestNum { get; set; } //玩家总抽卡次数
        public int DailyChestNum { get; set; } //每日抽卡次数
    }
}