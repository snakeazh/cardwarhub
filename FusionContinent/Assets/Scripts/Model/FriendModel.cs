using Cardwar;

namespace FusionContinent.Model
{
    /// <summary>
    /// 玩家简要信息，对应 proto PlayerBaseInfo。
    /// </summary>
    public class PlayerBaseInfoModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Avatar { get; set; }
        public int AvatarFrame { get; set; }

        public int Level { get; set; }
        public long CombatPower { get; set; }
    }

    /// <summary>
    /// 好友条目，对应 FriendListResponse.FriendInfo。
    /// </summary>
    public class FriendInfoModel
    {
        public PlayerBaseInfoModel PlayerInfo { get; set; }
        public long FriendTime { get; set; }

        public int PlayerId => PlayerInfo.PlayerId;
    }

    /// <summary>
    /// 搜索结果条目，对应 SearchPlayerResponse.SearchPlayerInfo。
    /// </summary>
    public class SearchPlayerInfoModel
    {
        public PlayerBaseInfoModel PlayerInfo { get; set; }
        public bool IsFriend { get; set; }

        public int PlayerId => PlayerInfo.PlayerId;
    }

    /// <summary>
    /// 好友申请条目，对应 ApplyListResponse.ApplyInfo。
    /// </summary>
    public class FriendApplyInfoModel
    {
        public PlayerBaseInfoModel PlayerInfo { get; set; }
        public long ApplyTime { get; set; }
        public ApplyStatusType ApplyStatus { get; set; }
        public long ApplyId { get; set; }

        public int PlayerId => PlayerInfo.PlayerId;
        public long OperateId => ApplyId;

        public bool IsPending => ApplyStatus == ApplyStatusType.Wait;
    }
}
