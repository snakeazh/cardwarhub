using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.Challenge
{
    public class AchievementPanelViewModel : ViewModel
    {
        public BindableProperty<string> CurrentLevelName { get; } = new BindableProperty<string>("无");
        public BindableProperty<List<int>> DisplayAllAchievements { get; } = new BindableProperty<List<int>>(new List<int>());
        public BindableProperty<List<TaskActivityListStruct>> DisplayAllActiveAchievements { get; } = new BindableProperty<List<TaskActivityListStruct>>(new List<TaskActivityListStruct>());

        public Command OnReturnCommand { get; private set; }
        public Command<int> OnReceiveTaskRewardCommand { get; private set; }
        public Command<int> OnReceiveActiveRewardCommand { get; private set; }

        private AchievementService mService;
        private TbStageBattleConfig mStageBattleConfig;
        private readonly Dictionary<(int, int), StageBattleConfig> mStageCfgMap = new Dictionary<(int, int), StageBattleConfig>();

        public override void OnCreate()
        {
            base.OnCreate();

            mService = GameServiceManager.Get<AchievementService>();
            mStageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;

            BuildStageCfgMap();
            BuildAchievementLists();
            SortLists();
            RefreshCurrentStage();

            OnReturnCommand = new Command(OnReturn);
            OnReceiveTaskRewardCommand = new Command<int>(ReceiveTaskRewardAsync);
            OnReceiveActiveRewardCommand = new Command<int>(ReceiveActiveRewardAsync);
        }

        private void BuildStageCfgMap()
        {
            mStageCfgMap.Clear();
            if (mStageBattleConfig == null) return;

            foreach (var cfg in mStageBattleConfig.DataList)
            {
                var key = (cfg.ChapterId, cfg.Difficulty);
                mStageCfgMap.TryAdd(key, cfg);
            }
        }

        private void BuildAchievementLists()
        {
            var achievementIds = new List<int>();
            var allTasks = DataConfigManager.Tables.TbTaskConfig.DataList;
            foreach (var item in allTasks)
            {
                if (item.Type != TaskType.ChapterAchievements) continue;

                foreach (var sub in item.TaskList)
                {
                    achievementIds.Add(sub.Id);
                }
            }

            var activeAchievements = new List<TaskActivityListStruct>();
            var allActivityTasks = DataConfigManager.Tables.TbTaskTypeConfig.DataList;
            foreach (var item in allActivityTasks)
            {
                if (item.Type != TaskType.ChapterAchievements) continue;

                foreach (var activity in item.ActivityList)
                {
                    activeAchievements.Add(activity);
                }
            }

            DisplayAllAchievements.Value = achievementIds;
            DisplayAllActiveAchievements.Value = activeAchievements;
        }

        private void SortLists()
        {
            DisplayAllAchievements.Value.Sort();
            DisplayAllActiveAchievements.Value.Sort((a, b) => a.ActiveNum.CompareTo(b.ActiveNum));
        }

        public void RefreshAllData()
        {
            RefreshCurrentStage();
            OnRefreshList?.Invoke();
        }

       private void RefreshCurrentStage()
       {
           var clearStageId = Global.PlayerBattle.ClearStageId;
           var stageCfg = mStageBattleConfig?.GetOrDefault(clearStageId);
           CurrentLevelName.Value = stageCfg != null ? stageCfg.StageName : "无";
       }


       public ChapterAchievementItemState BuildChapterItemState(int dataIndex)
        {
            var list = DisplayAllAchievements.Value;
            if (dataIndex < 0 || dataIndex >= list.Count)
            {
                return new ChapterAchievementItemState();
            }

           var subTaskId = list[dataIndex];
            if (!TaskConfigMap.TryGetSubTask(subTaskId, out var subTaskCfg, out var taskCfg))
           {
                return new ChapterAchievementItemState
                {
                    TaskId = subTaskId,
                    Title = string.Empty,
                    Rewards = new List<RewardAndNum>(),
                    ChestIcon = "Copper",
                };
            }

            Global.PlayerTasks.TryGetValue(subTaskId, out var task);
            var taskState = task?.State ?? TaskState.Progress;
            var taskProgress = task?.Progress ?? 0;

            return new ChapterAchievementItemState
            {
                TaskId = subTaskId,
                Title = subTaskCfg.Desc,
                Rewards = subTaskCfg.Rewards,
                ProgressFill = ConvertTaskProgressToFill(taskProgress),
                CanReceive = taskState == TaskState.Complete,
                IsReceived = taskState == TaskState.Received,
                ChestIcon = taskState == TaskState.Received ? "CopperOpen" : "Copper",
            };
        }

        public ActivityAchievementItemState BuildActivityItemState(int dataIndex)
        {
            var list = DisplayAllActiveAchievements.Value;
            if (dataIndex < 0 || dataIndex >= list.Count)
            {
                return new ActivityAchievementItemState();
            }

            var data = list[dataIndex];
            Global.PlayerTaskActives.TryGetValue(TaskType.ChapterAchievements, out var active);

            bool isReceive = false;
            bool canReceive = false;
            float fill = 0f;

            if (active != null)
            {
                isReceive = active.ReceiveRews != null && active.ReceiveRews.Contains(data.ActiveNum);
                canReceive = !isReceive && active.Progress >= data.ActiveNum;

                int step = 5;
                int maxProgressForThisIndex = (dataIndex + 1) * step;
                int minProgressForThisIndex = dataIndex * step;

                if (active.Progress >= maxProgressForThisIndex)
                {
                    fill = 1f;
                }
                else if (active.Progress > minProgressForThisIndex)
                {
                    float currentVal = active.Progress - minProgressForThisIndex;
                    fill = currentVal / step;
                }
            }

            return new ActivityAchievementItemState
            {
                ActiveNum = data.ActiveNum,
                ProgressText = $"完成第{data.ActiveNum}章",
                ProgressFill = fill,
                CanReceive = canReceive,
                IsReceived = isReceive,
                ChestIcon = isReceive ? $"{data.ChestIcon}Open" : data.ChestIcon,
                Rewards = data.Rewards,
            };
        }

        private async void ReceiveTaskRewardAsync(int taskId)
        {
            if (mService == null) return;

            if (await mService.ReceiveAchievementReward(taskId))
            {
                RefreshAllData();
            }
        }

        private async void ReceiveActiveRewardAsync(int activeNum)
        {
            if (mService == null) return;

            if (await mService.ReceiveAchievementChestReward(TaskType.ChapterAchievements, activeNum))
            {
                RefreshAllData();
            }
        }

        private void OnReturn()
        {
            OnClosePanel?.Invoke();
        }

        private float ConvertTaskProgressToFill(int progress)
        {
            switch (progress)
            {
                case 0:
                    return 0f;
                case 1:
                    return 0.065f;
                case 2:
                    return 0.54f;
                case 3:
                    return 1f;
                default:
                    return progress > 3 ? 1f : 0f;
            }
        }

        public Action OnClosePanel;
        public Action OnRefreshList;
    }

    public struct ChapterAchievementItemState
    {
        public int TaskId;
        public string Title;
        public float ProgressFill;
        public bool CanReceive;
        public bool IsReceived;
        public string ChestIcon;
        public List<RewardAndNum> Rewards;
    }

    public struct ActivityAchievementItemState
    {
        public int ActiveNum;
        public string ProgressText;
        public float ProgressFill;
        public bool CanReceive;
        public bool IsReceived;
        public string ChestIcon;
        public List<RewardAndNum> Rewards;
    }
}
