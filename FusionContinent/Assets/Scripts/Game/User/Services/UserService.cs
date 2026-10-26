using Cardwar;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework.Services;

namespace Game.User
{
    /// <summary>
    /// 用户相关服务：头像、头像框等外观更新。
    /// </summary>
    public class UserService : GameServiceBase
    {
        public async UniTask<bool> UpdateAvatarAsync(int avatarHeadConfigId)
        {
            try
            {
                var request = new UpdateAvatarRequest
                {
                    Avatar = avatarHeadConfigId,
                };
                var proto = await WebDataFun.SendProtoPostRequest<UpdateAvatarResponse>(
                    ApiKeys.ApiUserUpdateAvatar, request);
                if (proto?.User == null)
                {
                    return false;
                }

                ApplyAvatarFields(proto.User);
                Global.Publisher.Publish(MessagePublisher.CHANGE_USER_AVATAR, Global.User.Avatar);
                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[UserService] UpdateAvatarAsync error: {ex.Message}");
                throw;
            }
        }

        public async UniTask<bool> UpdateAvatarFrameAsync(int avatarFrameId)
        {
            try
            {
                var request = new UpdateAvatarFrameRequest { AvatarFrame = avatarFrameId };
                var proto = await WebDataFun.SendProtoPostRequest<UpdateAvatarFrameResponse>(
                    ApiKeys.ApiUserUpdateAvatarFrame, request);
                if (proto?.User == null)
                {
                    return false;
                }

                ApplyAvatarFields(proto.User);
                Global.Publisher.Publish(MessagePublisher.CHANGE_USER_AVATAR_FRAME, Global.User.AvatarFrame);
                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[UserService] UpdateAvatarFrameAsync error: {ex.Message}");
                throw;
            }
        }

        private static void ApplyAvatarFields(Cardwar.User user)
        {
            Global.User.Avatar = user.Avatar;
            Global.User.AvatarFrame = user.AvatarFrame;
        }
    }
}
