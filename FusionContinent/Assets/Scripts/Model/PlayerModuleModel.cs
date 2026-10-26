using System;
using Cardwar;
using Config;
using FusionContinent.Utils;
using QFramework;

namespace FusionContinent.Model
{
    public class PlayerModule
    {
        public int Id { get; set; } // 自增Id
        public int UserId { get; set; } // 用户Id
        public int ModuleId { get; set; } // 模块id
        public PlayerModuleStatus Status { get; set; } // 状态：0.未解锁，1.已解锁
        public PlayerModule(PlayerModule v = null)
        {
            if (v == null) return;

            Id = v.Id;
            UserId = v.UserId;
            ModuleId = v.ModuleId;
            Status = v.Status;
        }
    }
    
    /**
     * 玩家模块状态
     */
    public enum PlayerModuleStatus
    {
        Ready    = 0, // 默认
        Unlocked = 1, // 已解锁
    }

    public class PlayerModuleModel
    {
        // 检查模块状态
        public static bool CheckModuleStatus(int moduleId, PlayerModuleStatus status = PlayerModuleStatus.Unlocked)
        {
            if (Global.PlayerModules.TryGetValue(moduleId, out var playerModule))
            {
                return playerModule.Status >= status;
            }

            return false;
        }
        
        // 设置模块状态（服务器）
        public static async void SaveModuleStatus(int moduleId, Action callback = null, PlayerModuleStatus status = PlayerModuleStatus.Unlocked)
        {
            if (CheckModuleStatus(moduleId, status)) return;
            
            try
            {
                LogTool.Log("正在请求服务器添加引导状态，请稍候...");
                var request = new SaveModuleStatusRequest
                {
                    ModuleId = moduleId,
                    Status = (int)status,
                };
                var proto = await WebDataFun.SendProtoPostRequest<SaveModuleStatusResponse>(ApiKeys.ApiModuleSaveStatus, request);

                if (proto == null || proto.Module == null)
                {
                    throw new Exception("服务器返回空响应");
                }

                // 更新本地数据
                var localModule = ProtoMappers.MapPlayerModule(proto.Module);
                Global.PlayerModules[localModule.ModuleId] = localModule;
                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"设置引导状态失败：{ex.Message} {ex.StackTrace}";
                if (GameMainPanel.Default != null) FusionContinent.Toast.Show(errorMsg);
                LogTool.LogError(errorMsg);
            }
        }
    }
}