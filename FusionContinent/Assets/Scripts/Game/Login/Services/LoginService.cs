using System;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using Manager;
using Newtonsoft.Json;
using QFramework;
using UIFramework.Services;
using UnityEngine;
using Cardwar;


namespace Game.Login
{
    /// <summary>
    /// 登录服务 - 处理登录逻辑和数据持久化
    /// 单机化：无服务器列表/选服逻辑
    /// </summary>
    public class LoginService : GameServiceBase
    {
        public bool IsLoggedIn { get; private set; }
        public string CurrentToken { get; private set; }
        public UserData CurrentUser { get; private set; }

        // 全局登录成功事件 - 所有ViewModel都可以订阅
        public event System.Action<LoginPlayer> OnLoginSuccess;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[LoginService] 初始化完成");
        }

        /// <summary>
        /// 将版本号字符串转为整数，例如 "1.6.8.9" -> 1689（去掉分隔点后按十进制解析）。
        /// </summary>
        static int VersionConvertInt(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return 0;
            }

            var digitsOnly = version.Replace(".", string.Empty);
            return int.TryParse(digitsOnly, out var versionInt) ? versionInt : 0;
        }
        
        /// <summary>
        /// 异步登录
        /// </summary>
        public async UniTask<LoginPlayer> TryLoginAsync(string account, string password)
        {
            // try
            {
                var req = new Cardwar.LoginRequest
                {
                    Account = account,
                    Password = password,
                    Version = VersionConvertInt(Application.version)
                };

                var proto = await WebDataFun.SendProtoPostRequest<LoginResponse>(ApiKeys.ApiLogin, req);

                if (proto == null)
                {
                    return null;
                }

                IsLoggedIn = true;
                CurrentToken = proto.Token;

                // 直接从 Protobuf User 映射到本地 UserData（统一 Mapper）
                UserData localUser = ProtoMappers.MapUser(proto.User);
                if (localUser == null)
                {
                    localUser = new UserData();
                }
                CurrentUser = localUser;
                ApplyUserData(localUser);

                if (proto.Archive != null)
                {
                    // 直接应用 Protobuf 存档（不经由 JSON）
                    ArchiveModel.ApplyPlayerArchive(proto.Archive);
                }

                LogTool.Log("[LoginService] 登录成功");

                var loginPlayer = new LoginPlayer
                {
                    Token = CurrentToken,
                    User = CurrentUser,
                    Archive = null // 存档已直接应用到全局
                };

                OnLoginSuccess?.Invoke(loginPlayer);
                return loginPlayer;
            }
            // catch (System.Exception ex)
            // {
            //     LogTool.LogError($"[LoginService] 登录异常: {ex.Message} {ex.StackTrace}");
            //     Toast.Show(ex.Message);
            //     return null;
            // }
        }

        /// <summary>
        /// 过滤名字中的敏感词。
        /// </summary>
        public string FilterUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return string.Empty;
            return SensitiveCharacterTool.Filter(userName);
        }

        /// <summary>
        /// 名字是否超过长度限制。
        /// </summary>
        public bool IsUserNameTooLong(string userName, int maxLength = 10)
        {
            return !string.IsNullOrEmpty(userName) && userName.Length > maxLength;
        }

        /// <summary>
        /// 生成随机用户名。
        /// </summary>
        public string GenerateRandomUserName()
        {
            return RandomTool.RandomUserName(0);
        }

        /// <summary>
        /// 初始化默认角色外观（男性）。
        /// </summary>
        public void InitDefaultFigureData()
        {
            var playerConst = DataConfigManager.Tables.TbPlayerConst;
            Global.Player.FigureData = playerConst.InitManFigures;
        }

        /// <summary>
        /// 保存角色名并进入游戏。
        /// </summary>
        public async UniTask<(bool Success, string ErrorMessage, float TipDuration, bool IsShowTag)> SavePlayerAndEnterGameAsync(string userName, int sexType = 0)
        {
            var normalizedName = userName;
            if (normalizedName == null)
            {
                normalizedName = string.Empty;
            }
            normalizedName = normalizedName.Trim();
            if (normalizedName == string.Empty)
            {
                return (false, "名称不能为空", 3f, false);
            }

            // Global.StageId.Value = guideStageId;

            try
            {
                // 单机化：token 恒为本地登录发放的 "local"，仅判空确认已登录
                if (string.IsNullOrEmpty(CurrentToken))
                {
                    return (false, "登录状态失效，请重新登录", 3f, false);
                }

                // 使用 Protobuf 请求保存角色信息
                var req = new Cardwar.SavePlayerRequest
                {
                    UserName = normalizedName,
                    Sex = sexType,
                    Figures = JsonConvert.SerializeObject(Global.Player.FigureData)
                };

                var proto = await WebDataFun.SendProtoPostRequest<SavePlayerResponse>(ApiKeys.ApiPlayerSavePlayer, req);

                if (proto == null)
                {
                    return (false, "操作失败：保存角色信息失败", 5f, false);
                }

                // 映射并应用用户数据
                var mappedUser = FusionContinent.Utils.ProtoMappers.MapUser(proto.User);
                if (mappedUser == null)
                {
                    mappedUser = new UserData();
                }
                ApplyUserData(mappedUser);

                // 映射并设置玩家信息
                var mappedPlayer = FusionContinent.Utils.ProtoMappers.MapUserPlayer(proto.Player);
                if (mappedPlayer != null)
                {
                    Global.Player = mappedPlayer;
                }

                Global.Publisher.Publish(MessagePublisher.CHANGE_USER_NAME, mappedUser.UserName);
                return (true, "修改成功", 1f, false);
            }
            catch (Exception ex)
            {
                Toast.Show(ex.Message);
                // LogTool.LogError($"[LoginService] 创建角色并进入游戏失败: {ex.Message}");
                return (false, $"操作失败：{ex.Message}", 5f, false);
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            IsLoggedIn = false;
            CurrentToken = null;
            CurrentUser = null;
            LogTool.Log("[LoginService] 登出");
        }

        /// <summary>
        /// 检查引导状态
        /// </summary>
        public bool CheckGuideComplete(GuideType guideType)
        {
            return Global.Player.CheckGuideStatus(guideType);
        }

        private async UniTask<(bool Success, string ErrorMessage, float TipDuration, bool IsShowTag)> StartGuideBattleAndEnterSceneAsync()
        {
            try
            {
                var tcs = new UniTaskCompletionSource<string>();
                GlobalFunc.StartBattle(Global.StageId.Value, () =>
                {
                    tcs.TrySetResult(string.Empty);
                }, (err) =>
                {
                    tcs.TrySetResult(err);
                });


                var errMsg = await tcs.Task;
                if (!string.IsNullOrEmpty(errMsg))
                {
                    return (false, errMsg, 0.5f, true);
                }

                return (true, string.Empty, 0f, false);
            }
            catch (Exception ex)
            {
                LogTool.LogError($"[LoginService] 开始战斗失败: {ex.Message}");
                return (false, $"操作失败：{ex.Message}", 3f, false);
            }
        }

        

        public void ApplyUserData(UserData user)
        {
            Global.UID = user.UserId;
            Global.User = user;
        }
    }
}
