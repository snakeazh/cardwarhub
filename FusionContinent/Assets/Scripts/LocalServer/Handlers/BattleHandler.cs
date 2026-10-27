using System;
using System.Collections.Generic;
using System.Globalization;
using Cardwar;
using Config;
using FusionContinent.Manager;
using Google.Protobuf.Collections;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 战斗模块 Handler，逐行对照 Server/app/api/application/battleapplication/application_battle.go。
    /// 结算接口体量大，拆到 BattleSettleLogic.cs（partial 同类）；本文件含其余 6 个接口 + 共享的存档/配置查询辅助。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案；事务/行锁不迁移，同步顺序执行。
    /// 不迁移：战斗日志（LogModel.CreateBattleLog 流水表）、排行榜上榜（rankingapplication，在线模块）。
    /// 任务进度已由 TaskEngine 接入（BattleSettleLogic 三处钩子）。
    /// </summary>
    public partial class BattleHandler
    {
        // 对照 battleModel/playerBattleModel.go:20-25
        private const int BattleStatusIdle = 0;
        private const int BattleStatusStart = 1;

        // 对照 stageModel/playerStageModel.go:12-23
        private const int StageStatusNormal = 0;
        private const int StageStatusClear = 1;
        private const int StageStatusLock = 2;
        private const int ConditionStatusNormal = 0;
        private const int ConditionStatusAchieve = 1;
        private const int ConditionStatusCollect = 2;

        // 对照 chapterModel/PlayerChapterModel.go:14-19
        private const int ChapterStatusUnlock = 1;
        private const int ChapterStatusClear = 2;

        // 对照 enums StageCondition（enums 常量文件:9-13）
        private const int StageCondition1 = 1;
        private const int StageCondition2 = 2;
        private const int StageCondition3 = 3;

        // 对照 configFiles/MapNodeState.go:20
        private const int MapNodeStateClear = 1;

        // 对照 HiddenRealmTowerModel/PlayerHiddenRealmTowerModel.go:30
        private const int HiddenRealmTowerId = 1;

        // ============================================================
        // 开始战斗
        // ============================================================

        // 对照 application_battle.go:41-190 StartBattle
        // 偏差：Go 事务段（:166-185）失败时 panic("开始战斗失败")；本地前置校验已保证 Execute 不会失败，省略该兜底
        public StartBattleResponse Start(LocalServerContext ctx, StartBattleRequest req)
        {
            int stageId = req.StageId;
            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(stageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误");
                throw new LocalServerException("战斗关卡ID错误");
            }

            // 用户关卡信息（:49）；Go 零值语义：记录不存在时 Condition1=0(ConditionStatusNormal)
            PlayerStage stage = GetUserStage(ctx, stageId);
            int stageCondition1 = stage == null ? ConditionStatusNormal : stage.Condition1;

            // 消耗体力（:52-63）；新手引导关未通过不消耗体力
            var items = new Dictionary<int, long>();
            int guidesStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
            bool isGuideStagePass = stageId == guidesStageId && stageCondition1 != ConditionStatusNormal;
            if (isGuideStagePass || stageId != guidesStageId)
            {
                if (stageCfg.ConsumeStamina > 0)
                {
                    if (stageCfg.ConsumeStamina > GetStamina(ctx))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 体力不足");
                        throw new LocalServerException("体力不足");
                    }
                    items[GetItemIdByType(ItemType.Stamina)] = -stageCfg.ConsumeStamina;
                }
            }

            // 消耗道具（:66-73）
            if (stageCfg.ConsumeItem.Count > 0)
            {
                Dictionary<int, long> consumeItem = ctx.ItemEntrance.ConsumeToItems(stageCfg.ConsumeItem);
                if (!ctx.ItemEntrance.CheckEnough(consumeItem))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                    throw new LocalServerException("道具不足");
                }
                items = MergeItems(items, consumeItem);
            }

            // 关卡类型分支（:76-150）
            switch (stageCfg.StageType)
            {
                case StageType.ChallengeBoss: // 挑战Boss（:77-91）
                {
                    ChallengeBossConfig bossCfg = GetOpenBossCfg();
                    if (bossCfg == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 暂未开放");
                        throw new LocalServerException("暂未开放");
                    }

                    PlayerChallengeBoss challengeBoss = GetChallengeBoss(ctx, bossCfg.Id);
                    // 对照 :85 Go 零值语义 Id==0 → 本地记录不存在即 null，等价
                    if (challengeBoss == null || challengeBoss.DailyChallengeNum >= bossCfg.DailyChallengeNum)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 挑战次数不足");
                        throw new LocalServerException("挑战次数不足");
                    }

                    challengeBoss.DailyChallengeNum++;
                    ctx.Store.MarkDirty(PlayerArchive.ChallengeBossFieldNumber);
                    break;
                }
                case StageType.EndlessTower: // 无尽塔（:93-123）
                {
                    EndlessTowerConfig towerCfg = GetOpenTowerCfg();
                    // Go 端 towerCfg 为 nil 时解引用 panic，本地 fail fast 同样直接抛
                    PlayerEndlessTower endlessTower = GetEndlessTower(ctx, towerCfg.Id);
                    if (endlessTower == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 玩家无尽塔未初始化");
                        throw new LocalServerException("玩家无尽塔未初始化");
                    }

                    MapNodeListStruct nodeCfg = GetTowerMapNodeCfg(endlessTower.MapId, stageId);
                    if (nodeCfg == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 挑战地图节点不存在");
                        throw new LocalServerException("挑战地图节点不存在");
                    }

                    // 对照 ClearNodes.FromJson（:107-108）
                    Dictionary<int, int> clearNodes = JsonConvert.DeserializeObject<Dictionary<int, int>>(endlessTower.ClearNodes);
                    int nodeState;
                    if (clearNodes.TryGetValue(nodeCfg.NodeId, out nodeState) && nodeState >= MapNodeStateClear)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 地图节点已挑战");
                        throw new LocalServerException("地图节点已挑战");
                    }

                    if (!CheckPreNodes(clearNodes, nodeCfg.PreNone))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 未解锁前置节点");
                        throw new LocalServerException("未解锁前置节点");
                    }

                    endlessTower.NodeId = nodeCfg.NodeId;
                    endlessTower.DailyChallengeNum++;
                    endlessTower.TotalChallengeNum++;
                    // 偏差：客户端 proto PlayerEndlessTower 无 UpdateTime 字段，跳过时间更新（:122）
                    ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);
                    break;
                }
                case StageType.GreaterRift: // 秘境塔（:125-142）
                {
                    PlayerHiddenRealmTower hiddenRealmTower = GetHiddenRealmTower(ctx);
                    if (hiddenRealmTower == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 玩家秘境塔未初始化");
                        throw new LocalServerException("玩家秘境塔未初始化");
                    }

                    // 检查该秘境层是否已解锁（:134）
                    if (!IsFloorUnlockedByEntries(hiddenRealmTower, stageCfg.Id))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 关卡未解锁");
                        throw new LocalServerException("关卡未解锁");
                    }

                    hiddenRealmTower.DailyChallengeNum++;
                    hiddenRealmTower.TotalChallengeNum++;
                    // 偏差：客户端 proto PlayerHiddenRealmTower 无 UpdateTime 字段，跳过时间更新（:141）
                    ctx.Store.MarkDirty(PlayerArchive.HiddenRealmTowersFieldNumber);
                    break;
                }
                default:
                    // 检查章节关卡是否解锁（:145-149）
                    if (!CheckChapterStageIdUnlock(ctx, stageCfg, stageId))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 关卡未解锁");
                        throw new LocalServerException("关卡未解锁");
                    }

                    // 每日限通：主线关卡每天只能首次通关一个新关卡（已通关关卡可重复挑战）
                    if (stageCfg.StageType == StageType.Main
                        && (stage == null || stage.Condition1 == ConditionStatusNormal)
                        && HasFirstClearedMainStageToday(ctx))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 今日已通关新关卡，拦截开战: stageId={stageId}");
                        throw new LocalServerException("今日已通关新关卡，请明天再来挑战");
                    }
                    break;
            }

            PlayerBattle battle = ctx.Store.Archive.Battle;
            if (battle.Status > BattleStatusIdle)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已开始战斗");
                throw new LocalServerException("已开始战斗");
            }
            if (stage == null)
            {
                stage = CreatePlayerStage(ctx, stageId); // 新增关卡信息（:157-159）
            }
            if (stage.Status == StageStatusLock)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 关卡已被锁定");
                throw new LocalServerException("关卡已被锁定");
            }

            // 对照 :166-181 事务段（事务不迁移，同步顺序执行）
            battle.Status = BattleStatusStart;
            battle.ClearStageTime = 0;
            battle.CurrentStageId = stageId;
            // 偏差：客户端 proto PlayerBattle 无 UpdateTime 字段，跳过时间更新（:170）
            ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);

            // 扣除相关道具（:175）
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            // 战斗日志 LogModel.CreateBattleLog（:179）流水表不迁移
            return new StartBattleResponse { Stage = stage, Battle = battle, Items = rsItems };
        }

        /// <summary>
        /// 今日（每日重置点内）是否已首次通关过任意主线关卡。
        /// 判定口径与 DailyResetRunner 一致：GetDayInterval==0 视为同一重置日。
        /// </summary>
        private bool HasFirstClearedMainStageToday(LocalServerContext ctx)
        {
            long nowTs = DateTimeOffset.Now.ToUnixTimeSeconds();
            foreach (PlayerStage s in ctx.Store.Archive.Stages)
            {
                if (s.ClearStageTime == 0) continue;
                StageBattleConfig cfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(s.StageId);
                if (cfg == null || cfg.StageType != StageType.Main) continue;
                if (ServerTimeManager.GetDayInterval(s.ClearStageTime, nowTs) == 0) return true;
            }
            return false;
        }

        // ============================================================
        // 退出战斗
        // ============================================================
        // 对照 application_battle.go:451-465 ExitBattle
        public ExitBattleResponse Exit(LocalServerContext ctx)
        {
            PlayerBattle battle = ctx.Store.Archive.Battle;
            int stageId = battle.CurrentStageId;
            if (battle.Status != BattleStatusStart || stageId == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗未开始");
                throw new LocalServerException("战斗未开始");
            }

            battle.Status = BattleStatusIdle;
            ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);
            return new ExitBattleResponse { Battle = battle };
        }

        // ============================================================
        // 战斗双倍奖励
        // ============================================================

        // 对照 application_battle.go:477-534 DoubleReward
        // 响应 proto 对照 battle_converter.go:145-151 toDoubleRewardResponse（复用 SettleBattleResponse）
        public SettleBattleResponse DoubleReward(LocalServerContext ctx, SettleBattleRequest req)
        {
            // req.BattleData 仅在解析校验阶段使用，业务逻辑不依赖（battle.go:170）
            PlayerBattle battle = ctx.Store.Archive.Battle;
            int stageId = battle.CurrentStageId;
            if (battle.Status != BattleStatusIdle || stageId == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗未结算");
                throw new LocalServerException("战斗未结算");
            }

            if (battle.DailyDoubleRewardNum >= DataConfigManager.Tables.TbGameBattleConst.DailyDoubleRewardNum)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 超出每日次数");
                throw new LocalServerException("超出每日次数");
            }

            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(stageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误");
                throw new LocalServerException("战斗关卡ID错误");
            }

            var rewards = new Dictionary<int, long>();
            const int doubleRide = 2;
            // Go 端 TbMonsterGenConfig.Get 缺配置时 nil 解引用 panic，本地 Get 缺 key 同样 fail fast
            List<MonsterGenStage> dayGenCfgs = DataConfigManager.Tables.TbMonsterGenConfig.Get(stageId).StageList;

            // 通关奖励组（:502-504）
            if (stageCfg.RewardGroups.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardToItemsRide(stageCfg.RewardGroups, doubleRide, false));
            }

            // 刷怪奖励（:507-510）
            MonsterGenStage lastGenCfg = dayGenCfgs[dayGenCfgs.Count - 1];
            if (lastGenCfg.Rewards.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardToItemsRide(lastGenCfg.Rewards, doubleRide, false));
            }

            // 对照 :513-525 事务段：保存战斗信息 + 添加奖励
            battle.DailyDoubleRewardNum++;
            ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);

            ResultItems rsItems = null;
            if (rewards.Count > 0)
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
                rewards = ctx.ItemEntrance.ExpandedItems;
            }

            var resp = new SettleBattleResponse { Battle = battle, Items = rsItems };
            FillRewards(resp.Rewards, rewards);
            return resp;
        }

        // ============================================================
        // 关卡扫荡
        // ============================================================

        // 对照 application_battle.go:547-675 StageSweeping
        // 偏差：Go 事务段失败时 panic("扫荡失败")；本地前置校验已保证 Execute 不会失败，省略该兜底
        public StageSweepingResponse StageSweeping(LocalServerContext ctx, StageSweepingRequest req)
        {
            int stageId = req.StageId;
            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(stageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误");
                throw new LocalServerException("战斗关卡ID错误");
            }

            if (!CheckChapterStageIdUnlock(ctx, stageCfg, stageId))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 关卡未解锁");
                throw new LocalServerException("关卡未解锁");
            }

            PlayerBattle battle = ctx.Store.Archive.Battle;
            if (battle.Status != BattleStatusIdle || stageId == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗未结算");
                throw new LocalServerException("战斗未结算");
            }

            var rewards = new Dictionary<int, long>();
            var costItems = new Dictionary<int, long>();

            // 消耗体力（:569-577）
            int staminaItemId = GetItemIdByType(ItemType.Stamina);
            if (stageCfg.ConsumeStamina > 0)
            {
                if (stageCfg.ConsumeStamina > GetStamina(ctx))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 体力不足");
                    throw new LocalServerException("体力不足");
                }
                costItems[staminaItemId] = -stageCfg.ConsumeStamina;
            }

            // 消耗道具（:579-587）
            if (stageCfg.ConsumeItem.Count > 0)
            {
                Dictionary<int, long> consumeItem = ctx.ItemEntrance.ConsumeToItems(stageCfg.ConsumeItem);
                if (!ctx.ItemEntrance.CheckEnough(consumeItem))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                    throw new LocalServerException("道具不足");
                }
                costItems = MergeItems(costItems, consumeItem);
            }

            // 对照 :589-637 事务段（事务不迁移，同步顺序执行）
            PlayerHiddenRealmTower resultTower = null;
            switch (stageCfg.StageType)
            {
                case StageType.GreaterRift: // 秘境塔（:591-624）
                {
                    PlayerHiddenRealmTower hiddenRealmTower = GetHiddenRealmTower(ctx);
                    if (hiddenRealmTower == null)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 玩家秘境塔未初始化");
                        throw new LocalServerException("玩家秘境塔未初始化");
                    }

                    // 检查该秘境层是否已解锁（:600）
                    if (!IsFloorUnlockedByEntries(hiddenRealmTower, stageCfg.Id))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 关卡未解锁");
                        throw new LocalServerException("关卡未解锁");
                    }

                    // 扫荡要求该层已通关（:605-611）
                    int floor = FloorByStageId(stageCfg.Id);
                    HiddenRealmFloor floorInfo = FindFloorInfo(hiddenRealmTower.Floors, floor);
                    if (floorInfo == null || !floorInfo.Cleared)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 该层未通关，不可扫荡");
                        throw new LocalServerException("该层未通关，不可扫荡");
                    }

                    hiddenRealmTower.DailyChallengeNum++;
                    hiddenRealmTower.TotalChallengeNum++;
                    hiddenRealmTower.DailyStageSweepingNum++;
                    // 偏差：客户端 proto 无 UpdateTime 字段，跳过时间更新（:616）
                    ctx.Store.MarkDirty(PlayerArchive.HiddenRealmTowersFieldNumber);
                    resultTower = hiddenRealmTower;

                    // 秘境塔层奖励（:621-624）
                    Dictionary<int, long> towerRewards = GenTowerRewards(ctx, stageCfg.Id);
                    if (towerRewards.Count > 0)
                    {
                        rewards = MergeItems(rewards, towerRewards);
                    }
                    break;
                }
                default: // :625-637
                {
                    if (battle.DailyStageSweepingNum >= DataConfigManager.Tables.TbGameBattleConst.DailyStageSweepingNum)
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 超出每日次数");
                        throw new LocalServerException("超出每日次数");
                    }

                    // 重置挂机时间（:632-634）
                    battle.AfkTime = FormatNow();
                    battle.DailyStageSweepingNum++;
                    ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);
                    break;
                }
            }

            // 通关固定奖励（:639-642）
            if (stageCfg.Rewards.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.Rewards));
            }

            // 通关奖励组（:644-647）
            if (stageCfg.RewardGroups.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.RewardGroups));
            }

            // 刷怪奖励（:649-654）
            List<MonsterGenStage> dayGenCfgs = DataConfigManager.Tables.TbMonsterGenConfig.Get(stageId).StageList;
            MonsterGenStage lastGenCfg = dayGenCfgs[dayGenCfgs.Count - 1];
            if (lastGenCfg.Rewards.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(lastGenCfg.Rewards));
            }

            // 添加奖励（:656-661）
            ResultItems rsItems = null;
            if (rewards.Count > 0 || costItems.Count > 0)
            {
                Dictionary<int, long> allItems = MergeItems(costItems, rewards);
                rsItems = ctx.ItemEntrance.Execute(allItems);
                rewards = ctx.ItemEntrance.ExpandedItems;
            }

            // 去除奖励中体力显示（:663-664）
            rewards.Remove(staminaItemId);

            var resp = new StageSweepingResponse { Battle = battle, Items = rsItems };
            FillRewards(resp.Rewards, rewards);
            // 对照 battle_converter.go:139-141：秘境塔有记录才下发
            if (resultTower != null)
            {
                resp.HiddenRealmTower = resultTower;
            }
            return resp;
        }

        // ============================================================
        // 章节挂机奖励
        // ============================================================

        // 对照 application_battle.go:775-841 ChapterAfkRewards
        // 偏差：Go 先解引用 AfkTime(:795) 后判 nil(:801)，nil 时是 runtime panic；本地 proto 为空串，按 0 秒处理落入「挂机时长不足」分支
        public ChapterAfkRewardsResponse ChapterAfkRewards(LocalServerContext ctx, ChapterAfkRewardsRequest req)
        {
            PlayerBattle battle = ctx.Store.Archive.Battle;
            if (battle.Status != BattleStatusIdle || battle.ClearStageId == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗未结算");
                throw new LocalServerException("战斗未结算");
            }

            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(battle.ClearStageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误 [{battle.ClearStageId}]");
                throw new LocalServerException($"战斗关卡ID错误 [{battle.ClearStageId}]");
            }

            ChapterStageConfig chapterCfg = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(stageCfg.ChapterId);
            if (chapterCfg == null || (chapterCfg.AfkRewards.Count == 0 && chapterCfg.AfkRewardGroups.Count == 0))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 该章节不能挂机 [{stageCfg.ChapterId}]");
                throw new LocalServerException($"该章节不能挂机 [{stageCfg.ChapterId}]");
            }

            // 时间差值，并判断是否超出最大限制（:794-798）
            long afkSeconds = 0;
            if (battle.AfkTime.Length > 0)
            {
                afkSeconds = DiffSeconds(battle.AfkTime);
            }
            long afkTimeLimitMax = DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax;
            if (afkSeconds > afkTimeLimitMax)
            {
                afkSeconds = afkTimeLimitMax;
            }

            int produceNum = (int)(afkSeconds / DataConfigManager.Tables.TbGameBattleConst.AfkTimeProduce);
            if (battle.AfkTime.Length == 0 || produceNum <= 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 挂机时长不足 [{produceNum}]");
                throw new LocalServerException($"挂机时长不足 [{produceNum}]");
            }

            var rewards = new Dictionary<int, long>();

            // 挂机固定奖励（:809-812）
            if (chapterCfg.AfkRewards.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardToItemsRide(chapterCfg.AfkRewards, produceNum, false));
            }

            // 挂机奖励组（:814-817）
            if (chapterCfg.AfkRewardGroups.Count > 0)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardToItemsRide(chapterCfg.AfkRewardGroups, produceNum, false));
            }

            // 对照 :820-832 事务段：保存战斗信息 + 添加奖励
            battle.AfkTime = FormatNow();
            ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);

            ResultItems rsItems = null;
            if (rewards.Count > 0)
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
                rewards = ctx.ItemEntrance.ExpandedItems;
            }

            // 响应对照 battle_converter.go:118-124
            var resp = new ChapterAfkRewardsResponse { Battle = battle, Items = rsItems };
            FillRewards(resp.Rewards, rewards);
            return resp;
        }

        // ============================================================
        // 领取关卡条件奖励
        // ============================================================

        // 对照 application_battle.go:686-763 StageReceiveRewards
        // 响应 proto 对照 battle_converter.go:126-131：只回 Rewards+Stage（Go 不下发 Items，客户端有 ApplyRewardsFallback 兜底）
        public StageReceiveRewardsResponse StageReceiveRewards(LocalServerContext ctx, StageReceiveRewardsRequest req)
        {
            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(req.StageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误");
                throw new LocalServerException("战斗关卡ID错误");
            }

            // 判断达成条件（:694-726）；Go 零值语义：记录不存在 Id==0 → 条件未达成
            PlayerStage stage = GetUserStage(ctx, req.StageId);
            if (stage == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 条件未达成");
                throw new LocalServerException("条件未达成");
            }
            if (req.Condition == StageCondition1)
            {
                if (stage.Condition1 == ConditionStatusNormal)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 条件未达成");
                    throw new LocalServerException("条件未达成");
                }
                if (stage.Condition1 == ConditionStatusCollect)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                    throw new LocalServerException("已领取");
                }
            }
            else if (req.Condition == StageCondition2)
            {
                if (stage.Condition2 == ConditionStatusNormal)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 条件未达成");
                    throw new LocalServerException("条件未达成");
                }
                if (stage.Condition2 == ConditionStatusCollect)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                    throw new LocalServerException("已领取");
                }
            }
            else if (req.Condition == StageCondition3)
            {
                if (stage.Condition3 == ConditionStatusNormal)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 条件未达成");
                    throw new LocalServerException("条件未达成");
                }
                if (stage.Condition3 == ConditionStatusCollect)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                    throw new LocalServerException("已领取");
                }
            }

            // 获取关卡奖励 && 设置关卡状态（:731-743）
            var rewards = new Dictionary<int, long>();
            if (req.Condition == StageCondition1)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.TreasureRewardsOne));
                stage.Condition1 = ConditionStatusCollect;
            }
            if (req.Condition == StageCondition2)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.TreasureRewardsTwo));
                stage.Condition2 = ConditionStatusCollect;
            }
            if (req.Condition == StageCondition3)
            {
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.TreasureRewardsThree));
                stage.Condition3 = ConditionStatusCollect;
            }

            // 对照 :747-753 事务段：发送奖励 + 更改状态
            ctx.ItemEntrance.Execute(rewards);
            ctx.Store.MarkDirty(PlayerArchive.StagesFieldNumber);
            rewards = ctx.ItemEntrance.ExpandedItems;

            var resp = new StageReceiveRewardsResponse { Stage = stage };
            FillRewards(resp.Rewards, rewards);
            return resp;
        }

        // ============================================================
        // 以下为共享辅助（关卡/章节/体力/无尽塔/挑战Boss/秘境塔/时间）
        // ============================================================

        // 对照 utils/map.go:28-38 MergeMapsAndSum64（同 key 累加）
        private Dictionary<int, long> MergeItems(params Dictionary<int, long>[] maps)
        {
            var result = new Dictionary<int, long>();
            foreach (Dictionary<int, long> map in maps)
            {
                foreach (var kv in map)
                {
                    Accumulate(result, kv.Key, kv.Value);
                }
            }
            return result;
        }

        private void Accumulate(Dictionary<int, long> map, int key, long num)
        {
            long cur;
            map.TryGetValue(key, out cur);
            map[key] = cur + num;
        }

        private void FillRewards(MapField<int, long> target, Dictionary<int, long> rewards)
        {
            foreach (var kv in rewards)
            {
                target[kv.Key] = kv.Value;
            }
        }

        // 对照 stageModel.GetUserStageInfo（playerStageModel.go:77-85）；查不到返回 null（Go 零值 Id=0）
        private PlayerStage GetUserStage(LocalServerContext ctx, int stageId)
        {
            foreach (PlayerStage stage in ctx.Store.Archive.Stages)
            {
                if (stage.StageId == stageId)
                {
                    return stage;
                }
            }
            return null;
        }

        // 对照 stageModel.CreatePlayerStage（playerStageModel.go:107-120）
        // Id 为原自增主键，取当前集合最大 Id+1（从 1 开始）；proto 无 CreateTime/UpdateTime 字段
        private PlayerStage CreatePlayerStage(LocalServerContext ctx, int stageId)
        {
            int maxId = 0;
            foreach (PlayerStage stage in ctx.Store.Archive.Stages)
            {
                if (stage.Id > maxId)
                {
                    maxId = stage.Id;
                }
            }
            var playerStage = new PlayerStage
            {
                Id = maxId + 1,
                UserId = ctx.UserId,
                StageId = stageId,
            };
            ctx.Store.Archive.Stages.Add(playerStage);
            ctx.Store.MarkDirty(PlayerArchive.StagesFieldNumber);
            return playerStage;
        }

        // 对照 chapterModel.GetPlayerChapterInfo（PlayerChapterModel.go:57-66）；查不到返回 null（Go 零值）
        private PlayerChapter GetChapter(LocalServerContext ctx, int chapterId)
        {
            foreach (PlayerChapter chapter in ctx.Store.Archive.Chapters)
            {
                if (chapter.ChapterId == chapterId)
                {
                    return chapter;
                }
            }
            return null;
        }

        // 对照 chapterModel.CheckChapterStageIdUnlock（PlayerChapterModel.go:89-99）
        private bool CheckChapterStageIdUnlock(LocalServerContext ctx, StageBattleConfig stageCfg, int stageId)
        {
            if (stageCfg.StageType == StageType.Guide)
            {
                return true; // 新手关卡默认解锁
            }
            if (stageCfg.StageType == StageType.GreaterRift)
            {
                return true; // 秘境走楼层解锁，不走章节
            }
            PlayerChapter chapter = GetChapter(ctx, stageCfg.ChapterId);
            // 对照 :98 Go 零值语义：记录不存在时 UnlockStageId=0 → 未解锁
            return chapter != null && chapter.UnlockStageId > 0 && chapter.UnlockStageId >= stageId;
        }

        // 对照 chapterModel.UnlockStage（PlayerChapterModel.go:101-130）
        private void UnlockStage(LocalServerContext ctx, int stageId, int condition1)
        {
            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(stageId);
            PlayerChapter chapter = UnlockNextChapter(ctx, stageCfg.ChapterId);
            // 偏差：Go 配置缺失时返回零值章节并继续 Save（插入 chapter_id=0 脏行），本地按意图直接跳过
            if (chapter == null)
            {
                return;
            }

            // 如果当前关卡条件1满足，则设置通关（:107）
            if (condition1 == ConditionStatusAchieve)
            {
                chapter.Status = ChapterStatusClear;
                // 章节礼包标记（:109-120 HasGiftPack/IsGiftPackPurchased）：客户端 proto PlayerChapter 无该字段，跳过
                ctx.Store.MarkDirty(PlayerArchive.ChaptersFieldNumber);

                // 解锁下一个章节（:123-128）
                int nextChapterId = stageCfg.ChapterId + 1;
                ChapterStageConfig nextChapterConfig = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(nextChapterId);
                if (nextChapterConfig != null)
                {
                    UnlockNextChapter(ctx, nextChapterId);
                }
            }
        }

        // 对照 chapterModel.UnlockNextChapter（PlayerChapterModel.go:132-150）：配置缺失返回 null（Go 零值）
        private PlayerChapter UnlockNextChapter(LocalServerContext ctx, int chapterId)
        {
            PlayerChapter chapter = GetChapter(ctx, chapterId);
            if (chapter != null)
            {
                return chapter;
            }

            ChapterStageConfig chapterCfg = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(chapterId);
            if (chapterCfg == null)
            {
                return null;
            }

            // 默认解锁的章节ID（:140-147）；Id 为原自增主键，取当前集合最大 Id+1
            int maxId = 0;
            foreach (PlayerChapter c in ctx.Store.Archive.Chapters)
            {
                if (c.Id > maxId)
                {
                    maxId = c.Id;
                }
            }
            chapter = new PlayerChapter
            {
                Id = maxId + 1,
                UserId = ctx.UserId,
                ChapterId = chapterId,
                UnlockStageId = chapterCfg.InitUnlockStageId,
                Status = ChapterStatusUnlock,
            };
            ctx.Store.Archive.Chapters.Add(chapter);
            ctx.Store.MarkDirty(PlayerArchive.ChaptersFieldNumber);
            return chapter;
        }

        // 对照 ResourceService.GetStamina（PlayerResourceService.go:72-75 → GetResource:18-43）
        private int GetStamina(LocalServerContext ctx)
        {
            return GetOrCreateResource(ctx).Stamina;
        }

        // 对照 ResourceService.GetResource（PlayerResourceService.go:18-43）：懒建 + 体力自动恢复，有变更标脏
        private PlayerResource GetOrCreateResource(LocalServerContext ctx)
        {
            TbPlayerConst constCfg = DataConfigManager.Tables.TbPlayerConst;
            if (ctx.Store.Archive.Resource == null)
            {
                ctx.Store.Archive.Resource = new PlayerResource
                {
                    Id = ctx.UserId,
                    Stamina = constCfg.MaxStamina,
                    StaminaTime = FormatNow(),
                };
                ctx.Store.MarkDirty(PlayerArchive.ResourceFieldNumber);
            }

            PlayerResource resource = ctx.Store.Archive.Resource;
            int maxStamina = constCfg.MaxStamina;
            if (resource.Stamina < maxStamina) // 当前体力小于最大体力时才会自动恢复
            {
                int recoveryTime = (int)constCfg.StaminaRecoveryTime;
                // 对照 utils/time.go:33-35 GetBeginAndEndDiffSecond(now, staminaTime)
                long diffSecond = DateTimeOffset.Now.ToUnixTimeSeconds() - ParseTime(resource.StaminaTime);
                if (diffSecond >= recoveryTime)
                {
                    resource.Stamina += (int)(diffSecond / recoveryTime);
                    if (resource.Stamina > maxStamina)
                    {
                        resource.Stamina = maxStamina;
                    }
                    resource.StaminaTime = FormatNow();
                    ctx.Store.MarkDirty(PlayerArchive.ResourceFieldNumber);
                }
            }
            return resource;
        }

        // 对照 ItemService.GetItemIdByType（PlayerItemService.go:316-325）
        private int GetItemIdByType(ItemType itype)
        {
            foreach (ItemConfig cfg in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                if (cfg.Type == itype)
                {
                    return cfg.Id;
                }
            }
            return 0;
        }

        // 对照 EndlessTowerModel.GetById（PlayerEndlessTowerModel.go:100-108）；查不到返回 null（Go 零值）
        private PlayerEndlessTower GetEndlessTower(LocalServerContext ctx, int towerId)
        {
            foreach (PlayerEndlessTower tower in ctx.Store.Archive.EndlessTowers)
            {
                if (tower.TowerId == towerId)
                {
                    return tower;
                }
            }
            return null;
        }

        // 对照 EndlessTowerModel.GetOpenTowerCfg（PlayerEndlessTowerModel.go:110-118）
        private EndlessTowerConfig GetOpenTowerCfg()
        {
            foreach (EndlessTowerConfig cfg in DataConfigManager.Tables.TbEndlessTowerConfig.DataList)
            {
                if (cfg.Status == ActivityStatusType.Open)
                {
                    return cfg;
                }
            }
            return null;
        }

        // 对照 EndlessTowerModel.GetTowerMapNodeCfg（PlayerEndlessTowerModel.go:120-133）
        private MapNodeListStruct GetTowerMapNodeCfg(int mapId, int stageId)
        {
            EndlessTowerMapConfig cfg = DataConfigManager.Tables.TbEndlessTowerMapConfig.GetOrDefault(mapId);
            if (cfg == null)
            {
                return null;
            }
            foreach (MapNodeListStruct nodeCfg in cfg.NodeList)
            {
                if (nodeCfg.StageId == stageId)
                {
                    return nodeCfg;
                }
            }
            return null;
        }

        // 对照 EndlessTowerModel.CheckPreNodes（PlayerEndlessTowerModel.go:65-80）：任一前置节点已通关即放行
        private bool CheckPreNodes(Dictionary<int, int> clearNodes, List<int> preNodes)
        {
            if (preNodes.Count == 0)
            {
                return true;
            }
            foreach (int preNodeId in preNodes)
            {
                int nodeState;
                if (clearNodes.TryGetValue(preNodeId, out nodeState) && nodeState >= MapNodeStateClear)
                {
                    return true;
                }
            }
            return false;
        }

        // 对照 ChallengeBossModel.GetById（PlayerChallengeBossModel.go:63-71）；查不到返回 null（Go 零值）
        private PlayerChallengeBoss GetChallengeBoss(LocalServerContext ctx, int bossId)
        {
            foreach (PlayerChallengeBoss boss in ctx.Store.Archive.ChallengeBoss)
            {
                if (boss.BossId == bossId)
                {
                    return boss;
                }
            }
            return null;
        }

        // 对照 ChallengeBossModel.GetOpenBossCfg（PlayerChallengeBossModel.go:73-80）
        private ChallengeBossConfig GetOpenBossCfg()
        {
            foreach (ChallengeBossConfig cfg in DataConfigManager.Tables.TbChallengeBossConfig.DataList)
            {
                if (cfg.Status == ActivityStatusType.Open)
                {
                    return cfg;
                }
            }
            return null;
        }

        // 对照 HiddenRealmTowerModel.GetById（PlayerHiddenRealmTowerModel.go:96-107）；查不到返回 null
        // Go 端 GetOpenTowerCfg 直接返回配置表本身（:109-117），本地直接用 DataConfigManager.Tables.TbHiddenRealmTowerConfig
        private PlayerHiddenRealmTower GetHiddenRealmTower(LocalServerContext ctx)
        {
            foreach (PlayerHiddenRealmTower tower in ctx.Store.Archive.HiddenRealmTowers)
            {
                if (tower.TowerId == HiddenRealmTowerId)
                {
                    return tower;
                }
            }
            return null;
        }

        // 对照 HiddenRealmTowerModel.FindFloorInfo（PlayerHiddenRealmTowerModel.go:143-150）
        // 偏差：Go 端层数据是 floor_data JSON 字符串，客户端 proto 已是 repeated HiddenRealmFloor，直接遍历
        private HiddenRealmFloor FindFloorInfo(RepeatedField<HiddenRealmFloor> floors, int floor)
        {
            foreach (HiddenRealmFloor f in floors)
            {
                if (f.Floor == floor)
                {
                    return f;
                }
            }
            return null;
        }

        // 对照 HiddenRealmTowerService.FloorByStageId（PlayerHiddenRealmTowerService.go:296-303）
        private int FloorByStageId(int stageId)
        {
            foreach (HiddenRealmTowerConfig f in DataConfigManager.Tables.TbHiddenRealmTowerConfig.DataList)
            {
                if (f.StageId == stageId)
                {
                    return f.Id;
                }
            }
            return 0;
        }

        // 对照 HiddenRealmTowerService.IsFloorUnlockedByEntries（PlayerHiddenRealmTowerService.go:287-293）
        private bool IsFloorUnlockedByEntries(PlayerHiddenRealmTower tower, int stageId)
        {
            int floor = FloorByStageId(stageId);
            if (floor == 0)
            {
                return false;
            }
            return FindFloorInfo(tower.Floors, floor) != null;
        }

        // 对照 HiddenRealmTowerService.GenFloorEntries（PlayerHiddenRealmTowerService.go:312-335）
        // + monsterEntryTypePool（:338-347，排除 0=ALL）+ pickEntryType（:351-365，entryType=0 全随机不重复）
        private List<HiddenRealmFloorEntry> GenFloorEntries(LocalServerContext ctx, HiddenRealmTowerConfig floorCfg)
        {
            var result = new List<HiddenRealmFloorEntry>();
            if (floorCfg == null || floorCfg.MonsterEntryNum <= 0)
            {
                return result;
            }

            var pool = new List<int>();
            foreach (MonsterEntryConfig cfg in DataConfigManager.Tables.TbMonsterEntryConfig.DataList)
            {
                if ((int)cfg.MonsterEntryType != 0)
                {
                    pool.Add((int)cfg.MonsterEntryType);
                }
            }
            if (pool.Count == 0)
            {
                return result;
            }

            var used = new HashSet<int>();
            for (int i = 0; i < floorCfg.MonsterEntryNum; i++)
            {
                var candidates = new List<int>();
                foreach (int t in pool)
                {
                    if (!used.Contains(t))
                    {
                        candidates.Add(t);
                    }
                }
                if (candidates.Count == 0)
                {
                    break;
                }
                int entryType = candidates[ctx.Rng.Next(candidates.Count)];
                used.Add(entryType);
                result.Add(new HiddenRealmFloorEntry { EntryType = entryType, Level = floorCfg.MonsterEntryLevel });
            }
            return result;
        }

        // 对照 HiddenRealmTowerService.GenTowerRewards（PlayerHiddenRealmTowerService.go:369-392）：
        // BasicRewards 全量 + RandomRewards 每条按 Pro(万分比) 独立概率 roll
        private Dictionary<int, long> GenTowerRewards(LocalServerContext ctx, int stageId)
        {
            var items = new Dictionary<int, long>();
            int floor = FloorByStageId(stageId);
            if (floor == 0)
            {
                return items;
            }
            // 对照 :378 Go 按 stageId 取层配置（非 floor），保留原口径
            HiddenRealmTowerConfig floorCfg = DataConfigManager.Tables.TbHiddenRealmTowerConfig.GetOrDefault(stageId);
            if (floorCfg == null)
            {
                return items;
            }
            foreach (Config.RewardAndNum r in floorCfg.BasicRewards)
            {
                Accumulate(items, r.Id, r.Num);
            }
            foreach (RewardAndNumAndPro r in floorCfg.RandomRewards)
            {
                if (ctx.Rng.Next(100) < r.Pro)
                {
                    Accumulate(items, r.Id, r.Num);
                }
            }
            return items;
        }

        // 对照 HiddenRealmTowerService.MarkFloorClearedByStage（PlayerHiddenRealmTowerService.go:254-284）：
        // 首次通关冻结该层词条（cleared=true），返回塔记录 + 是否首次通关
        private PlayerHiddenRealmTower MarkFloorClearedByStage(LocalServerContext ctx, int stageId, out bool newlyCleared)
        {
            newlyCleared = false;
            PlayerHiddenRealmTower tower = GetHiddenRealmTower(ctx);
            if (tower == null)
            {
                return null;
            }
            int floor = FloorByStageId(stageId);
            if (floor == 0)
            {
                return tower;
            }
            HiddenRealmTowerConfig floorCfg = DataConfigManager.Tables.TbHiddenRealmTowerConfig.GetOrDefault(floor);
            HiddenRealmFloor floorInfo = FindFloorInfo(tower.Floors, floor);
            if (floorInfo == null)
            {
                // 兜底：正常已解锁才能进入战斗，这里补生成（:270-275）
                floorInfo = new HiddenRealmFloor { Floor = floor };
                floorInfo.Entries.AddRange(GenFloorEntries(ctx, floorCfg));
                tower.Floors.Add(floorInfo);
            }
            if (!floorInfo.Cleared)
            {
                floorInfo.Cleared = true;
                // 偏差：客户端 proto 无 UpdateTime 字段，跳过时间更新（:279）
                ctx.Store.MarkDirty(PlayerArchive.HiddenRealmTowersFieldNumber);
                newlyCleared = true;
            }
            return tower;
        }

        // 对照 HiddenRealmTowerService.TxUnlockFloorsByChapter（PlayerHiddenRealmTowerService.go:141-184）：
        // 仅处理 ChapterId 匹配的层，有变更返回塔记录，无变更返回 null
        private PlayerHiddenRealmTower UnlockHiddenRealmFloorsByChapter(LocalServerContext ctx, int chapterId)
        {
            if (chapterId <= 0)
            {
                return null;
            }
            PlayerHiddenRealmTower tower = GetHiddenRealmTower(ctx);
            if (tower == null)
            {
                return null;
            }
            bool changed = false;
            int maxFloor = tower.Floor;
            foreach (HiddenRealmTowerConfig floorCfg in DataConfigManager.Tables.TbHiddenRealmTowerConfig.DataList)
            {
                if (floorCfg.ChapterId != chapterId)
                {
                    continue;
                }
                int floor = floorCfg.Id;
                if (FindFloorInfo(tower.Floors, floor) != null)
                {
                    continue; // 已解锁
                }
                var floorInfo = new HiddenRealmFloor { Floor = floor, Cleared = false };
                floorInfo.Entries.AddRange(GenFloorEntries(ctx, floorCfg));
                tower.Floors.Add(floorInfo);
                changed = true;
                if (floor > maxFloor)
                {
                    maxFloor = floor;
                }
            }
            if (!changed)
            {
                return null;
            }
            if (maxFloor != tower.Floor)
            {
                tower.Floor = maxFloor;
            }
            // 偏差：客户端 proto 无 UpdateTime 字段，跳过时间更新（:181）
            ctx.Store.MarkDirty(PlayerArchive.HiddenRealmTowersFieldNumber);
            return tower;
        }

        // 时间格式对照 chest_converter.go:287-292 formatTime（RFC3339），与 NewPlayerInitializer.FormatTime 一致
        private string FormatNow()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }

        private long ParseTime(string rfc3339)
        {
            return DateTimeOffset.Parse(rfc3339, CultureInfo.InvariantCulture).ToUnixTimeSeconds();
        }

        // 对照 utils/time.go:75-80 DiffSeconds（now - begin 秒差，保证为正）
        private long DiffSeconds(string beginTime)
        {
            long diff = DateTimeOffset.Now.ToUnixTimeSeconds() - ParseTime(beginTime);
            return diff < 0 ? -diff : diff;
        }
    }
}
