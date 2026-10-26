using System.Collections.Generic;

namespace FusionContinent.Model
{
    /// <summary>
    /// 玩家邮件，对应 proto PlayerMail。
    /// </summary>
    public class PlayerMailModel
    {
        public int Id { get; set; }
        public int MailType { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<RewardItem> Rewards { get; set; } = new List<RewardItem>();
        public bool IsRead { get; set; }
        public bool IsClaimed { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string CreateTime { get; set; }

        public bool HasRewards => Rewards.Count > 0;

        public bool HasPendingAction => !IsRead || (HasRewards && !IsClaimed);
    }
}
