using Cardwar;

namespace FusionContinent.Model
{
    public enum ChatPushTargetType
    {
        None = 0,
        Player = 1,
        Channel = 2
    }

    public class ChatPushModel
    {
        public long MsgId { get; set; }
        public long Timestamp { get; set; }
        public PlayerBaseInfoModel Sender { get; set; }
        public string Content { get; set; }
        public ChatPushTargetType TargetType { get; set; }
        public int ToPlayerId { get; set; }
        public ChatChannelType Channel { get; set; }
        public bool IsSelf { get; set; }
        public bool IsTimeMarker { get; set; }
    }

    public class ChatChannelMessageModel
    {
        public long MsgId { get; set; }
        public long Timestamp { get; set; }
        public bool IsTimeMarker { get; set; }
        public PlayerBaseInfoModel Sender { get; set; }
        public string Content { get; set; }
        public ChatChannelType Channel { get; set; }
        public bool IsSelf { get; set; }
    }
}
