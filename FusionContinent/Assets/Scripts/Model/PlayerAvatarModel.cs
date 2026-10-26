using FusionContinent;

namespace FusionContinent.Model
{
    public class PlayerAvatar
    {
        public int Id { get; set; }
        public int AvatarId { get; set; }

        public PlayerAvatar()
        {
        }

        public PlayerAvatar(PlayerAvatar source)
        {
            Id = source.Id;
            AvatarId = source.AvatarId;
        }
    }

    public class PlayerAvatarFrame
    {
        public int Id { get; set; }
        public int AvatarFrameId { get; set; }

        public PlayerAvatarFrame()
        {
        }

        public PlayerAvatarFrame(PlayerAvatarFrame source)
        {
            Id = source.Id;
            AvatarFrameId = source.AvatarFrameId;
        }
    }

    public static class PlayerAvatarData
    {
        public static bool HasAvatar(int headConfigId)
        {
            foreach (var avatar in Global.PlayerAvatars.Values)
            {
                if (avatar.AvatarId == headConfigId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAvatarFrame(int headConfigId)
        {
            foreach (var frame in Global.PlayerAvatarFrames.Values)
            {
                if (frame.AvatarFrameId == headConfigId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
