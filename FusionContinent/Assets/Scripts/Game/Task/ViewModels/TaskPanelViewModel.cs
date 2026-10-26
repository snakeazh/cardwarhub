using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Cysharp.Threading.Tasks;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.Task
{
    public class TaskPanelViewModel : ViewModel
    {
        public BindableProperty<TaskType> CurrentTaskType { get; } = new BindableProperty<TaskType>(TaskType.Daily);
        public BindableProperty<int> ActivityProgress { get; } = new BindableProperty<int>(0);
        public BindableProperty<float> ActivityFillAmount { get; } = new BindableProperty<float>(0f);
        public BindableProperty<List<PlayerTask>> DisplayedTasks { get; } = new BindableProperty<List<PlayerTask>>(new List<PlayerTask>());
        public BindableProperty<List<TaskTypeConfig>> VisibleTaskTypes { get; } = new BindableProperty<List<TaskTypeConfig>>(new List<TaskTypeConfig>());

        public Command OnCloseCommand { get; private set; }
        public Command<TaskType> OnSelectTaskTypeCommand { get; private set; }
        public Command<int> OnReceiveTaskRewardCommand { get; private set; }
        public Command<int> OnReceiveChestRewardCommand { get; private set; }

        private readonly PrioritySorter<PlayerTask> mPrioritySorter = new PrioritySorter<PlayerTask>();
        private TaskService mService;
        private TbTaskTypeConfig mTaskTypeConfig;

        public override void OnCreate()
        {
            base.OnCreate();

            EnsureServiceRegistered();
            mService = GameServiceManager.Get<TaskService>();
            mTaskTypeConfig = DataConfigManager.Tables.TbTaskTypeConfig;

            OnCloseCommand = new Command(OnClose);
            OnSelectTaskTypeCommand = new Command<TaskType>(SelectTaskType);
            OnReceiveTaskRewardCommand = new Command<int>(ReceiveTaskRewardAsync);
            OnReceiveChestRewardCommand = new Command<int>(ReceiveTaskChestRewardAsync);

            if (mService != null)
            {
                mService.OnTaskDataChanged += RefreshCurrentData;
            }

            LoadVisibleTaskTypes();
            var defaultType = VisibleTaskTypes.Value.Count > 0 ? VisibleTaskTypes.Value[0].Type : TaskType.Daily;
            SelectTaskType(defaultType);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (mService != null)
            {
                mService.OnTaskDataChanged -= RefreshCurrentData;
            }
        }

        private void EnsureServiceRegistered()
        {
            if (GameServiceManager.Get<TaskService>() != null) return;
            GameServiceManager.RegisterAndInitialize(new TaskService());
        }

        private void LoadVisibleTaskTypes()
        {
            var taskTypes = mTaskTypeConfig?.DataList
                .Where(v => v.ShowPanel == ShowPanelType.DailyTask)
                .ToList() ?? new List<TaskTypeConfig>();

            VisibleTaskTypes.Value = taskTypes;
        }

        private void SelectTaskType(TaskType taskType)
        {
            CurrentTaskType.Value = taskType;
            RefreshCurrentData();
            OnPlayListAnimation?.Invoke();
        }

        private void RefreshCurrentData()
        {
            var type = CurrentTaskType.Value;
            var typeCfg = mTaskTypeConfig?.GetOrDefault(type);
            if (typeCfg == null)
            {
                ActivityProgress.Value = 0;
                ActivityFillAmount.Value = 0f;
                DisplayedTasks.Value = new List<PlayerTask>();
                OnRefreshView?.Invoke();
                return;
            }

            if (Global.PlayerTaskActives.TryGetValue(type, out var active))
            {
                ActivityProgress.Value = active.Progress;
                var maxActive = typeCfg.ActivityList != null && typeCfg.ActivityList.Count > 0
                    ? typeCfg.ActivityList.Last().ActiveNum
                    : 0;
                ActivityFillAmount.Value = maxActive > 0 ? Math.Clamp((float)active.Progress / maxActive, 0f, 1f) : 0f;
            }
            else
            {
                ActivityProgress.Value = 0;
                ActivityFillAmount.Value = 0f;
            }

            var taskList = Global.PlayerTasks.Values.Where(v => v.Type == type).ToList();
            mPrioritySorter.SetData(taskList);
            mPrioritySorter.SetScoreCalculator(CalculateScore);
            DisplayedTasks.Value = new List<PlayerTask>(mPrioritySorter.GetSortedList());

            OnRefreshView?.Invoke();
        }

        private int CalculateScore(PlayerTask task)
        {
            if (task == null || !Global.PlayerTasks.TryGetValue(task.Id, out var currentTask))
            {
                return 0;
            }

            int score = task.Id;
            switch (currentTask.State)
            {
                case TaskState.Progress:
                    score += 1000;
                    break;
                case TaskState.Complete:
                    score += 10000;
                    break;
                case TaskState.Received:
                    score += 100;
                    break;
            }

            return score;
        }

        private async void ReceiveTaskRewardAsync(int taskId)
        {
            if (mService == null) return;

            if (await mService.ReceiveTaskReward(taskId))
            {
                RefreshCurrentData();
            }
        }

        private async void ReceiveTaskChestRewardAsync(int activeNum)
        {
            if (mService == null) return;

            if (await mService.ReceiveTaskChestReward(CurrentTaskType.Value, activeNum))
            {
                RefreshCurrentData();
            }
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        public PlayerTask GetDisplayedTask(int index)
        {
            var tasks = DisplayedTasks.Value;
            if (index < 0 || index >= tasks.Count)
            {
                return null;
            }

            var taskId = tasks[index].Id;
            return Global.PlayerTasks.TryGetValue(taskId, out var task) ? task : tasks[index];
        }

        public TaskTypeConfig GetCurrentTaskTypeConfig()
        {
            return mTaskTypeConfig?.GetOrDefault(CurrentTaskType.Value);
        }

        public TaskChestState BuildChestState(int index)
        {
            var typeCfg = GetCurrentTaskTypeConfig();
            if (typeCfg == null || typeCfg.ActivityList == null || index < 0 || index >= typeCfg.ActivityList.Count)
            {
                return new TaskChestState();
            }

            var activeCfg = typeCfg.ActivityList[index];
            Global.PlayerTaskActives.TryGetValue(CurrentTaskType.Value, out var active);

            var isReceived = active?.ReceiveRews != null && active.ReceiveRews.Contains(activeCfg.ActiveNum);
            var progress = active?.Progress ?? 0;
            var canReceive = !isReceived && progress >= activeCfg.ActiveNum;

            return new TaskChestState
            {
                ActiveNum = activeCfg.ActiveNum,
                DisplayText = activeCfg.ActiveNum.ToString(),
                Icon = isReceived ? $"{activeCfg.ChestIcon}Open" : activeCfg.ChestIcon,
                CanReceive = canReceive,
                IsReceived = isReceived,
                Rewards = activeCfg.Rewards
            };
        }

        public string GetTaskButtonLocalizationKey(PlayerTask task)
        {
            if (task == null)
            {
                return "UI_BTN_GO_TO";
            }

            return task.State == TaskState.Progress ? "UI_BTN_GO_TO" : "UI_REW_NOT_CLAIMED";
        }

        public Action OnClosePanel;
        public Action OnRefreshView;
        public Action OnPlayListAnimation;
        public Action<string> OnShowErrorTip;
        public Action OnPublishTaskRedPoint;
    }

    public struct TaskChestState
    {
        public int ActiveNum;
        public string DisplayText;
        public string Icon;
        public bool CanReceive;
        public bool IsReceived;
        public List<RewardAndNum> Rewards;
    }
}
