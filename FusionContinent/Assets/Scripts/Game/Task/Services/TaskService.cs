using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;
using Game.RedDot;

namespace Game.Task
{
    public class TaskService : GameServiceBase
    {
        public override string ServiceName => "TaskService";

        public event Action OnTaskDataChanged;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            TaskConfigMap.Build();
        }

        public async UniTask<bool> ReceiveTaskReward(int taskId)
        {
            try
            {
                var request = new Cardwar.ReceiveTaskRewardRequest { TaskIds = { taskId } };
                var proto = await WebDataFun.SendProtoPostRequest<Cardwar.ReceiveTaskRewardResponse>(ApiKeys.ApiReceiveTaskReward, request);

                if (proto == null)
                {
                    Toast.Show("请求失败：空响应");
                    return false;
                }

                foreach (var kv in proto.Tasks)
                {
                    Global.PlayerTasks[kv.Key] = ProtoMappers.MapPlayerTask(kv.Key, kv.Value);
                }

                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                var rewards = proto.Rewards.ToDictionary(kv => kv.Key.ToString(), kv => (int)kv.Value);
                if (rewards.Count > 0)
                {
                    GlobalFunc.ShowRewardTips(rewards).Forget();
                }

                PublishTaskRedDot();
                OnTaskDataChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogError($"[TaskService] ReceiveTaskReward error: {ex.Message}");
                Toast.Show($"操作失败：{ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> ReceiveTaskChestReward(TaskType type, int activeNum)
        {
            try
            {
                var request = new Cardwar.ReceiveTaskActiveRewardRequest
                {
                    TaskType = (int)type,
                    ActiveNum = activeNum,
                };
                var proto = await WebDataFun.SendProtoPostRequest<Cardwar.ReceiveTaskActiveRewardResponse>(ApiKeys.ApiReceiveTaskActiveReward, request);

                if (proto == null)
                {
                    Toast.Show("请求失败：空响应");
                    return false;
                }

                foreach (var kv in proto.Tasks)
                {
                    Global.PlayerTaskActives[(TaskType)kv.Key] = ProtoMappers.MapPlayerTaskActiveRewards(kv.Key, kv.Value);
                }

                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                var rewards = proto.Rewards.ToDictionary(kv => kv.Key.ToString(), kv => (int)kv.Value);
                if (rewards.Count > 0)
                {
                    GlobalFunc.ShowRewardTips(rewards).Forget();
                }

                PublishTaskRedDot();
                OnTaskDataChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogError($"[TaskService] ReceiveTaskChestReward error: {ex.Message}");
                Toast.Show($"操作失败：{ex.Message}");
                return false;
            }
        }

        private void PublishTaskRedDot()
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TaskPageDayBtn, null);
        }
    }
}
