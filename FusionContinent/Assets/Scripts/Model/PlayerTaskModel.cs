using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.Model
{
    public class PlayerTask
    {
        public int Id { get; set; } // subtask config id
        public TaskType Type { get; set; } // task type
        public int Progress { get; set; } // task progress
        public TaskState State { get; set; } // task state
        public TaskConfig TaskCfg { get; set; } // parent task group config
        public TaskListStruct SubTaskCfg { get; set; } // subtask config

        public PlayerTask(int taskId, PlayerTask v)
        {
            if (taskId == 0) return;
            
            Id = taskId;
            if (!TaskConfigMap.TryGetSubTask(Id, out var subTaskCfg, out var taskCfg))
                throw new Exception($"未配置的子任务ID: {Id}");
            
            TaskCfg = taskCfg;
            SubTaskCfg = subTaskCfg;
            Type = TaskCfg.Type;
            State = v.State;
            Progress = v.Progress;
        }

        public PlayerTask()
        {
        }
    }

    public class PlayerTaskActiveRewards
    {
        public TaskType Type { get; set; }
        public int Progress { get; set; }
        public List<int> ReceiveRews { get; set; }
        public TaskTypeConfig TypeCfg { get; set; }

        public PlayerTaskActiveRewards(TaskType type, PlayerTaskActiveRewards v)
        {
            Type = type;
            if (v == null) return;
            
            TypeCfg = DataConfigManager.Tables.TbTaskTypeConfig.GetOrDefault(Type);
            if (TypeCfg == null)
                throw new Exception($"未配置的任务Type: {Type}");

            Progress = v.Progress;
            ReceiveRews = v.ReceiveRews;
        }
    }

    public class TaskResult
    {
        public Dictionary<int, PlayerTask> Tasks { get; set; }
        public Dictionary<TaskType, PlayerTaskActiveRewards> TaskActives { get; set; }
        public ItemGroup Items { get; set; }
        public Dictionary<string, int> Rewards { get; set; }
    }
}
