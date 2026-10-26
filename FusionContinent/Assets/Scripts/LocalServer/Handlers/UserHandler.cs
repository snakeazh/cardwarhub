using Cardwar;
using Google.Protobuf;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 用户外观 Handler，逐行对照 Server/app/api/application/application_user.go
    /// UpdateAvatarHandler/UpdateAvatarFrameHandler（:92-117）。
    /// 头像/头像框持有判定对照 AvatarModel.IsOwned/IsFrameOwned（内存 Archive.Avatars/AvatarFrames）。
    /// </summary>
    public class UserHandler
    {
        // 对照 application_user.go:92-104 UpdateAvatarHandler
        // 偏差：Go 的 User.Avatar 是 string（converter 转 int32 下发），客户端 proto 已是 int32，直接比较赋值
        public UpdateAvatarResponse UpdateAvatar(LocalServerContext ctx, UpdateAvatarRequest req)
        {
            // 对照 AvatarModel.IsOwned
            if (!IsAvatarOwned(ctx, req.Avatar))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 头像[{req.Avatar}]未拥有");
                throw new LocalServerException("头像未拥有");
            }

            User user = ctx.Store.User;
            if (user.Avatar != req.Avatar)
            {
                user.Avatar = req.Avatar;
                ctx.Store.MarkUserDirty();
            }
            return new UpdateAvatarResponse { User = user };
        }

        // 对照 application_user.go:106-117 UpdateAvatarFrameHandler
        public UpdateAvatarFrameResponse UpdateAvatarFrame(LocalServerContext ctx, UpdateAvatarFrameRequest req)
        {
            // 对照 AvatarModel.IsFrameOwned
            if (!IsAvatarFrameOwned(ctx, req.AvatarFrame))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 头像框[{req.AvatarFrame}]未拥有");
                throw new LocalServerException("头像框未拥有");
            }

            User user = ctx.Store.User;
            if (user.AvatarFrame != req.AvatarFrame)
            {
                user.AvatarFrame = req.AvatarFrame;
                ctx.Store.MarkUserDirty();
            }
            return new UpdateAvatarFrameResponse { User = user };
        }

        // 对照历史 commit 5ae53b9 的 user.go SaveUserGuides（现行 Go 服务端已删除该接口，客户端无活跃调用点）：
        // guide 字符串追加到 User.Guides 逗号分隔列表，重复 panic「引导已存在」。
        // 偏差：历史接口是 form 表单 string 参数，无 proto 定义；此处借 SaveGuidesRequest.Id 承载（0 视为空参数）。
        // 历史接口 JSON 返回 guideArr 字符串列表，无对应响应 proto，返回 null（LocalApi 回空 Data）。
        public IMessage AddGuides(LocalServerContext ctx, SaveGuidesRequest req)
        {
            string guideStr = req.Id.ToString();
            if (req.Id == 0)
            {
                throw new LocalServerException("引导参数不能为空");
            }

            User user = ctx.Store.User;
            // 对照 Go strings.Split(user.Guides, ",")：空串切出 [""]，保留 Go 原口径
            var guideArr = new System.Collections.Generic.List<string>(user.Guides.Split(','));
            foreach (string v in guideArr)
            {
                if (v == guideStr)
                {
                    throw new LocalServerException("引导已存在");
                }
            }

            // 对照 Go append + strings.Join（Guides 为空时结果为 ",guide"，保留 Go 原口径）
            guideArr.Add(guideStr);
            user.Guides = string.Join(",", guideArr);
            ctx.Store.MarkUserDirty();
            return null;
        }

        // 对照 AvatarModel.IsOwned（playerAvatarModel.go:42-46）
        private bool IsAvatarOwned(LocalServerContext ctx, int avatarId)
        {
            foreach (PlayerAvatar avatar in ctx.Store.Archive.Avatars)
            {
                if (avatar.AvatarId == avatarId)
                {
                    return true;
                }
            }
            return false;
        }

        // 对照 AvatarModel.IsFrameOwned（playerAvatarFrameModel.go:42-46）
        private bool IsAvatarFrameOwned(LocalServerContext ctx, int avatarFrameId)
        {
            foreach (PlayerAvatarFrame frame in ctx.Store.Archive.AvatarFrames)
            {
                if (frame.AvatarFrameId == avatarFrameId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
