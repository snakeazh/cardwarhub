using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cardwar;
using Config;
using FusionContinent.LocalServer;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.HotFixEditor
{
    /// <summary>
    /// 服务端单机化 Editor 冒烟测试（非生产代码，仅 batchmode/菜单手动执行）。
    /// 绕过 YooAsset 直接读 Res/Config/Json 加载 Luban 配置，驱动 Global.LocalApi 全链路验证：
    /// 建档/任务/天赋升级/领奖/读档/整档落盘。运行前后备份并恢复真机 save_archive.json。
    /// 执行：Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod FusionContinent.HotFixEditor.LocalServerSmokeTest.Run
    /// </summary>
    public static class LocalServerSmokeTest
    {
        private static int sPassCount;
        private static int sFailCount;
        private static readonly List<string> sFailNames = new List<string>();

        [MenuItem("Tools/LocalServer冒烟测试")]
        public static void Run()
        {
            sPassCount = 0;
            sFailCount = 0;
            sFailNames.Clear();

            string saveFile = Path.Combine(Application.persistentDataPath, "save_archive.json");
            string backupFile = null;

            Debug.Log("[SMOKE] ==== LocalServer 冒烟测试开始 ====");
            Debug.Log($"[SMOKE] persistentDataPath={Application.persistentDataPath}");

            try
            {
                // 用例1前置：备份已存在的真机存档
                if (File.Exists(saveFile))
                {
                    backupFile = saveFile + ".smoke_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    File.Move(saveFile, backupFile);
                    Debug.Log($"[SMOKE] 已备份 save_archive.json -> {backupFile}");
                }

                SetupTables();

                Case("清档建档", Case1Login);
                Case("每日登录任务", Case2DailyLoginTask);
                Case("天赋普通升级", Case3TalentNormalUpgrade);
                Case("天赋额外升级", Case4TalentExtraUpgrade);
                Case("材料不足", Case5NotEnoughMaterial);
                Case("满级", Case6MaxLevel);
                Case("任务领奖链路", Case7TaskReward);
                Case("消耗体力触发任务进度", Case7bStaminaConsumeTaskProgress);
                Case("重启读档", Case8Reload);
                Case("同日重登不触发每日重置", Case8bSameDayReloginNoDailyReset);
                Case("整档落盘", Case9WholeArchiveFlush);
                Case("旧存档缺field48字段兼容", Case10OldArchiveMissingDailyRefreshField);
                Case("英灵升级同步技能等级", Case11SoldierUpgradeSyncsSkillLevels);
                Case("老档技能等级登录迁移", Case12LoginMigratesSkillLevels);
                Case("门票登录懒更新与过期重置", Case13TicketLazyUpdateOnLogin);
            }
            catch (Exception e)
            {
                sFailCount++;
                Debug.Log($"[SMOKE] FAIL 测试框架自身异常: {e}");
            }
            finally
            {
                // 用例收尾：恢复存档（无论成败都要执行）
                try
                {
                    if (File.Exists(saveFile))
                    {
                        File.Delete(saveFile);
                    }
                    if (backupFile != null && File.Exists(backupFile))
                    {
                        File.Move(backupFile, saveFile);
                        Debug.Log($"[SMOKE] PASS 恢复存档: 已把 {Path.GetFileName(backupFile)} 恢复为 save_archive.json");
                        sPassCount++;
                    }
                    else if (backupFile == null)
                    {
                        Debug.Log("[SMOKE] PASS 恢复存档: 原本无存档，已删除测试产生的 save_archive.json");
                        sPassCount++;
                    }
                    else
                    {
                        sFailCount++;
                        sFailNames.Add("恢复存档");
                        Debug.Log($"[SMOKE] FAIL 恢复存档: 备份文件不存在 {backupFile}");
                    }
                }
                catch (Exception e)
                {
                    sFailCount++;
                    sFailNames.Add("恢复存档");
                    Debug.Log($"[SMOKE] FAIL 恢复存档: {e.Message}");
                }

                Debug.Log($"[SMOKE] ==== 汇总: PASS={sPassCount} FAIL={sFailCount}" +
                          (sFailCount > 0 ? " 失败用例: " + string.Join(",", sFailNames) : "") + " ====");
            }

            if (sFailCount > 0)
            {
                throw new Exception($"[SMOKE] 存在失败用例: {string.Join(",", sFailNames)}");
            }
        }

        private static void Case(string name, Func<string> body)
        {
            try
            {
                string detail = body();
                sPassCount++;
                Debug.Log($"[SMOKE] PASS {name}: {detail}");
            }
            catch (Exception e)
            {
                sFailCount++;
                sFailNames.Add(name);
                Debug.Log($"[SMOKE] FAIL {name}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void Check(bool cond, string detail)
        {
            if (!cond)
            {
                throw new Exception(detail);
            }
        }

        // ---------- 环境搭建 ----------

        private static void SetupTables()
        {
            string jsonDir = Path.Combine(Application.dataPath, "Res/Config/Json");
            Check(Directory.Exists(jsonDir), "配置目录不存在: " + jsonDir);

            var tables = new Tables(file =>
            {
                string path = Path.Combine(jsonDir, file + ".json");
                if (!File.Exists(path))
                {
                    throw new Exception("Tables 请求的配置 json 不存在: " + path);
                }
                return JSON.Parse(File.ReadAllText(path));
            });

            // DataConfigManager.Tables 是 private setter，反射赋值
            PropertyInfo prop = typeof(DataConfigManager).GetProperty("Tables",
                BindingFlags.Public | BindingFlags.Static);
            Check(prop != null, "DataConfigManager.Tables 属性不存在");
            MethodInfo setter = prop.GetSetMethod(true);
            Check(setter != null, "DataConfigManager.Tables setter 不存在");
            setter.Invoke(null, new object[] { tables });

            Debug.Log($"[SMOKE] 配置表加载完成 TbTalentConfig={tables.TbTalentConfig.DataList.Count} " +
                      $"TbTaskConfig={tables.TbTaskConfig.DataList.Count} TbItemConfig={tables.TbItemConfig.DataList.Count}");
        }

        private static LocalServerContext Ctx => Global.LocalServer;

        // ---------- 辅助 ----------

        private static GameResponse Dispatch(string url, Google.Protobuf.IMessage req)
        {
            return Global.LocalApi.Dispatch(url, req);
        }

        private static long GetItemNum(int itemId)
        {
            foreach (PlayerItem item in Ctx.Store.Archive.Items)
            {
                if (item.ItemId == itemId)
                {
                    return item.ItemNum;
                }
            }
            return 0;
        }

        private static PlayerTalent GetTalent(int talentId)
        {
            foreach (PlayerTalent t in Ctx.Store.Archive.Talents)
            {
                if (t.TalentId == talentId)
                {
                    return t;
                }
            }
            return null;
        }

        private static void GmAddItem(int itemId, long num)
        {
            var req = new AddItemsRequest();
            req.Items[itemId] = num;
            GameResponse resp = Dispatch(ApiKeys.ApiAddItems, req);
            Check(resp.Code == 200, $"GM AddItems({itemId},{num}) 失败: code={resp.Code} msg={resp.Msg}");
        }

        // 摊平任务配置，找每日组中指定 TargetType 的子任务
        private static int FindDailySubTask(TaskTargetType targetType)
        {
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                if (group.Type != TaskType.Daily)
                {
                    continue;
                }
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.TargetType == targetType)
                    {
                        return sub.Id;
                    }
                }
            }
            return 0;
        }

        // 第一个「1级可升且消耗为普通道具」的天赋（跳过消耗 Resource/Exp 模块的，保证扣减断言可靠）
        private static TalentConfig FindTestTalent()
        {
            foreach (TalentConfig cfg in DataConfigManager.Tables.TbTalentConfig.DataList)
            {
                if (cfg.TalentList.Count < 2)
                {
                    continue;
                }
                TalentListStruct lv1 = cfg.TalentList[0];
                if (lv1.UnlockLv > 1 || lv1.ConsumeItems.Count == 0)
                {
                    continue;
                }
                bool allPlain = true;
                foreach (Config.RewardAndNum r in lv1.ConsumeItems)
                {
                    ItemConfig itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(r.Id);
                    if (itemCfg == null || itemCfg.ModuleType == ModuleType.Resource
                        || itemCfg.ModuleType == ModuleType.Exp || itemCfg.ModuleType == ModuleType.Card
                        || itemCfg.ModuleType == ModuleType.Equip || itemCfg.ModuleType == ModuleType.Task)
                    {
                        allPlain = false;
                        break;
                    }
                }
                if (allPlain)
                {
                    return cfg;
                }
            }
            return null;
        }

        // ---------- 用例 ----------

        private static string Case1Login()
        {
            string saveFile = Path.Combine(Application.persistentDataPath, "save_archive.json");
            Check(!File.Exists(saveFile), "清档后 save_archive.json 仍存在（备份流程异常）");

            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"登录失败 code={resp.Code} msg={resp.Msg}");

            LoginResponse login = LoginResponse.Parser.ParseFrom(resp.Data);
            Check(login.Archive != null, "LoginResponse.Archive 为空");
            Check(login.User != null && login.User.UserId > 0, $"UserId 异常: {login.User?.UserId}");
            // 对照 user.go:207-209：新档首次登录应全量刷新战力（RefreshAll），新档有初始卡牌/天赋，战力应非 0
            Check(login.User.CombatPower > 0, $"新档首次登录战力未刷新: CombatPower={login.User.CombatPower}（RefreshAll 应已生效）");
            Check(Ctx.Store.Loaded, "Store 未标记 Loaded");
            Check(File.Exists(saveFile), "登录落盘后 save_archive.json 不存在");

            return $"userId={login.User.UserId} account={login.User.Account} cards={login.Archive.Cards.Count} talents={login.Archive.Talents.Count} combatPower={login.User.CombatPower}";
        }

        private static string Case2DailyLoginTask()
        {
            int taskId = FindDailySubTask(TaskTargetType.DailyLoginToGames);
            Check(taskId != 0, "配置中找不到 DailyLoginToGames 的每日子任务");
            Check(Ctx.Store.Archive.Tasks.TryGetValue(taskId, out TaskDetail detail), $"存档缺任务 {taskId}");
            Check(detail.Progress >= 1, $"每日登录任务进度异常 progress={detail.Progress}（新档当日 Go 原口径可能 +2，>=1 即可）");
            return $"taskId={taskId} progress={detail.Progress} state={detail.State}";
        }

        private static string Case3TalentNormalUpgrade()
        {
            TalentConfig cfg = FindTestTalent();
            Check(cfg != null, "找不到 1 级可升且消耗普通道具的天赋");
            TalentListStruct lv1 = cfg.TalentList[0];

            // GM 备足 1 级材料（多备 10 个供后续用例扣减）
            foreach (Config.RewardAndNum r in lv1.ConsumeItems)
            {
                GmAddItem(r.Id, r.Num + 10);
            }

            PlayerTalent talent = GetTalent(cfg.Id);
            int lvBefore = talent != null ? talent.Lv : 0;
            var before = new Dictionary<int, long>();
            foreach (Config.RewardAndNum r in lv1.ConsumeItems)
            {
                before[r.Id] = GetItemNum(r.Id);
            }

            GameResponse resp = Dispatch(ApiKeys.ApiUpGradeTalent, new UpGradeTalentRequest { Id = cfg.Id, UpgradeExtra = 0 });
            Check(resp.Code == 200, $"升级失败 code={resp.Code} msg={resp.Msg}");

            talent = GetTalent(cfg.Id);
            Check(talent != null && talent.Lv == lvBefore + 1, $"Lv 未+1: before={lvBefore} after={(talent != null ? talent.Lv : -1)}");

            var deltas = new List<string>();
            foreach (Config.RewardAndNum r in lv1.ConsumeItems)
            {
                long after = GetItemNum(r.Id);
                Check(before[r.Id] - after == r.Num, $"材料 {r.Id} 扣减不符: before={before[r.Id]} after={after} 应扣={r.Num}");
                deltas.Add($"{r.Id}:{before[r.Id]}->{after}");
            }
            return $"talentId={cfg.Id} Lv {lvBefore}->{lvBefore + 1} 消耗 {string.Join(" ", deltas)}";
        }

        private static string Case4TalentExtraUpgrade()
        {
            TalentConfig cfg = FindTestTalent();
            TalentListStruct extraCfg = null;
            foreach (TalentListStruct lv in cfg.TalentList)
            {
                if (lv.ExtraConsumeItems.Count > 0)
                {
                    extraCfg = lv;
                    break;
                }
            }
            Check(extraCfg != null, $"天赋 {cfg.Id} 没有任何 ExtraConsumeItems 档位，无法验证额外升级");

            foreach (Config.RewardAndNum r in extraCfg.ExtraConsumeItems)
            {
                GmAddItem(r.Id, r.Num);
            }

            PlayerTalent talent = GetTalent(cfg.Id);
            int extraBefore = talent.ExtraLv;
            var before = new Dictionary<int, long>();
            foreach (Config.RewardAndNum r in extraCfg.ExtraConsumeItems)
            {
                before[r.Id] = GetItemNum(r.Id);
            }

            GameResponse resp = Dispatch(ApiKeys.ApiUpGradeTalent, new UpGradeTalentRequest { Id = cfg.Id, UpgradeExtra = 1 });
            Check(resp.Code == 200, $"额外升级失败 code={resp.Code} msg={resp.Msg}");

            talent = GetTalent(cfg.Id);
            // TalentHandler 语义：ExtraLv 跳级赋值为该档 Level（非 +1）
            Check(talent.ExtraLv == extraCfg.Level, $"ExtraLv 跳级不符: before={extraBefore} after={talent.ExtraLv} 期望档位 Level={extraCfg.Level}");

            var deltas = new List<string>();
            foreach (Config.RewardAndNum r in extraCfg.ExtraConsumeItems)
            {
                long after = GetItemNum(r.Id);
                Check(before[r.Id] - after == r.Num, $"额外材料 {r.Id} 扣减不符: before={before[r.Id]} after={after} 应扣={r.Num}");
                deltas.Add($"{r.Id}:{before[r.Id]}->{after}");
            }
            return $"talentId={cfg.Id} ExtraLv {extraBefore}->{talent.ExtraLv}（跳级到档位 Level={extraCfg.Level}）消耗 {string.Join(" ", deltas)}";
        }

        private static string Case5NotEnoughMaterial()
        {
            TalentConfig cfg = FindTestTalent();
            PlayerTalent talent = GetTalent(cfg.Id);
            TalentListStruct nextLv = cfg.TalentList[talent.Lv];

            // 通过 GM 负数把消耗材料清零，保证必然不足
            foreach (Config.RewardAndNum r in nextLv.ConsumeItems)
            {
                long cur = GetItemNum(r.Id);
                if (cur > 0)
                {
                    GmAddItem(r.Id, -cur);
                }
            }

            GameResponse resp = Dispatch(ApiKeys.ApiUpGradeTalent, new UpGradeTalentRequest { Id = cfg.Id, UpgradeExtra = 0 });
            Check(resp.Code == 500, $"材料不足应返回 500，实际 code={resp.Code}");
            Check(resp.Msg == "道具不足", $"材料不足文案不符: \"{resp.Msg}\"（TalentHandler 抛出的是「道具不足」）");
            Check(GetTalent(cfg.Id).Lv == talent.Lv, "升级失败但 Lv 发生了变化（回滚失效）");
            return $"talentId={cfg.Id} 目标 Lv{talent.Lv + 1} code={resp.Code} msg=\"{resp.Msg}\"";
        }

        private static string Case6MaxLevel()
        {
            TalentConfig cfg = FindTestTalent();
            PlayerTalent talent = GetTalent(cfg.Id);
            int maxLv = cfg.TalentList.Count;

            // 直接把内存存档推到满级前一级（测试数据准备，不改生产代码）
            talent.Lv = maxLv - 1;

            TalentListStruct lastLv = cfg.TalentList[maxLv - 1];
            foreach (Config.RewardAndNum r in lastLv.ConsumeItems)
            {
                long cur = GetItemNum(r.Id);
                if (cur < r.Num)
                {
                    GmAddItem(r.Id, r.Num - cur);
                }
            }

            GameResponse resp = Dispatch(ApiKeys.ApiUpGradeTalent, new UpGradeTalentRequest { Id = cfg.Id, UpgradeExtra = 0 });
            Check(resp.Code == 200, $"升到满级失败 code={resp.Code} msg={resp.Msg}");
            Check(GetTalent(cfg.Id).Lv == maxLv, $"未满级: Lv={GetTalent(cfg.Id).Lv} max={maxLv}");

            GameResponse resp2 = Dispatch(ApiKeys.ApiUpGradeTalent, new UpGradeTalentRequest { Id = cfg.Id, UpgradeExtra = 0 });
            Check(resp2.Code == 500, $"满级后再升应返回 500，实际 code={resp2.Code}");
            Check(resp2.Msg == "已满级", $"满级文案不符: \"{resp2.Msg}\"");
            return $"talentId={cfg.Id} 满级 Lv={maxLv}，再升 code={resp2.Code} msg=\"{resp2.Msg}\"";
        }

        private static string Case7TaskReward()
        {
            var details = new List<string>();

            // 7.1 领取登录后已达成的每日登录任务
            int loginTaskId = FindDailySubTask(TaskTargetType.DailyLoginToGames);
            long goldBefore = GetItemNum(1010001);
            var receiveReq = new ReceiveTaskRewardRequest();
            receiveReq.TaskIds.Add(loginTaskId);
            GameResponse resp = Dispatch(ApiKeys.ApiReceiveTaskReward, receiveReq);
            Check(resp.Code == 200, $"领取任务 {loginTaskId} 失败 code={resp.Code} msg={resp.Msg}");
            Check(Ctx.Store.Archive.Tasks[loginTaskId].State == (int)TaskState.Received,
                $"任务 {loginTaskId} 状态未置 Received: state={Ctx.Store.Archive.Tasks[loginTaskId].State}");
            long goldGain = GetItemNum(1010001) - goldBefore;
            Check(goldGain > 0, "任务奖励未入档（金币未增加）");
            details.Add($"领取任务{loginTaskId} 金币+{goldGain}");

            // 7.2 重复领取
            var repeatReq = new ReceiveTaskRewardRequest();
            repeatReq.TaskIds.Add(loginTaskId);
            GameResponse respRepeat = Dispatch(ApiKeys.ApiReceiveTaskReward, repeatReq);
            Check(respRepeat.Code == 500 && respRepeat.Msg == "已领取",
                $"重复领取应 500「已领取」，实际 code={respRepeat.Code} msg=\"{respRepeat.Msg}\"");
            details.Add("重复领取拒绝OK");

            // 7.3 未达成任务领取
            int undoneTaskId = FindDailySubTask(TaskTargetType.ConsumeSpecifyItemId);
            Check(undoneTaskId != 0, "找不到 ConsumeSpecifyItemId 每日任务");
            Check(Ctx.Store.Archive.Tasks[undoneTaskId].State == (int)TaskState.Progress,
                $"任务 {undoneTaskId} 预期进行中，实际 state={Ctx.Store.Archive.Tasks[undoneTaskId].State}");
            var undoneReq = new ReceiveTaskRewardRequest();
            undoneReq.TaskIds.Add(undoneTaskId);
            GameResponse respUndone = Dispatch(ApiKeys.ApiReceiveTaskReward, undoneReq);
            Check(respUndone.Code == 500 && respUndone.Msg == "未达成",
                $"未达成领取应 500「未达成」，实际 code={respUndone.Code} msg=\"{respUndone.Msg}\"");
            details.Add("未达成拒绝OK");

            // 7.4 TaskEngine.SetProgress 推达标一个章节成就任务（引擎单元化验证）
            // 选 CompleteAllDifficulties(param1=1)：摊平后仅命中章节成就组，避开 Weekly 组「任务类型未定义」
            int mainTaskId = 0;
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                if (group.Type != TaskType.ChapterAchievements)
                {
                    continue;
                }
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.TargetType == TaskTargetType.CompleteAllDifficulties && sub.TargetParam1 == 1)
                    {
                        mainTaskId = sub.Id;
                    }
                }
            }
            Check(mainTaskId != 0, "找不到 CompleteAllDifficulties(param1=1) 章节成就任务");
            Ctx.TaskEngine.SetProgress(TaskTargetType.CompleteAllDifficulties, 3, 1, true);
            Check(Ctx.Store.Archive.Tasks[mainTaskId].State == (int)TaskState.Complete,
                $"SetProgress 后任务 {mainTaskId} 未达成: state={Ctx.Store.Archive.Tasks[mainTaskId].State}");
            var mainReq = new ReceiveTaskRewardRequest();
            mainReq.TaskIds.Add(mainTaskId);
            GameResponse respMain = Dispatch(ApiKeys.ApiReceiveTaskReward, mainReq);
            Check(respMain.Code == 200, $"领取章节成就任务 {mainTaskId} 失败 code={respMain.Code} msg={respMain.Msg}");
            Check(Ctx.Store.Archive.Tasks[mainTaskId].State == (int)TaskState.Received,
                $"任务 {mainTaskId} 未置 Received");
            details.Add($"SetProgress推达标+领取任务{mainTaskId} OK");

            // 7.5 活跃度宝箱：推 Daily 活跃度到第一档
            TaskTypeConfig dailyType = DataConfigManager.Tables.TbTaskTypeConfig.GetOrDefault(TaskType.Daily);
            Check(dailyType != null && dailyType.ActivityList.Count >= 2, "TbTaskTypeConfig 缺 Daily 活跃档位");
            TaskActivityListStruct tier1 = dailyType.ActivityList[0];
            TaskActivityListStruct tier2 = dailyType.ActivityList[1];

            Ctx.TaskEngine.AddActiveProgress(TaskType.Daily, tier1.ActiveNum);
            GameResponse respActive = Dispatch(ApiKeys.ApiReceiveTaskActiveReward,
                new ReceiveTaskActiveRewardRequest { TaskType = (int)TaskType.Daily, ActiveNum = tier1.ActiveNum });
            Check(respActive.Code == 200, $"领取活跃宝箱失败 code={respActive.Code} msg={respActive.Msg}");
            ActiveDetail active = Ctx.Store.Archive.TaskActives[(int)TaskType.Daily];
            Check(active.ReceiveRews.Contains(tier1.ActiveNum), $"ReceiveRews 未记录 {tier1.ActiveNum}");
            details.Add($"活跃度{tier1.ActiveNum}宝箱领取OK ReceiveRews=[{string.Join(",", active.ReceiveRews)}]");

            // 7.6 活跃宝箱重复领取
            GameResponse respActive2 = Dispatch(ApiKeys.ApiReceiveTaskActiveReward,
                new ReceiveTaskActiveRewardRequest { TaskType = (int)TaskType.Daily, ActiveNum = tier1.ActiveNum });
            Check(respActive2.Code == 500 && respActive2.Msg == "已领取",
                $"活跃宝箱重复领取应 500「已领取」，实际 code={respActive2.Code} msg=\"{respActive2.Msg}\"");
            // 7.7 活跃宝箱未达成
            GameResponse respActive3 = Dispatch(ApiKeys.ApiReceiveTaskActiveReward,
                new ReceiveTaskActiveRewardRequest { TaskType = (int)TaskType.Daily, ActiveNum = tier2.ActiveNum });
            Check(respActive3.Code == 500 && respActive3.Msg == "未达成",
                $"活跃宝箱未达成应 500「未达成」，实际 code={respActive3.Code} msg=\"{respActive3.Msg}\"");
            details.Add("活跃宝箱重复/未达成拒绝OK");

            return string.Join("；", details);
        }

        private static string Case7bStaminaConsumeTaskProgress()
        {
            // 回归：旧代码 TaskEngine.SetProgress 命中未实现组类型（Weekly）会抛 LocalServerException，
            // 消耗体力（1010006）同时匹配每日 1002 / 周任务 2001 / 主线 3001-3003，导致整个请求 500。
            // 修复后应降级为跳过：Execute 不抛异常、每日任务正常推进、周任务被跳过不入档。
            const int staminaItemId = 1010006;
            ItemConfig staminaCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(staminaItemId);
            Check(staminaCfg != null && staminaCfg.ModuleType == ModuleType.Resource
                  && staminaCfg.Type == ItemType.Stamina,
                "1010006 不是 Resource/Stamina 体力道具，用例前提不成立");

            int dailyTaskId = 0;
            int weeklyTaskId = 0;
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.TargetType != TaskTargetType.ConsumeSpecifyItemId || sub.TargetParam1 != staminaItemId)
                    {
                        continue;
                    }
                    if (group.Type == TaskType.Daily)
                    {
                        dailyTaskId = sub.Id;
                    }
                    else if (group.Type == TaskType.Weekly)
                    {
                        weeklyTaskId = sub.Id;
                    }
                }
            }
            Check(dailyTaskId != 0 && weeklyTaskId != 0,
                $"配置中找不到匹配 ConsumeSpecifyItemId+{staminaItemId} 的每日/每周任务（回归前提不成立）: daily={dailyTaskId} weekly={weeklyTaskId}");

            // 确保体力足够（新档满体力，正常不会走到 GM 补足分支）
            int dailyCfgTargetNum = 0;
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                if (group.Type != TaskType.Daily)
                {
                    continue;
                }
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.Id == dailyTaskId)
                    {
                        dailyCfgTargetNum = sub.TargetNum;
                    }
                }
            }
            int consume = dailyCfgTargetNum; // 正好推到达标
            if (Ctx.Store.Archive.Resource.Stamina < consume)
            {
                GmAddItem(staminaItemId, consume - Ctx.Store.Archive.Resource.Stamina);
            }
            int staminaBefore = Ctx.Store.Archive.Resource.Stamina;
            int dailyProgressBefore = Ctx.Store.Archive.Tasks[dailyTaskId].Progress;
            Check(!Ctx.Store.Archive.Tasks.ContainsKey(weeklyTaskId), $"周任务 {weeklyTaskId} 不应建档（Weekly 组不做懒补齐）");

            // 直接调 ItemEntrance.Execute（不走 Dispatch）：旧代码此处抛 LocalServerException → LocalApi 转成 500
            var consumeItems = new Dictionary<int, long> { { staminaItemId, -consume } };
            ResultItems rs = Ctx.ItemEntrance.Execute(consumeItems);
            Check(rs != null, "ItemEntrance.Execute 返回 null");

            int staminaAfter = Ctx.Store.Archive.Resource.Stamina;
            Check(staminaAfter == staminaBefore - consume, $"体力扣减不符: before={staminaBefore} after={staminaAfter} 应扣={consume}");

            TaskDetail dailyDetail = Ctx.Store.Archive.Tasks[dailyTaskId];
            Check(dailyDetail.Progress == dailyProgressBefore + consume,
                $"每日任务 {dailyTaskId} 进度未累加: before={dailyProgressBefore} after={dailyDetail.Progress} 应+{consume}");
            Check(dailyDetail.State == (int)TaskState.Complete,
                $"每日任务 {dailyTaskId} 应达成: progress={dailyDetail.Progress} targetNum={dailyCfgTargetNum} state={dailyDetail.State}");

            // 周任务被跳过：不入档、不出现在结果下发里
            Check(!Ctx.Store.Archive.Tasks.ContainsKey(weeklyTaskId), $"周任务 {weeklyTaskId} 被错误建档/推进");
            Check(!rs.TaskDetails.ContainsKey(weeklyTaskId), $"周任务 {weeklyTaskId} 出现在 ResultItems.TaskDetails 下发中");

            // 同 TargetType 的主线任务（3001-3003）不受影响，正常累加
            int mainProgressed = 0;
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                if (group.Type != TaskType.ChapterAchievements)
                {
                    continue;
                }
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.TargetType == TaskTargetType.ConsumeSpecifyItemId && sub.TargetParam1 == staminaItemId
                        && Ctx.Store.Archive.Tasks.TryGetValue(sub.Id, out TaskDetail d) && d.Progress >= consume)
                    {
                        mainProgressed++;
                    }
                }
            }

            return $"消耗体力{consume}（{staminaBefore}->{staminaAfter}）每日任务{dailyTaskId} 进度 {dailyProgressBefore}->{dailyDetail.Progress} 达成；" +
                   $"周任务{weeklyTaskId} 跳过未入档；主线任务推进 {mainProgressed} 个";
        }

        private static int sCheckTalentId;
        private static string Case8Reload()
        {
            TalentConfig cfg = FindTestTalent();
            sCheckTalentId = cfg.Id;
            PlayerTalent talent = GetTalent(cfg.Id);
            int lvMem = talent.Lv;
            int extraMem = talent.ExtraLv;
            long matMem = GetItemNum(cfg.TalentList[0].ConsumeItems[0].Id);
            long goldMem = GetItemNum(1010001);
            int loginTaskId = FindDailySubTask(TaskTargetType.DailyLoginToGames);
            int taskStateMem = Ctx.Store.Archive.Tasks[loginTaskId].State;
            int activeRewsMem = Ctx.Store.Archive.TaskActives[(int)TaskType.Daily].ReceiveRews.Count;

            Ctx.Store.Flush();

            // 全新上下文重建（模拟重启进程）
            Global.LocalServer = new LocalServerContext();
            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"重登失败 code={resp.Code} msg={resp.Msg}");

            PlayerTalent loaded = GetTalent(cfg.Id);
            Check(loaded != null && loaded.Lv == lvMem, $"天赋 Lv 读档不一致: 内存={lvMem} 读档={(loaded != null ? loaded.Lv : -1)}");
            Check(loaded.ExtraLv == extraMem, $"天赋 ExtraLv 读档不一致: 内存={extraMem} 读档={loaded.ExtraLv}");
            long matLoaded = GetItemNum(cfg.TalentList[0].ConsumeItems[0].Id);
            Check(matLoaded == matMem, $"材料读档不一致: 内存={matMem} 读档={matLoaded}");
            long goldLoaded = GetItemNum(1010001);
            Check(goldLoaded == goldMem, $"金币读档不一致: 内存={goldMem} 读档={goldLoaded}");
            int taskStateLoaded = Ctx.Store.Archive.Tasks[loginTaskId].State;
            Check(taskStateLoaded == taskStateMem, $"任务状态读档不一致: 内存={taskStateMem} 读档={taskStateLoaded}");
            int activeRewsLoaded = Ctx.Store.Archive.TaskActives[(int)TaskType.Daily].ReceiveRews.Count;
            Check(activeRewsLoaded == activeRewsMem, $"活跃领取记录读档不一致: 内存={activeRewsMem} 读档={activeRewsLoaded}");

            return $"天赋Lv={lvMem} ExtraLv={extraMem} 材料={matLoaded} 金币={goldLoaded} 任务状态={taskStateLoaded} 活跃领取数={activeRewsLoaded}（均与内存一致）";
        }

        // 回归：GetDayInterval 曾有恒真的 +1 天修正，导致 DailyResetRunner.NeedReset 每次重登都为真，
        // 每日扫荡次数/每日双倍奖励次数等每日字段每次重登都被清零。修复后同日重登 GetDayInterval=0，
        // DailyResetRunner 不执行：重登前置为非零的每日字段与每日登录任务进度必须保持不变。
        private static string Case8bSameDayReloginNoDailyReset()
        {
            PlayerBattle battle = Ctx.Store.Archive.Battle;
            Check(battle != null, "存档 Battle 为空，用例前提不成立（建档应已初始化）");

            // 造标记：每日字段置非零（DailyResetRunner.ResetBattle 会清零）；记录每日登录任务进度
            // （DailyResetRunner.Run 会 +1，该任务 Case7 已 Received，进度仍能反映重置是否执行）
            battle.DailyStageSweepingNum = 3;
            battle.DailyDoubleRewardNum = 2;
            Ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);
            int loginTaskId = FindDailySubTask(TaskTargetType.DailyLoginToGames);
            int loginProgressBefore = Ctx.Store.Archive.Tasks[loginTaskId].Progress;

            Ctx.Store.Flush();

            // 全新上下文重建 + 同日重登（模拟重启进程）
            Global.LocalServer = new LocalServerContext();
            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"重登失败 code={resp.Code} msg={resp.Msg}");

            PlayerBattle loaded = Ctx.Store.Archive.Battle;
            Check(loaded != null && loaded.DailyStageSweepingNum == 3,
                $"同日重登后 DailyStageSweepingNum 被重置: 读档={(loaded != null ? loaded.DailyStageSweepingNum : -1)} 预期=3（DailyResetRunner 不应执行）");
            Check(loaded.DailyDoubleRewardNum == 2,
                $"同日重登后 DailyDoubleRewardNum 被重置: 读档={loaded.DailyDoubleRewardNum} 预期=2（DailyResetRunner 不应执行）");
            int loginProgressAfter = Ctx.Store.Archive.Tasks[loginTaskId].Progress;
            Check(loginProgressAfter == loginProgressBefore,
                $"同日重登后每日登录任务 {loginTaskId} 进度被推进: before={loginProgressBefore} after={loginProgressAfter}（DailyResetRunner 不应执行）");

            return $"同日重登后 DailyStageSweepingNum=3 DailyDoubleRewardNum=2 每日登录进度={loginProgressAfter}（均未变，DailyResetRunner 未执行）";
        }

        private static string Case9WholeArchiveFlush()
        {
            string saveFile = Path.Combine(Application.persistentDataPath, "save_archive.json");
            Check(File.Exists(saveFile), $"存档文件不存在: {saveFile}");
            string beforeText = File.ReadAllText(saveFile);

            // 只改天赋的小操作：翻转天赋使用中状态（UseTalent 只 MarkDirty(Talents)），整档方案下应全量重写单文件
            PlayerTalent talent = GetTalent(sCheckTalentId);
            int newStatus = talent.Status == 1 ? 0 : 1;
            var useReq = new UseTalentRequest { Id = sCheckTalentId, Status = newStatus };
            GameResponse resp = Dispatch(ApiKeys.ApiTalentUse, useReq);
            Check(resp.Code == 200, $"UseTalent 失败 code={resp.Code} msg={resp.Msg}");

            string afterText = File.ReadAllText(saveFile);
            Check(afterText != beforeText, "UseTalent 后存档文件内容未变化（整档未重写）");

            // 落盘内容必须反映改动：JSON 中该天赋的 status 与内存一致（status=0 时 proto JSON 省略该字段）
            JSONNode archiveNode = JSON.Parse(afterText)["archive"];
            Check(archiveNode != null, "落盘 JSON 缺 archive 节点");
            bool found = false;
            foreach (JSONNode node in archiveNode["talents"].AsArray)
            {
                if (node["talentId"].AsInt == sCheckTalentId)
                {
                    found = true;
                    int statusInFile = node["status"] == null ? 0 : node["status"].AsInt;
                    Check(statusInFile == newStatus,
                        $"落盘 JSON 中天赋 {sCheckTalentId} status={statusInFile} 与内存不符（预期 {newStatus}）");
                }
            }
            Check(found, $"落盘 JSON 中找不到天赋 {sCheckTalentId}");

            return $"save_archive.json 已整档重写，天赋 {sCheckTalentId} status={newStatus} 与内存一致";
        }

        // 旧存档兼容：task_daily_last_refresh_time(field 48) 是后加字段，旧档的整档 JSON 中没有该键
        // （proto JSON 默认值本就会省略）。从 save_archive.json 移除该键模拟旧档，验证：字段缺席 =
        // proto 默认值 0，TryRefreshDaily 视为从未刷新，登录即触发一次日刷新，任务系统状态正确。
        private static string Case10OldArchiveMissingDailyRefreshField()
        {
            string saveFile = Path.Combine(Application.persistentDataPath, "save_archive.json");

            Ctx.Store.Flush(); // 确保前面用例的改动全部落盘

            // 章节成就任务跨刷新保持（Case7 已领取），用于验证日刷新只重建 daily 组
            int mainTaskId = 0;
            foreach (TaskConfig group in DataConfigManager.Tables.TbTaskConfig.DataList)
            {
                if (group.Type != TaskType.ChapterAchievements)
                {
                    continue;
                }
                foreach (TaskListStruct sub in group.TaskList)
                {
                    if (sub.TargetType == TaskTargetType.CompleteAllDifficulties && sub.TargetParam1 == 1)
                    {
                        mainTaskId = sub.Id;
                    }
                }
            }
            Check(mainTaskId != 0, "找不到 CompleteAllDifficulties(param1=1) 章节成就任务");

            // 模拟旧档：整档 JSON 的 archive 节点移除 taskDailyLastRefreshTime 键
            JSONNode root = JSON.Parse(File.ReadAllText(saveFile));
            JSONNode archiveNode = root["archive"];
            Check(archiveNode != null, "存档 JSON 缺 archive 节点");
            Check(archiveNode["taskDailyLastRefreshTime"] != null, "用例前提不成立：存档 JSON 本就不含 taskDailyLastRefreshTime");
            archiveNode.AsObject.Remove("taskDailyLastRefreshTime");
            File.WriteAllText(saveFile, root.ToString(), new System.Text.UTF8Encoding(false));

            // 重建上下文模拟重启进程，读「旧档」
            Global.LocalServer = new LocalServerContext();
            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"旧档重登失败 code={resp.Code} msg={resp.Msg}");

            // 字段缺席 → TaskDailyLastRefreshTime=0 → TryRefreshDaily 立即触发一次刷新并写回当前时间
            long refreshTime = Ctx.Store.Archive.TaskDailyLastRefreshTime;
            Check(refreshTime > 0, "缺失 field 48 读档后未触发每日刷新（TaskDailyLastRefreshTime 仍为 0）");

            // 每日任务已按配置全量重建（Case7 已领取的登录任务不再是 Received）；日常活跃度清空。
            // GetDayInterval 修复后同日重登不再跑 DailyResetRunner（不会 +1 每日登录进度），
            // 重建后的登录任务必为进行中（Progress）
            int dailyTaskId = FindDailySubTask(TaskTargetType.DailyLoginToGames);
            Check(dailyTaskId != 0, "配置中找不到 DailyLoginToGames 的每日子任务");
            Check(Ctx.Store.Archive.Tasks.TryGetValue(dailyTaskId, out TaskDetail dailyDetail),
                $"日刷新后存档缺每日任务 {dailyTaskId}");
            Check(dailyDetail.State != (int)TaskState.Received,
                $"每日任务 {dailyTaskId} 未被日刷新重建（仍为已领取）: state={dailyDetail.State}");
            Check(dailyDetail.State == (int)TaskState.Progress,
                $"每日任务 {dailyTaskId} 状态异常: state={dailyDetail.State}（修复后同日重登不触发重置，预期进行中）");
            ActiveDetail dailyActive = Ctx.Store.Archive.TaskActives[(int)TaskType.Daily];
            Check(dailyActive.Progress == 0 && dailyActive.ReceiveRews.Count == 0,
                $"日常活跃度未清空: progress={dailyActive.Progress} ReceiveRews={dailyActive.ReceiveRews.Count}");

            // 其它数据不受影响：章节成就任务仍保持已领取
            Check(Ctx.Store.Archive.Tasks.TryGetValue(mainTaskId, out TaskDetail mainDetail)
                  && mainDetail.State == (int)TaskState.Received,
                $"章节成就任务 {mainTaskId} 状态丢失");

            return $"旧档（无 taskDailyLastRefreshTime 键）重登 OK，TaskDailyLastRefreshTime 0->{refreshTime}，" +
                   $"每日任务{dailyTaskId}已重建（state={dailyDetail.State} progress={dailyDetail.Progress}），章节成就任务{mainTaskId}保持已领取";
        }

        // ---------- 英灵技能等级跟随（产品改动：技能升级移除，技能等级跟随英灵等级） ----------

        private static int sSyncSoldierId;

        private static PlayerCard GetCard(int cardId)
        {
            foreach (PlayerCard c in Ctx.Store.Archive.Cards)
            {
                if (c.CardId == cardId)
                {
                    return c;
                }
            }
            return null;
        }

        // 该英灵已拥有（Num>0）的 Skill/SkillPassive 卡
        private static List<PlayerCard> OwnedSkillsOf(int soldierCardId)
        {
            var result = new List<PlayerCard>();
            foreach (PlayerCard c in Ctx.Store.Archive.Cards)
            {
                if (c.Num <= 0)
                {
                    continue;
                }
                CardBaseConfig cc = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(c.CardId);
                if (cc == null || (cc.Type != CardType.Skill && cc.Type != CardType.SkillPassive))
                {
                    continue;
                }
                SpecialEffectsConfig spe = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(c.CardId);
                if (spe == null || spe.ActsOnCard != soldierCardId)
                {
                    continue;
                }
                result.Add(c);
            }
            return result;
        }

        // 消耗/奖励是否全是普通道具（ModuleType 默认分组，GM 可加、扣减可断言）
        private static bool IsPlainItems(IList<Config.RewardAndNum> rewards)
        {
            foreach (Config.RewardAndNum r in rewards)
            {
                ItemConfig itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(r.Id);
                if (itemCfg == null || itemCfg.ModuleType == ModuleType.Card
                    || itemCfg.ModuleType == ModuleType.Equip || itemCfg.ModuleType == ModuleType.Resource
                    || itemCfg.ModuleType == ModuleType.Exp || itemCfg.ModuleType == ModuleType.Task)
                {
                    return false;
                }
            }
            return true;
        }

        private static string Case11SoldierUpgradeSyncsSkillLevels()
        {
            Tables tables = DataConfigManager.Tables;
            int heroAwakingLevel = tables.TbPlayerConst.HeroAwakingLevel;

            // 找一个已拥有、未觉醒、配了觉醒档（觉醒档有 AwakeningConsume）、有已拥有技能卡、
            // 且觉醒档前一级升级消耗与觉醒消耗全是普通道具的英灵
            PlayerCard soldier = null;
            CardDevelopConfig devCfg = null;
            foreach (PlayerCard c in Ctx.Store.Archive.Cards)
            {
                CardBaseConfig cc = tables.TbCardBaseConfig.GetOrDefault(c.CardId);
                if (cc == null || cc.Type != CardType.Soldier)
                {
                    continue;
                }
                if (c.Data != null && c.Data.IsAwaked)
                {
                    continue;
                }
                CardDevelopConfig dev = tables.TbCardDevelopConfig.GetOrDefault(c.CardId);
                if (dev == null || dev.LevelPropertyList.Count < heroAwakingLevel)
                {
                    continue;
                }
                DevelopLevelPropertyList awakeLvCfg = dev.LevelPropertyList[heroAwakingLevel - 1];
                if (awakeLvCfg.AwakeningConsume.Count == 0)
                {
                    continue;
                }
                if (!IsPlainItems(dev.LevelPropertyList[heroAwakingLevel - 2].ConsumeItems)
                    || !IsPlainItems(awakeLvCfg.AwakeningConsume))
                {
                    continue;
                }
                bool hasAwakingSkill = false;
                foreach (SpecialEffectsConfig spe in tables.TbSpecialEffectsConfig.DataList)
                {
                    if (spe.AwakingSkill == 1 && spe.ActsOnCard == c.CardId)
                    {
                        hasAwakingSkill = true;
                        break;
                    }
                }
                if (!hasAwakingSkill || OwnedSkillsOf(c.CardId).Count == 0)
                {
                    continue;
                }
                soldier = c;
                devCfg = dev;
                break;
            }
            Check(soldier != null, "找不到满足条件的英灵（未觉醒/有觉醒档/有已拥有技能卡/消耗为普通道具）");
            sSyncSoldierId = soldier.CardId;

            // 直接把英灵推到觉醒档前一级（测试数据准备，同 Case6 手法）
            soldier.Lv = heroAwakingLevel - 1;
            DevelopLevelPropertyList lvCfg = devCfg.LevelPropertyList[soldier.Lv - 1];
            // ConsumeItems 序列化为 {"itemId":num}（服务端按字典反序列化，与客户端 JsonConvert 行为一致）
            var consumeNode = new JSONObject();
            foreach (Config.RewardAndNum r in lvCfg.ConsumeItems)
            {
                GmAddItem(r.Id, r.Num);
                consumeNode[r.Id.ToString()] = r.Num;
            }

            GameResponse resp = Dispatch(ApiKeys.ApiCardLevelUp,
                new LevelUpCardRequest { CardId = soldier.CardId, ConsumeItems = consumeNode.ToString() });
            Check(resp.Code == 200, $"英灵升级失败 code={resp.Code} msg={resp.Msg}");
            Check(GetCard(sSyncSoldierId).Lv == heroAwakingLevel,
                $"英灵未升到觉醒档: Lv={GetCard(sSyncSoldierId).Lv} 预期={heroAwakingLevel}");

            // 断言全部已拥有技能卡同步到 min(英灵Lv, 技能上限)，且随响应 Items.Cards 下发
            LevelUpCardResponse lvResp = LevelUpCardResponse.Parser.ParseFrom(resp.Data);
            var details = new List<string>();
            foreach (PlayerCard skill in OwnedSkillsOf(sSyncSoldierId))
            {
                SpecialEffectsConfig spe = tables.TbSpecialEffectsConfig.GetOrDefault(skill.CardId);
                int expected = Math.Min(heroAwakingLevel, spe.LevelPropertyList.Count);
                Check(skill.Lv == expected,
                    $"技能卡 {skill.CardId} 未同步: Lv={skill.Lv} 预期={expected}（英灵Lv={heroAwakingLevel} 上限={spe.LevelPropertyList.Count}）");
                Check(lvResp.Items.Cards.ContainsKey(skill.CardId) && lvResp.Items.Cards[skill.CardId].Lv == expected,
                    $"技能卡 {skill.CardId} 未随 LevelUpCardResponse.Items.Cards 下发或等级不符");
                details.Add($"{skill.CardId}:1->{skill.Lv}");
            }

            // 觉醒：空消耗请求 → ExecAwaking 发觉醒技能卡，发放即置 min(英灵Lv, 觉醒技能上限)
            // 觉醒消耗配在觉醒档（Level=heroAwakingLevel，索引 heroAwakingLevel-1）
            DevelopLevelPropertyList awakeConsumeCfg = devCfg.LevelPropertyList[heroAwakingLevel - 1];
            foreach (Config.RewardAndNum r in awakeConsumeCfg.AwakeningConsume)
            {
                GmAddItem(r.Id, r.Num);
            }
            GameResponse respAwake = Dispatch(ApiKeys.ApiCardLevelUp,
                new LevelUpCardRequest { CardId = soldier.CardId, CardLv = heroAwakingLevel + 1 });
            Check(respAwake.Code == 200, $"觉醒失败 code={respAwake.Code} msg={respAwake.Msg}");
            Check(GetCard(sSyncSoldierId).Data.IsAwaked, "觉醒后 IsAwaked 未置位");
            foreach (SpecialEffectsConfig spe in tables.TbSpecialEffectsConfig.DataList)
            {
                if (spe.AwakingSkill != 1 || spe.ActsOnCard != sSyncSoldierId)
                {
                    continue;
                }
                PlayerCard awkCard = GetCard(spe.Id);
                Check(awkCard != null, $"觉醒技能卡 {spe.Id} 未发放");
                int expected = Math.Min(heroAwakingLevel, spe.LevelPropertyList.Count);
                Check(awkCard.Lv == expected,
                    $"觉醒技能卡 {spe.Id} 等级不符: Lv={awkCard.Lv} 预期=min({heroAwakingLevel},{spe.LevelPropertyList.Count})={expected}");
                details.Add($"觉醒技能{spe.Id}:{awkCard.Lv}(clamp={spe.LevelPropertyList.Count})");
            }

            // 防御：技能卡走 levelUp 一律拒绝
            int anySkillId = OwnedSkillsOf(sSyncSoldierId)[0].CardId;
            GameResponse respSkill = Dispatch(ApiKeys.ApiCardLevelUp,
                new LevelUpCardRequest { CardId = anySkillId, CardLv = 2 });
            Check(respSkill.Code == 500 && respSkill.Msg == "技能等级跟随英灵等级提升",
                $"技能卡升级应 500「技能等级跟随英灵等级提升」，实际 code={respSkill.Code} msg=\"{respSkill.Msg}\"");

            return $"英灵{sSyncSoldierId} 升到{heroAwakingLevel}级，技能同步 [{string.Join(" ", details)}]，技能卡直升拒绝OK";
        }

        // 老档迁移：技能卡 Lv 落后英灵时，登录（LoginHandler 迁移钩子）向上补齐
        private static string Case12LoginMigratesSkillLevels()
        {
            Check(sSyncSoldierId != 0, "Case11 未先执行（无同步英灵）");
            PlayerCard skill = OwnedSkillsOf(sSyncSoldierId)[0];
            SpecialEffectsConfig spe = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skill.CardId);
            int soldierLv = GetCard(sSyncSoldierId).Lv;
            int expected = Math.Min(soldierLv, spe.LevelPropertyList.Count);
            Check(expected > 1, $"用例前提不成立：同步目标等级 {expected} <= 1，无法验证补齐");

            // 模拟老档：技能卡 Lv 打回 1，落盘后重登
            skill.Lv = 1;
            Ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            Ctx.Store.Flush();

            Global.LocalServer = new LocalServerContext();
            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"重登失败 code={resp.Code} msg={resp.Msg}");

            PlayerCard loaded = GetCard(skill.CardId);
            Check(loaded != null && loaded.Lv == expected,
                $"登录迁移未补齐: 技能卡 {skill.CardId} Lv={(loaded != null ? loaded.Lv : -1)} 预期={expected}（英灵Lv={soldierLv}）");

            // 幂等：再登一次等级不变
            Ctx.Store.Flush();
            Global.LocalServer = new LocalServerContext();
            GameResponse resp2 = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp2.Code == 200, $"二次重登失败 code={resp2.Code} msg={resp2.Msg}");
            Check(GetCard(skill.CardId).Lv == expected, "二次登录后技能等级发生变化（迁移不幂等）");

            return $"技能卡{skill.CardId} Lv 1->{expected}（英灵{sSyncSoldierId} Lv={soldierLv}），二次登录幂等OK";
        }

        // 门票懒更新（对照 Go TicketService.LazyUpdateTicketOnLogin / PlayerTicketService.go:9-26）：
        // 建档登录后配置内门票应全部懒补齐（NextClaimTime=0 即可领取）；
        // NextClaimTime 非 0 且已过期（早于现在）的门票，登录时重置为 0（CheckNeedReset）
        private static string Case13TicketLazyUpdateOnLogin()
        {
            Tables tables = DataConfigManager.Tables;
            Check(tables.TbTicketConfig.DataList.Count > 0, "TbTicketConfig 为空，用例前提不成立");
            Check(Ctx.Store.Archive.Tickets.Count == tables.TbTicketConfig.DataList.Count,
                $"建档登录后门票未懒补齐: 存档={Ctx.Store.Archive.Tickets.Count} 配置={tables.TbTicketConfig.DataList.Count}");

            // 造过期门票：NextClaimTime 改成过去的 unix 秒，落盘后重登
            PlayerTicket ticket = Ctx.Store.Archive.Tickets[0];
            long pastUnix = DateTimeOffset.Now.ToUnixTimeSeconds() - 3600;
            ticket.NextClaimTime = pastUnix;
            Ctx.Store.MarkDirty(PlayerArchive.TicketsFieldNumber);
            Ctx.Store.Flush();

            // 全新上下文重建 + 重登（模拟重启进程）
            Global.LocalServer = new LocalServerContext();
            GameResponse resp = Dispatch(ApiKeys.ApiLogin, new LoginRequest { Account = "smoke" });
            Check(resp.Code == 200, $"重登失败 code={resp.Code} msg={resp.Msg}");

            PlayerTicket loaded = null;
            foreach (PlayerTicket t in Ctx.Store.Archive.Tickets)
            {
                if (t.TicketId == ticket.TicketId)
                {
                    loaded = t;
                    break;
                }
            }
            Check(loaded != null, $"重登后门票 {ticket.TicketId} 丢失");
            Check(loaded.NextClaimTime == 0,
                $"过期门票未重置为可领取: ticketId={ticket.TicketId} NextClaimTime={loaded.NextClaimTime}（重置前={pastUnix}，预期重置为 0）");

            return $"门票懒补齐 {Ctx.Store.Archive.Tickets.Count}/{tables.TbTicketConfig.DataList.Count}，过期门票{ticket.TicketId} NextClaimTime {pastUnix}->0（重置可领取）";
        }
    }
}
