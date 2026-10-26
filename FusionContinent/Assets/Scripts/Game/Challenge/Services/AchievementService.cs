using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;

namespace Game.Challenge
{
    /// <summary>
    /// Achievement reward compatibility layer, delegates to Game.Task.TaskService.
    /// </summary>
    public class AchievementService : GameServiceBase
    {
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
        }

        public async UniTask<bool> ReceiveAchievementReward(int taskId)
        {
            var taskService = GameServiceManager.Get<Game.Task.TaskService>();
            if (taskService == null)
            {
                LogTool.LogError("[AchievementService] TaskService is not available");
                Toast.Show("任务服务未就绪");
                return false;
            }

            return await taskService.ReceiveTaskReward(taskId);
        }

        public async UniTask<bool> ReceiveAchievementChestReward(TaskType type, int activeNum)
        {
            var taskService = GameServiceManager.Get<Game.Task.TaskService>();
            if (taskService == null)
            {
                LogTool.LogError("[AchievementService] TaskService is not available");
                Toast.Show("任务服务未就绪");
                return false;
            }

            return await taskService.ReceiveTaskChestReward(type, activeNum);
        }
    }
}
