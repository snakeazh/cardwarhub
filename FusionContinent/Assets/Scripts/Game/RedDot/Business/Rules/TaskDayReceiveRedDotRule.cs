using System.Collections.Generic;
using System.Linq;
using Config;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public sealed class TaskDayReceiveRedDotRule : RedDotRuleBase
    {
        readonly TaskType _taskType = TaskType.Daily;
        TaskTypeConfig _taskTypeConfig;
        TbTaskTypeConfig _tbTaskTypeConfig;

        public override RedNodeType NodeType => RedNodeType.MainPageTaskBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnRedTypeChange);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageTaskBtn, OnRedTypeChange);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TaskPageDayBtn, OnRedTypeChange);
            _tbTaskTypeConfig = DataConfigManager.Tables.TbTaskTypeConfig;
            _taskTypeConfig = _tbTaskTypeConfig.DataList[(int)_taskType];
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageTaskBtn, OnRedTypeChange);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TaskPageDayBtn, OnRedTypeChange);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnRedTypeChange);
        }

        void OnRedTypeChange(object obj)
        {
            Context.MarkDirty();
        }

        public override int Evaluate()
        {
            TaskType currentTaskType = _taskType;
            int conditionNum = 0;
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Task, false))
            {
                return conditionNum;
            }

            TaskTypeConfig typeCfg = _tbTaskTypeConfig.GetOrDefault(currentTaskType);
            if (typeCfg != null && Global.PlayerTaskActives.TryGetValue(currentTaskType, out PlayerTaskActiveRewards active))
            {
                List<TaskActivityListStruct> activityList = typeCfg.ActivityList;
                for (int i = 0; i < activityList.Count; i++)
                {
                    TaskActivityListStruct activeCfg = activityList[i];
                    bool isReceiveChest = active.ReceiveRews != null && active.ReceiveRews.Contains(activeCfg.ActiveNum);
                    bool canReceive = !isReceiveChest && active.Progress >= activeCfg.ActiveNum;
                    if (canReceive)
                    {
                        conditionNum += 1;
                    }
                }
            }

            List<PlayerTask> taskList = Global.PlayerTasks.Values.Where(v => v.Type == currentTaskType).ToList();
            foreach (PlayerTask task in taskList)
            {
                if (task.State == TaskState.Complete)
                {
                    conditionNum++;
                }
            }

            return conditionNum;
        }
    }
}
