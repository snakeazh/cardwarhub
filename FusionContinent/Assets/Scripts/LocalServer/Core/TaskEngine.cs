using System;
using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 任务引擎，对照 Server/app/service/TaskService（PlayerTaskService/PlayerTaskDailyService/PlayerTaskMainService）。
    /// 存档：Archive.Tasks（main+daily 合并的 map，key=子任务Id）、Archive.TaskActives、
    /// Archive.TaskDailyLastRefreshTime（proto 新增字段 48，unix 秒；Go 端是 player_task.last_refresh_time 字段）。
    /// 下发偏差：Go 用 GeneralParam/notices 推送任务变更（SetDailyProgress:96-107 等），单机无推送链路，
    /// 统一累积到 pendingChanges，由 ItemEntrance.Execute 收尾时填入 ResultItems.TaskDetails（field 16）。
    /// Go 的周任务、TargetType 1/5/6/7、PlayerTaskTargetTypeCache 均为死代码，不迁移。
    /// </summary>
    public class TaskEngine
    {
        private readonly LocalServerContext _ctx;

        // 摊平表（懒构建）：子任务Id → 子配置 / 所属组类型。
        // UI 层 Model/TaskConfigMap 由界面触发构建，LocalServer 不依赖它，自行摊平
        private Dictionary<int, TaskListStruct> _subTaskMap;
        private Dictionary<int, TaskType> _subTaskGroupType;

        // 本请求内变更的子任务（taskId → 最新详情），Execute 收尾消费；TryRefreshDaily 的重置变更也进这里
        private readonly Dictionary<int, TaskDetail> _pendingChanges = new Dictionary<int, TaskDetail>();

        public TaskEngine(LocalServerContext ctx)
        {
            _ctx = ctx;
        }

        // 对照 PlayerTaskDailyService.go:14-30 TryRefreshDaily
        // 口径：Go utils.TimeToDate 按自然日（本地时区）比较；与 DailyResetRunner 的 5 点重置点口径并存（保持 Go 原口径）
        public bool TryRefreshDaily()
        {
            DateTime now = DateTime.Now;
            long lastRefresh = _ctx.Store.Archive.TaskDailyLastRefreshTime;
            if (lastRefresh != 0
                && DateTimeOffset.FromUnixTimeSeconds(lastRefresh).LocalDateTime.Date == now.Date)
            {
                return false;
            }

            EnsureSubTaskMap();
            // 对照 GetNewDailyTasks（:40-50）+ ResetDailyTask（PlayerTasksModel.go:152-165）：
            // 按配置全量重建 daily 集合（State=Progress/Progress=0）。存档是 main+daily 合并 map，
            // 配置中已删除的 daily 旧 key 无法辨别类型而残留（Go 端整段 JSON 替换会清掉），无害
            foreach (var kv in _subTaskGroupType)
            {
                if (kv.Value != TaskType.Daily)
                {
                    continue;
                }
                var detail = new TaskDetail();
                _ctx.Store.Archive.Tasks[kv.Key] = detail;
                _pendingChanges[kv.Key] = detail; // 让 UI 拿到归零后的状态
            }

            // 对照 :22-24：清空日常活跃度；章节成就活跃度永不清
            _ctx.Store.Archive.TaskActives[(int)TaskType.Daily] = new ActiveDetail();

            _ctx.Store.Archive.TaskDailyLastRefreshTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            _ctx.Store.MarkDirty(PlayerArchive.TasksFieldNumber);
            _ctx.Store.MarkDirty(PlayerArchive.TaskActivesFieldNumber);
            _ctx.Store.MarkDirty(PlayerArchive.TaskDailyLastRefreshTimeFieldNumber);
            return true;
        }

        // 对照 PlayerTaskService.go:81-112 SetProgress（targetParam 对照 TargetParam1）
        public void SetProgress(TaskTargetType targetType, int progress, int targetParam, bool isCover)
        {
            // 对照 :83-84
            if (progress == 0)
            {
                return;
            }
            // 对照 :85-88
            if (progress < 0)
            {
                LogTool.LogWarning($"userId={_ctx.UserId} task cannot negative taskNum, progress={progress}");
                throw new LocalServerException("task cannot negative taskNum");
            }

            EnsureSubTaskMap();
            // 对照 :90-96：收集 TargetType 与 TargetParam1 都匹配的子任务，按组类型分发
            var dailyIds = new List<int>();
            var mainIds = new List<int>();
            foreach (var kv in _subTaskMap)
            {
                TaskListStruct sub = kv.Value;
                if (sub.TargetType != targetType || sub.TargetParam1 != targetParam)
                {
                    continue;
                }

                TaskType groupType = _subTaskGroupType[kv.Key];
                switch (groupType)
                {
                    case TaskType.Daily:
                        dailyIds.Add(kv.Key);
                        break;
                    case TaskType.ChapterAchievements:
                        mainIds.Add(kv.Key);
                        break;
                    default:
                        // 偏差：Go 此处 panic「任务类型未定义」（:106-109），但 Go 的 weekly 任务整体是死代码，
                        // 其线上扁平配置不含其他组类型、panic 不可达；新版分组配置里周任务（如 2001 消耗体力）
                        // 会匹配到 TargetType，照搬 panic 会导致战斗开始等扣体力请求整个 500，故降级为跳过。
                        LogTool.LogWarning($"userId={_ctx.UserId} 跳过未实现组类型的任务进度: taskId={kv.Key} groupType={(int)groupType}");
                        break;
                }
            }

            if (dailyIds.Count > 0)
            {
                SetDailyProgress(dailyIds, progress, isCover);
            }
            if (mainIds.Count > 0)
            {
                SetMainProgress(mainIds, progress, isCover);
            }
        }

        // 对照 PlayerTaskService.go:120-133 SetActiveProgress；返回变更后的详情供调用方下发
        public ActiveDetail AddActiveProgress(TaskType type, int progress)
        {
            TryRefreshDaily(); // :121

            ActiveDetail detail = GetOrCreateActiveDetail((int)type);
            detail.Progress += progress;
            _ctx.Store.MarkDirty(PlayerArchive.TaskActivesFieldNumber);
            return detail;
            // Go 端 AddPlayerAssetLog 流水表不迁移
        }

        // 对照 TaskCache/PlayerTaskActiveCache.go:51-72 GetActive（无记录返回空详情）+ HMSetActive 落库
        public ActiveDetail GetOrCreateActiveDetail(int taskType)
        {
            if (!_ctx.Store.Archive.TaskActives.TryGetValue(taskType, out ActiveDetail detail))
            {
                detail = new ActiveDetail();
                _ctx.Store.Archive.TaskActives[taskType] = detail;
            }
            return detail;
        }

        // 登录准备，对照 application_player_archive.go:256-259 → TaskService.GetTaskAll：
        // GetDailyTaskAll（PlayerTaskDailyService.go:33-38）内含 TryRefreshDaily；
        // GetMainTaskAll（PlayerTaskMainService.go:14-32）懒补齐配置有而存档没有的章节成就任务
        public void EnsureTasksForLogin()
        {
            TryRefreshDaily();

            EnsureSubTaskMap();
            bool changed = false;
            foreach (var kv in _subTaskGroupType)
            {
                // 只补齐 Daily/ChapterAchievements（Go 的 GetTaskAll 不管其它组类型）；
                // 每日任务刷新后自然存在，这里兜首次/配置新增的情况
                if (kv.Value != TaskType.Daily && kv.Value != TaskType.ChapterAchievements)
                {
                    continue;
                }
                if (!_ctx.Store.Archive.Tasks.ContainsKey(kv.Key))
                {
                    _ctx.Store.Archive.Tasks[kv.Key] = new TaskDetail();
                    changed = true;
                }
            }
            if (changed)
            {
                _ctx.Store.MarkDirty(PlayerArchive.TasksFieldNumber);
            }
        }

        // 摊平表查询（供 TaskHandler 按子任务Id 查配置与组类型）
        public bool TryGetSubTask(int subTaskId, out TaskListStruct subTask, out TaskType groupType)
        {
            EnsureSubTaskMap();
            if (_subTaskMap.TryGetValue(subTaskId, out subTask))
            {
                groupType = _subTaskGroupType[subTaskId];
                return true;
            }
            groupType = default;
            return false;
        }

        // 取出并清空本请求内累积的任务变更（ItemEntrance.Execute 收尾时填入 ResultItems.TaskDetails）
        public Dictionary<int, TaskDetail> ConsumePendingTaskChanges()
        {
            var changes = new Dictionary<int, TaskDetail>(_pendingChanges);
            _pendingChanges.Clear();
            return changes;
        }

        // 请求失败回滚时由 LocalApi 调用：pending 引用的是回滚前对象，直接丢弃
        // （Go 事务回滚后 GeneralParam/notices 也不会发出）
        public void ClearPendingChanges()
        {
            _pendingChanges.Clear();
        }

        // 摊平 TbTaskConfig（任务组）→ 子任务Id 映射
        private void EnsureSubTaskMap()
        {
            if (_subTaskMap != null)
            {
                return;
            }
            _subTaskMap = new Dictionary<int, TaskListStruct>();
            _subTaskGroupType = new Dictionary<int, TaskType>();
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                foreach (TaskListStruct sub in group.TaskList)
                {
                    _subTaskMap[sub.Id] = sub;
                    _subTaskGroupType[sub.Id] = group.Type;
                }
            }
        }

        // 对照 PlayerTaskDailyService.go:58-108 SetDailyProgress
        private void SetDailyProgress(List<int> taskIds, int progress, bool isCover)
        {
            TryRefreshDaily(); // :59

            bool changed = false;
            foreach (int taskId in taskIds)
            {
                // taskIds 来自摊平表（配置必然存在），Go 的 :67-71「获取任务配置失败」分支不可达
                TaskListStruct sub = _subTaskMap[taskId];
                TaskDetail detail = GetOrCreateTaskDetail(taskId);
                // 对照 :73-75：只推进进行中的任务
                if (detail.State != (int)TaskState.Progress)
                {
                    continue;
                }

                // 对照 :77-82：是否覆盖进度
                detail.Progress = isCover ? progress : detail.Progress + progress;
                // 对照 :84-86
                if (detail.Progress >= sub.TargetNum)
                {
                    detail.State = (int)TaskState.Complete;
                }

                _pendingChanges[taskId] = detail;
                changed = true;
            }
            if (changed)
            {
                _ctx.Store.MarkDirty(PlayerArchive.TasksFieldNumber);
            }
            // 偏差：Go 经 GeneralParam 推送变更（:96-107），本地累积到 pendingChanges 由 ResultItems.TaskDetails 下发
        }

        // 对照 PlayerTaskMainService.go:40-88 SetMainProgress
        private void SetMainProgress(List<int> taskIds, int progress, bool isCover)
        {
            bool changed = false;
            foreach (int taskId in taskIds)
            {
                TaskListStruct sub = _subTaskMap[taskId];
                TaskDetail detail = GetOrCreateTaskDetail(taskId);
                // 对照 :53-55
                if (detail.State != (int)TaskState.Progress)
                {
                    continue;
                }

                // 对照 :57-62
                detail.Progress = isCover ? progress : detail.Progress + progress;
                // 对照 :64-66
                if (detail.Progress >= sub.TargetNum)
                {
                    detail.State = (int)TaskState.Complete;
                }

                _pendingChanges[taskId] = detail;
                changed = true;
            }
            if (changed)
            {
                _ctx.Store.MarkDirty(PlayerArchive.TasksFieldNumber);
            }
            // 偏差：Go 经 GeneralParam 推送变更（:76-87），本地累积到 pendingChanges 由 ResultItems.TaskDetails 下发
        }

        // 存档缺失时补零值详情（State=Progress/Progress=0），对照 GetMainTaskAll 懒补齐的零值语义
        // （登录 EnsureTasksForLogin 已保证配置内任务全部存在，此处仅兜底）
        private TaskDetail GetOrCreateTaskDetail(int taskId)
        {
            if (!_ctx.Store.Archive.Tasks.TryGetValue(taskId, out TaskDetail detail))
            {
                detail = new TaskDetail();
                _ctx.Store.Archive.Tasks[taskId] = detail;
            }
            return detail;
        }
    }
}
