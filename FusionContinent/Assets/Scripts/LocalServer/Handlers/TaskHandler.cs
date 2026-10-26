using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 任务 Handler，逐行对照 Server/app/api/application/taskapplication/application_task.go。
    /// 存档读写对照 Server/app/model/TaskModel + cache/TaskCache（内存 Archive.Tasks/TaskActives 替代 MySQL+Redis）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案。
    /// </summary>
    public class TaskHandler
    {
        // 对照 application_task.go:27-113 ReceiveTaskReward
        public ReceiveTaskRewardResponse ReceiveTaskReward(LocalServerContext ctx, ReceiveTaskRewardRequest req)
        {
            var rewards = new Dictionary<int, long>();
            var newTasks = new Dictionary<int, TaskDetail>();

            foreach (int taskId in req.TaskIds)
            {
                // 对照 :32-36（Go 的 TbTaskConfig 是摊平表，本地按组摊平后等价）
                if (!ctx.TaskEngine.TryGetSubTask(taskId, out TaskListStruct subCfg, out TaskType groupType))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 任务配置不存在: {taskId}");
                    throw new LocalServerException($"任务配置不存在: {taskId}");
                }

                // 对照 :38-88 按组类型分发（Daily/ChapterAchievements 处理相同，仅缓存 key 不同）
                switch (groupType)
                {
                    case TaskType.Daily:
                    case TaskType.ChapterAchievements:
                        break;
                    default:
                        // 对照 :85-88（Go panic 文案逐字）
                        LogTool.LogWarning($"userId={ctx.UserId} 任务类型未定义, 无法领取: {(int)groupType}");
                        throw new LocalServerException($"任务类型未定义, 无法领取: {(int)groupType}");
                }

                // 偏差：Go 领取路径不显式刷新（靠缓存懒加载），本地为保证跨天状态正确补刷
                // （必须在置 Received 之前刷新，否则刷新会重置掉本次领取状态）
                if (groupType == TaskType.Daily)
                {
                    ctx.TaskEngine.TryRefreshDaily();
                }

                // 存档缺失按零值处理（Go 缓存查不到会解析 panic，但登录懒补齐后实际不可达）
                if (!ctx.Store.Archive.Tasks.TryGetValue(taskId, out TaskDetail detail))
                {
                    detail = new TaskDetail();
                    ctx.Store.Archive.Tasks[taskId] = detail;
                }

                // 对照 :48-51 / :71-74
                if (detail.State == (int)TaskState.Received)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 任务已领取 taskId={taskId}");
                    throw new LocalServerException("已领取");
                }
                // 对照 :53-56 / :76-79
                if (detail.State != (int)TaskState.Complete)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 任务未达成 taskId={taskId}");
                    throw new LocalServerException("未达成");
                }

                // 更改任务状态（:59 / :82）；失败由 LocalApi 快照整体回滚，等价 Go 的事务+缓存一致性
                detail.State = (int)TaskState.Received;
                newTasks[taskId] = detail;

                // 合并奖励（:91 MergeMapsAndSum64）
                foreach (var kv in ctx.ItemEntrance.RewardsToItems(subCfg.Rewards))
                {
                    rewards.TryGetValue(kv.Key, out long cur);
                    rewards[kv.Key] = cur + kv.Value;
                }
            }

            // 对照 :95 UpdateTasks（TaskDetail 为引用类型，上面已就地改存档）
            ctx.Store.MarkDirty(PlayerArchive.TasksFieldNumber);

            // 对照 :98-106：Go 事务 err 分支「领取任务奖励失败」不可达（业务失败是 panic，回滚后 re-panic 原文案），
            // 本地让 ItemEntrance 的原文案上抛；活跃度道具经 HandleTasks → TaskEngine.AddActiveProgress 自动转活跃度
            ResultItems rsItems = ctx.ItemEntrance.Execute(rewards);

            // 对照 :108-112 + task_converter（Rewards=合并后的原始奖励 map，非展开后）
            var resp = new ReceiveTaskRewardResponse { Items = rsItems };
            foreach (var kv in newTasks)
            {
                resp.Tasks[kv.Key] = kv.Value;
            }
            foreach (var kv in rewards)
            {
                resp.Rewards[kv.Key] = kv.Value;
            }
            return resp;
        }

        // 对照 application_task.go:125-182 ReceiveTaskActiveReward
        public ReceiveTaskActiveRewardResponse ReceiveTaskActiveReward(LocalServerContext ctx, ReceiveTaskActiveRewardRequest req)
        {
            // 对照 :126-130
            TaskTypeConfig typeCfg = DataConfigManager.Tables.TbTaskTypeConfig.GetOrDefault((TaskType)req.TaskType);
            if (typeCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误 taskType={req.TaskType}");
                throw new LocalServerException("参数错误");
            }

            // 对照 :132-142
            TaskActivityListStruct activeCfg = null;
            foreach (TaskActivityListStruct cfg in typeCfg.ActivityList)
            {
                if (cfg.ActiveNum == req.ActiveNum)
                {
                    activeCfg = cfg;
                }
            }
            if (activeCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误 taskType={req.TaskType} activeNum={req.ActiveNum}");
                throw new LocalServerException("参数错误");
            }

            // 偏差：Go 领取路径不显式刷新（靠缓存懒加载），本地为保证跨天状态正确补刷（同 SetActiveProgress:121 口径）
            ctx.TaskEngine.TryRefreshDaily();

            // 对照 :144 TaskCache.GetActive（无记录返回空详情）
            ActiveDetail activeDetail = ctx.TaskEngine.GetOrCreateActiveDetail(req.TaskType);

            // 对照 :145-148
            if (activeDetail.Progress < req.ActiveNum)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 活跃度未达成 taskType={req.TaskType} activeNum={req.ActiveNum} progress={activeDetail.Progress}");
                throw new LocalServerException("未达成");
            }
            // 对照 :150-153
            if (activeDetail.ReceiveRews.Contains(req.ActiveNum))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 活跃奖励已领取 taskType={req.TaskType} activeNum={req.ActiveNum}");
                throw new LocalServerException("已领取");
            }

            // 对照 :156-165：Go 事务 err 分支「领取任务活跃奖励失败」不可达，本地原文案上抛
            Dictionary<int, long> rewards = ctx.ItemEntrance.RewardsToItems(activeCfg.Rewards);
            ResultItems rsItems = ctx.ItemEntrance.Execute(rewards);

            // 设置领取状态（:168-175）；Execute 成功后才写，等价 Go 事务回滚
            activeDetail.ReceiveRews.Add(req.ActiveNum);
            ctx.Store.MarkDirty(PlayerArchive.TaskActivesFieldNumber);

            // 对照 :177-181 + task_converter
            var resp = new ReceiveTaskActiveRewardResponse { Items = rsItems };
            resp.Tasks[req.TaskType] = activeDetail;
            foreach (var kv in rewards)
            {
                resp.Rewards[kv.Key] = kv.Value;
            }
            return resp;
        }
    }
}
