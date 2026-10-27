using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Google.Protobuf.Collections;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 战斗结算，对照 Server/app/api/application/battleapplication/application_battle.go:209-445 SettleBattle。
    /// 拆自 BattleHandler.cs（partial 同类），共享其常量与存档/配置查询辅助。
    /// </summary>
    public partial class BattleHandler
    {
        // ============================================================
        // 战斗结算
        // ============================================================

        // 对照 application_battle.go:209-445 SettleBattle
        // 偏差：Go 事务段（:368-429）失败时 panic("结算失败")；本地同步顺序执行，Execute 失败等价于回滚（不落盘），省略该兜底
        public SettleBattleResponse Settle(LocalServerContext ctx, SettleBattleRequest req)
        {
            BattleData battleData = req.BattleData;
            PlayerBattle battle = ctx.Store.Archive.Battle;
            int stageId = battle.CurrentStageId;
            if (battle.Status != BattleStatusStart || stageId == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗未开始");
                throw new LocalServerException("战斗未开始");
            }

            StageBattleConfig stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(stageId);
            if (stageCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 战斗关卡ID错误");
                throw new LocalServerException("战斗关卡ID错误");
            }

            // 用户当前关卡（:224）
            // 偏差：Go 记录不存在时保存零值关卡（stage_id=0 脏行），本地按意图补建正常关卡记录
            PlayerStage stage = GetUserStage(ctx, stageId);
            if (stage == null)
            {
                stage = CreatePlayerStage(ctx, stageId);
            }

            var rewards = new Dictionary<int, long>();
            PlayerEndlessTower resultEndlessTower = null;
            PlayerChallengeBoss resultChallengeBoss = null;
            PlayerHiddenRealmTower resultHiddenRealmTower = null;
            PlayerAtlas resultAtlas = null;
            List<PlayerCard> resultCards = null;
            List<PlayerTeam> resultTeams = null;

            // 每天刷怪配置（:229）；Go 缺配置时 nil 解引用 panic，本地 Get 缺 key 同样 fail fast
            List<MonsterGenStage> dayGenCfgs = DataConfigManager.Tables.TbMonsterGenConfig.Get(stageId).StageList;

            // 战斗胜利（:232）
            bool isWinner = battleData.Winner == ctx.UserId;
            if (isWinner)
            {
                // 通关固定奖励（:235-237）
                if (stageCfg.Rewards.Count > 0)
                {
                    rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.Rewards));
                }

                // 通关奖励组（:240-242）
                if (stageCfg.RewardGroups.Count > 0)
                {
                    rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.RewardGroups));
                }

                // 首次通关（:244-265）
                if (stage.Condition1 == ConditionStatusNormal)
                {
                    if (stageCfg.StageType == StageType.Main || stageCfg.StageType == StageType.Guide)
                    {
                        // 主线/引导章节首通排行 rankingapplication.UpdateChapterRanking（:248）：排行榜不迁移，跳过
                        // 设置最新通关主线关卡Id（:251-253）
                        if (stageId > battle.ClearStageId)
                        {
                            battle.ClearStageId = stageId;
                        }
                        // 记录首通时间戳：每日限通（每天只能首次通关一个主线新关卡）以此为判定依据
                        stage.ClearStageTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    }

                    // 初始挂机时间（:255-259）；Go nil → 本地 proto 空串
                    if (battle.AfkTime.Length == 0)
                    {
                        battle.AfkTime = FormatNow();
                    }
                    // 设置关卡状态为通关（:261-262）
                    stage.Status = StageStatusClear;
                    stage.Condition1 = ConditionStatusAchieve;
                    // 首次通关奖励（:264）
                    rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(stageCfg.FirstRewards));
                }
                if (stage.Condition2 == ConditionStatusNormal && !battleData.IsSoldierDeath)
                {
                    stage.Condition2 = ConditionStatusAchieve;
                }
                if (stage.Condition3 == ConditionStatusNormal && !battleData.IsSoldierReduceHp)
                {
                    stage.Condition3 = ConditionStatusAchieve;
                }

                // 刷怪奖励（:277-280）
                MonsterGenStage lastGenCfg = dayGenCfgs[dayGenCfgs.Count - 1];
                if (lastGenCfg.Rewards.Count > 0)
                {
                    rewards = MergeItems(rewards, ctx.ItemEntrance.RewardsToItems(lastGenCfg.Rewards));
                }

                // 关卡分数&挑战次数（:283-284）
                stage.Score += battleData.Scores;
                stage.PlayCount++;

                // 设置章节关卡（:287）
                UnlockStage(ctx, stageId, stage.Condition1);

                // 累加总分（:290）
                battle.Score += battleData.Scores;

                // 设置任务进度 - 挑战关卡（application_battle.go:293）
                ctx.TaskEngine.SetProgress(TaskTargetType.ChallengeLevel, 1, 0, false);

                // 关卡类型（:296-331）
                switch (stageCfg.StageType)
                {
                    case StageType.EndlessTower: // 无尽塔（:297-310）
                    {
                        EndlessTowerConfig towerCfg = GetOpenTowerCfg();
                        PlayerEndlessTower endlessTower = GetEndlessTower(ctx, towerCfg.Id);
                        // 对照 :300 Go 零值语义 NodeId==0 跳过；本地记录不存在即 null，等价
                        if (endlessTower != null && endlessTower.NodeId != 0)
                        {
                            Dictionary<int, int> clearNodes = JsonConvert.DeserializeObject<Dictionary<int, int>>(endlessTower.ClearNodes);
                            clearNodes[endlessTower.NodeId] = MapNodeStateClear;
                            endlessTower.ClearNodes = JsonConvert.SerializeObject(clearNodes);
                            // 偏差：客户端 proto 无 UpdateTime 字段，跳过时间更新（:306）
                            ctx.Store.MarkDirty(PlayerArchive.EndlessTowersFieldNumber);
                            resultEndlessTower = endlessTower;
                        }
                        break;
                    }
                    case StageType.GreaterRift: // 秘境（:312-331）
                    {
                        bool newlyCleared;
                        PlayerHiddenRealmTower hiddenRealmTower = MarkFloorClearedByStage(ctx, stageCfg.Id, out newlyCleared);
                        if (hiddenRealmTower != null)
                        {
                            resultHiddenRealmTower = hiddenRealmTower;
                        }
                        if (newlyCleared)
                        {
                            // 秘境塔排行 rankingapplication.UpdateHiddenRealmTowerRanking（:318-326）：排行榜不迁移，跳过
                        }
                        // 秘境塔层奖励（:327-330）
                        Dictionary<int, long> towerRewards = GenTowerRewards(ctx, stageCfg.Id);
                        if (towerRewards.Count > 0)
                        {
                            rewards = MergeItems(rewards, towerRewards);
                        }
                        break;
                    }
                }

                // 设置任务进度 - 完成章节所有难度（application_battle.go:334，覆盖式，参数是章节 Id）
                ctx.TaskEngine.SetProgress(TaskTargetType.CompleteAllDifficulties, stageCfg.Difficulty, stageCfg.ChapterId, true);
            }
            else
            {
                // 失败（:337-345）
                List<Config.RewardAndNum> genRewards = dayGenCfgs[0].Rewards;
                if (battleData.ClearDay < dayGenCfgs.Count)
                {
                    genRewards = dayGenCfgs[battleData.ClearDay].Rewards;
                }

                // 战斗失败，奖励减半
                rewards = MergeItems(rewards, ctx.ItemEntrance.RewardToItemsRide(genRewards, 0.5, false));
            }

            // 无论失败还是成功都会做的动作（:348-360）
            if (stageCfg.StageType == StageType.ChallengeBoss)
            {
                ChallengeBossConfig bossCfg = GetOpenBossCfg();
                PlayerChallengeBoss challengeBoss = GetChallengeBoss(ctx, bossCfg.Id);
                // 对照 :352 Go 零值语义 Id==0 跳过；本地记录不存在即 null，等价
                if (challengeBoss != null)
                {
                    challengeBoss.TotalScore += battleData.Scores;
                    challengeBoss.TotalDamage += battleData.Damage;
                    // 偏差：客户端 proto 无 UpdateTime 字段，跳过时间更新（:355）
                    ctx.Store.MarkDirty(PlayerArchive.ChallengeBossFieldNumber);
                    resultChallengeBoss = challengeBoss;
                }
            }

            // 通关经验（:362-366）
            if (stageCfg.Exp > 0)
            {
                int expItemId = GetItemIdByType(ItemType.PlayerExp);
                var expItems = new Dictionary<int, long> { { expItemId, stageCfg.Exp } };
                rewards = MergeItems(rewards, expItems);
            }

            // ============================================================
            // 对照 :368-425 事务段（事务/行锁不迁移，同步顺序执行）
            // ============================================================
            int guidesStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
            bool isGuideStage = stageId == guidesStageId;
            if (isGuideStage)
            {
                // 首次引导关未通关才重新设置用户卡牌（:371-385）
                if (stage.Status == StageStatusNormal)
                {
                    DeleteGuideCards(ctx); // :374 删除引导卡牌
                    AddInitCards(ctx); // :375 添加初始卡牌
                    resultCards = new List<PlayerCard>(ctx.Store.Archive.Cards); // :376

                    // 清空队伍（:378-384）
                    int talentId = DataConfigManager.Tables.TbTalentConfig.DataList[0].Id;
                    PlayerTeam team = GetTeamById(ctx, talentId);
                    // 偏差：Go 查不到队伍时保存零值行（插脏数据），本地按意图跳过
                    if (team != null)
                    {
                        team.Cards = "[]";
                        team.HeroTeam = "[]";
                        team.AsCardTeam = "[]";
                        ctx.Store.MarkDirty(PlayerArchive.TeamsFieldNumber);
                    }
                }
                resultTeams = new List<PlayerTeam>(ctx.Store.Archive.Teams); // :386

                // 引导关直接设置通关（:389）
                stage.Status = StageStatusClear;
            }

            // 保存关卡信息（:393）
            ctx.Store.MarkDirty(PlayerArchive.StagesFieldNumber);

            // 保存战斗信息（:396-398）
            battle.Status = BattleStatusIdle;
            battle.ClearStageTime = battleData.ClearStageTime;
            ctx.Store.MarkDirty(PlayerArchive.BattleFieldNumber);

            // 将任意流派叠加至满层：设置任务进度（application_battle.go:401-404）
            if (battleData.SchoolMaxCardCount)
            {
                ctx.TaskEngine.SetProgress(TaskTargetType.StackingACertainEffect, 1, 0, false);
            }

            // 添加奖励（:407-410）
            ResultItems rsItems = null;
            if (rewards.Count > 0)
            {
                rsItems = ctx.ItemEntrance.Execute(rewards);
                rewards = ctx.ItemEntrance.ExpandedItems;
            }

            // 解锁怪物图鉴（:413-415）
            if (battleData.MonsterAtlas.Count > 0)
            {
                resultAtlas = SaveMonsterAtlas(ctx, battleData.MonsterAtlas);
            }

            // 主线关卡首次通关后，解锁依赖该章节的秘境层（:418-420）
            if (isWinner && stageCfg.StageType == StageType.Main)
            {
                PlayerHiddenRealmTower unlockedTower = UnlockHiddenRealmFloorsByChapter(ctx, stageCfg.ChapterId);
                if (unlockedTower != null)
                {
                    resultHiddenRealmTower = unlockedTower;
                }
            }

            // 战斗日志 LogModel.CreateBattleLog（:423）流水表不迁移

            // 引导关卡首次通关重新设置卡牌后，刷新战力返回客户端（:432-439）
            if (isGuideStage)
            {
                ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSoldier);
                long combatPower = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSkill);
                if (rsItems == null)
                {
                    rsItems = new ResultItems();
                }
                rsItems.CombatPower = new CombatPower { CombatPower_ = combatPower };
            }

            // 返回客户端数据（:442-444）+ battle_converter.go:40-62 toSettleBattleResponse
            var resp = new SettleBattleResponse { Stage = stage, Battle = battle, Items = rsItems };
            FillRewards(resp.Rewards, rewards);
            if (resultCards != null)
            {
                resp.Cards.AddRange(resultCards);
            }
            if (resultTeams != null)
            {
                resp.Teams.AddRange(resultTeams);
            }
            if (resultEndlessTower != null)
            {
                resp.EndlessTower = resultEndlessTower;
            }
            if (resultChallengeBoss != null)
            {
                resp.ChallengeBoss = resultChallengeBoss;
            }
            // 对照 :55-57 Id!=0 校验：本地存档记录均带 Id，非空即下发
            if (resultHiddenRealmTower != null)
            {
                resp.HiddenRealmTower = resultHiddenRealmTower;
            }
            if (resultAtlas != null)
            {
                resp.Atlas = resultAtlas;
            }
            return resp;
        }

        // 对照 AtlasService.SaveMonsterAtlas（PlayerAtlasService.go:24-49）：只升不降，有变更才标脏
        private PlayerAtlas SaveMonsterAtlas(LocalServerContext ctx, MapField<int, int> atlas)
        {
            PlayerAtlas playerAtlas = ctx.Store.Archive.Atlas;
            bool isSave = false;
            foreach (var kv in atlas)
            {
                int oldState;
                if (playerAtlas.MonsterAtlas.TryGetValue(kv.Key, out oldState) && oldState >= kv.Value)
                {
                    continue;
                }
                isSave = true;
                playerAtlas.MonsterAtlas[kv.Key] = kv.Value;
            }
            if (isSave)
            {
                ctx.Store.MarkDirty(PlayerArchive.AtlasFieldNumber);
            }
            return playerAtlas;
        }

        // 对照 CardModel.DeleteByCardId（PlayerCardModel.go:250-264）：删除引导卡牌
        private void DeleteGuideCards(LocalServerContext ctx)
        {
            var cardIds = new HashSet<int>();
            foreach (InitCardStruct c in DataConfigManager.Tables.TbPlayerConst.GuidesTeams)
            {
                cardIds.Add(c.CardId);
            }
            RepeatedField<PlayerCard> cards = ctx.Store.Archive.Cards;
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cardIds.Contains(cards[i].CardId))
                {
                    cards.RemoveAt(i);
                }
            }
            ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
        }

        // 对照 CardService.TxAddInitCards（PlayerCardService.go:25-31）：map 语义同 cardId 后者覆盖
        private void AddInitCards(LocalServerContext ctx)
        {
            var addCards = new Dictionary<int, int>();
            foreach (InitCardStruct v in DataConfigManager.Tables.TbPlayerConst.InitCards)
            {
                addCards[v.CardId] = v.Level; // Go 用 Level 字段（对奥术卡即初始数量）
            }
            AddCardsLogic(ctx, addCards, true);
        }

        // 对照 CardService.TxAddCardsLogic（PlayerCardService.go:91-238），与 ItemEntrance.AddCardsLogic 同口径；
        // 此处是引导关重置直接操作存档，不走道具入口（避免重复卡转碎片语义）
        private List<PlayerCard> AddCardsLogic(LocalServerContext ctx, Dictionary<int, int> addCards, bool isAllCard)
        {
            Tables tables = DataConfigManager.Tables;
            var cards = new List<PlayerCard>();
            var cardIds = new HashSet<int>(addCards.Keys);
            // 对照 Go 从事务前 DB 快照取卡：本批新加的卡不参与后续技能卡判重
            var cardMap = new Dictionary<int, PlayerCard>();
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                cardMap[card.CardId] = card;
            }
            var changeExistAsCards = new List<PlayerCard>();

            foreach (var kv in addCards)
            {
                int cid = kv.Key;
                int cidNums = kv.Value;
                CardBaseConfig cfg = tables.TbCardBaseConfig.GetOrDefault(cid);
                if (cfg == null)
                {
                    continue; // 对照 :99-101 配置缺失跳过
                }

                bool exists = cardMap.TryGetValue(cid, out PlayerCard oldCard);
                if (cfg.Type != CardType.GeneralSkill)
                {
                    // 不是奥术卡，如果已经存在则不添加
                    if (exists)
                    {
                        continue;
                    }
                }
                else if (exists)
                {
                    // 奥术卡已经存在则需要叠加数量（:109-136，资产流水不迁移）
                    oldCard.Num += cidNums;
                    changeExistAsCards.Add(oldCard);
                    continue;
                }

                // 实际要添加的卡（proto 无 CreateTime 字段，不迁移）
                var addCard = new PlayerCard
                {
                    Id = NextCardId(ctx),
                    UserId = ctx.UserId,
                    CardId = cid,
                    Data = new PlayerCard.Types.CardData(),
                    Lv = 1,
                    Num = cidNums,
                };
                // 不是奥术卡，数量永远只能有一张
                if (cfg.Type != CardType.GeneralSkill)
                {
                    addCard.Num = 1;
                }
                ctx.Store.Archive.Cards.Add(addCard);
                cards.Add(addCard);

                // 获得英雄：附带全部已解锁技能卡（:156-194）
                if (isAllCard && cfg.Type == CardType.Soldier)
                {
                    var skillIds = new List<int>();
                    foreach (SpecialEffectsConfig v in tables.TbSpecialEffectsConfig.DataList)
                    {
                        CardBaseConfig cardBaseCfg = tables.TbCardBaseConfig.GetOrDefault(v.Id);
                        if (cardBaseCfg == null)
                        {
                            LogTool.LogError($"未配置的CardBaseId: [{v.Id}]错误");
                            continue;
                        }
                        // 解锁等级(0级表示无解锁等级)
                        if (cardBaseCfg.UnLockLevel > 1)
                        {
                            continue;
                        }
                        if (v.ActsOnCard == cfg.Id && cardBaseCfg.Type != CardType.SoldierNormalAtk)
                        {
                            skillIds.Add(v.Id);
                        }
                    }

                    foreach (int skillId in skillIds)
                    {
                        if (cardMap.ContainsKey(skillId) || cardIds.Contains(skillId))
                        {
                            continue;
                        }
                        var skillCardObj = new PlayerCard
                        {
                            Id = NextCardId(ctx),
                            UserId = ctx.UserId,
                            CardId = skillId,
                            Lv = 1,
                            Data = new PlayerCard.Types.CardData(),
                            Num = 1,
                        };
                        ctx.Store.Archive.Cards.Add(skillCardObj);
                        cards.Add(skillCardObj);
                    }
                }
            }

            cards.AddRange(changeExistAsCards);
            ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            return cards;
        }

        // 对照 TeamModel.GetByTeamId（PlayerTeamModel.go:99-107）；查不到返回 null（Go 零值）
        private PlayerTeam GetTeamById(LocalServerContext ctx, int teamId)
        {
            foreach (PlayerTeam team in ctx.Store.Archive.Teams)
            {
                if (team.TeamId == teamId)
                {
                    return team;
                }
            }
            return null;
        }

        // Id 为原自增主键，取当前集合最大 Id+1（从 1 开始），与 ItemEntrance.NextCardId 同口径
        private int NextCardId(LocalServerContext ctx)
        {
            int maxId = 0;
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                if (card.Id > maxId)
                {
                    maxId = card.Id;
                }
            }
            return maxId + 1;
        }
    }
}
