using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 新玩家建档。对照 Go：
    /// - user.go LoginHandler 的 CreateUser 分支（:131-141）与 model/userModel.go CreateUser（:181-219）
    /// - application_player_archive.go initNewPlayerData（:521-544，Phase A）+ GetPlayerArchive（:113-505，Phase B 各模块懒建默认值）
    /// 单机版全部同步顺序执行；完成后挂到 ctx.Store，由 LocalApi 收尾统一 Flush。
    /// proto 的 Id 字段（原 MySQL 自增主键）在各集合内从 1 开始递增分配。
    /// </summary>
    public class NewPlayerInitializer
    {
        /// <summary>单机本地账号 UserId 恒为 1。</summary>
        private const int LocalUserId = 1;

        private readonly LocalServerContext _ctx;
        private readonly HashSet<int> _addedCardIds = new HashSet<int>();

        public NewPlayerInitializer(LocalServerContext ctx)
        {
            _ctx = ctx;
        }

        public void CreateNew(string account)
        {
            User user = CreateUser(account);
            var archive = new PlayerArchive();
            InitLazyModules(archive);
            _ctx.Store.AttachNewArchive(user, archive);

            // 设置任务进度 - 每日登录（对照 user.go:139；LastRefreshTime=昨天保证 TryRefreshDaily 先全量重建再推进）
            _ctx.TaskEngine.SetProgress(TaskTargetType.DailyLoginToGames, 1, 0, false);

            // Phase A（对照 initNewPlayerData:521-544，顺序敏感：道具 → 卡牌/编队 → 装备）
            InitItems();
            InitCardsAndTeams(archive);
            InitSoldierEquips(archive);

            LogTool.Log($"[NewPlayerInitializer] 新档创建完成 account={account} cards={archive.Cards.Count} equips={archive.Equips.Count}");
        }

        // 对照 model/userModel.go CreateUser（:181-219）：Level=1、Exp=0、默认头像/头像框，CombatPower=0（阶段1再算）
        private User CreateUser(string account)
        {
            TbPlayerConst constCfg = DataConfigManager.Tables.TbPlayerConst;
            return new User
            {
                UserId = LocalUserId,
                Account = account,
                UserName = account,
                Level = 1,
                Avatar = constCfg.InitDefaultAvatar,
                AvatarFrame = constCfg.InitDefaultAvatarFrame,
                LoginTime = FormatTime(DateTime.Now),
            };
        }

        // 对照 GetPlayerArchive Phase B（application_player_archive.go:155-504）各模块懒建默认值
        private void InitLazyModules(PlayerArchive archive)
        {
            TbPlayerConst constCfg = DataConfigManager.Tables.TbPlayerConst;
            DateTime now = DateTime.Now;
            DateTime yesterday = now.AddDays(-1);

            // 玩家信息（:156-167）：Figure="[]"、GuideFlows=空 GuideFlows{}.ToJson()="{}"
            archive.Player = new UserPlayer { Id = LocalUserId, Figure = "[]", GuideFlows = "{}" };

            // 资源（ResourceService.GetResource:18-43）：新档体力满，StaminaTime=now
            archive.Resource = new PlayerResource { Id = LocalUserId, Stamina = constCfg.MaxStamina, StaminaTime = FormatTime(now) };

            // 战斗（battleModel.CreatePlayerBattle:107-119）：Status=0(Idle)，AfkTime 空（Go 为 nil → proto 空串）
            archive.Battle = new PlayerBattle { Id = LocalUserId };

            // 挑战（:191-202）：GemFusionRewards="[]"
            archive.Challenge = new PlayerChallenge { Id = LocalUserId, GemFusionRewards = "[]" };

            // 章节（:211-218 → chapterService.InitChapter:9-21 → chapterModel.UnlockNextChapter:132-150）
            foreach (int chapterId in constCfg.InitChapterIds)
            {
                ChapterStageConfig chapterCfg = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(chapterId);
                if (chapterCfg == null)
                {
                    continue; // 对照 UnlockNextChapter:137-139 配置不存在跳过
                }
                archive.Chapters.Add(new PlayerChapter
                {
                    Id = archive.Chapters.Count + 1,
                    UserId = LocalUserId,
                    ChapterId = chapterId,
                    UnlockStageId = chapterCfg.InitUnlockStageId,
                    Status = 1, // ChapterStatusUnlock
                });
            }

            // 签到（:221-231）：LastSinginDate=昨天
            archive.Signin = new PlayerSignin { Id = LocalUserId, LastSinginDate = FormatTime(yesterday) };

            // 活动（:318-327）：CreateSinginDate=昨天
            archive.Activity = new PlayerActivity { Id = LocalUserId, CreateSinginDate = FormatTime(yesterday) };

            // 天赋（TalentService.GetList:10-46 懒补齐；空档全部新建，第一个默认使用中）
            // 偏差：Go 原逻辑用 v.Id（自增主键）与 cfg.Id 比较，是已知 bug；此处按意图用 TalentId 比较
            foreach (TalentConfig cfg in DataConfigManager.Tables.TbTalentConfig.DataList)
            {
                archive.Talents.Add(new PlayerTalent
                {
                    Id = archive.Talents.Count + 1,
                    UserId = LocalUserId,
                    TalentId = cfg.Id,
                    Status = archive.Talents.Count == 0 ? 1 : 0, // 1=StatusInUse（TalentModel:34-35）
                });
            }

            // 装备槽位（:392-404 → EquipSlotModel.InitForUser:68-83）：按 TbEquipSlotConfig.SlotType，Level=1
            foreach (EquipSlotConfig cfg in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
            {
                archive.EquipSlots.Add(new PlayerEquipSlot
                {
                    Id = archive.EquipSlots.Count + 1,
                    UserId = LocalUserId,
                    Slot = (int)cfg.SlotType,
                    Level = 1,
                });
            }

            // 头像/头像框（userModel.CreateUser:198-211 授予默认头像与头像框）
            archive.Avatars.Add(new PlayerAvatar { Id = 1, AvatarId = constCfg.InitDefaultAvatar });
            archive.AvatarFrames.Add(new PlayerAvatarFrame { Id = 1, AvatarFrameId = constCfg.InitDefaultAvatarFrame });

            // 轮语者（CycleSpeakerService.GetCycleSpeakerOrCreateByUserId:41-51 + InitSpeakerSkin:53-67）
            foreach (int skinId in constCfg.InitManFigures)
            {
                archive.CycleSpeakerSkins.Add(new PlayerCycleSpeakerSkin
                {
                    Id = archive.CycleSpeakerSkins.Count + 1,
                    SkinId = skinId,
                    Level = 1,
                });
            }
            archive.PlayerSpeakerModule = new PlayerSpeakerModule
            {
                UseSkinId = constCfg.InitManFigures.Count > 0 ? constCfg.InitManFigures[0] : 0,
            };

            // 城墙（:420-432 → CityWallModel.InitCityWallForUser:60-81）：Level=1，默认策略槽位技能为空
            archive.CityWall = new PlayerCityWallArchive { Wall = new PlayerCityWall { Level = 1 } };

            // 门票（TicketService.LazyUpdateTicketOnLogin:9-26）：新档全部立即可领取（NextClaimTime=0，对照 ClaimableSentinel=epoch 0）
            foreach (TicketConfig cfg in DataConfigManager.Tables.TbTicketConfig.DataList)
            {
                archive.Tickets.Add(new PlayerTicket { TicketId = cfg.Id });
            }

            // 无尽塔（EndlessTowerService.GetList:12-52）：NextFloor → Floor=1、随机地图、清空节点/奖励
            // 注意：Go 的 Status==Close 过滤写在内层循环里，新档列表为空时过滤不生效（全量初始化），此处按 Go 实际行为
            foreach (EndlessTowerConfig cfg in DataConfigManager.Tables.TbEndlessTowerConfig.DataList)
            {
                archive.EndlessTowers.Add(new PlayerEndlessTower
                {
                    Id = archive.EndlessTowers.Count + 1,
                    UserId = LocalUserId,
                    TowerId = cfg.Id,
                    Floor = 1,
                    MapId = RandomTowerMapId(),
                    ClearNodes = "{}",
                    DailyRewards = "[]",
                    FloorRewards = "[]",
                });
            }

            // 挑战BOSS（ChallengeBossService.GetList:11-48）：同上，Status 过滤新档不生效
            foreach (ChallengeBossConfig cfg in DataConfigManager.Tables.TbChallengeBossConfig.DataList)
            {
                archive.ChallengeBoss.Add(new PlayerChallengeBoss
                {
                    Id = archive.ChallengeBoss.Count + 1,
                    UserId = LocalUserId,
                    BossId = cfg.Id,
                    ScoreRewards = "[]",
                    DailyRewards = "[]",
                });
            }

            // 秘境塔（HiddenRealmTowerService.GetList:15-45）：仅一座塔(TowerId=1)，NextFloor → Floor=1；
            // 新档无通关章节，refreshUnlockedFloors 不解锁任何层 → Floors 为空
            archive.HiddenRealmTowers.Add(new PlayerHiddenRealmTower
            {
                Id = 1,
                UserId = LocalUserId,
                TowerId = 1, // HiddenRealmTowerModel.HiddenRealmTowerId
                Floor = 1,
                DailyRewards = "[]",
                FloorRewards = "[]",
            });

            // 图鉴（AtlasService.GetAtlas:10-21）：空 MonsterAtlas
            archive.Atlas = new PlayerAtlas { Id = LocalUserId };

            // 兑换礼包（ExchangeService.GetExchangeGift:9-21）：CodeExchanges="{}"
            archive.ExchangeGift = new PlayerExchangeGift { Id = LocalUserId, CodeExchanges = "{}" };

            // 红点（RedDotService.GetAllRedDots:21-27）：新档邮件未读数 0
            archive.RedDots.Add(new RedDotInfo { Type = RedDotType.RedDotMail, Count = 0 });

            // 功能模块（:336-344）：新档无引导关关卡记录，不触发清空，Modules 本就为空
            // 关卡礼包 ChapterGiftPacks：章节均 HasGiftPack=0，为空
            // 纯读模块新档为空：Stages/Shops/ShopGoods/ChestPools/Tasks/TaskActives/FusionGemsV2/Weapons/CreatePlayerActivitys
            // 任务（TaskService.InitTask，PlayerTaskService.go:19-30）：Go 的 MainTasks/DailyTasks/ActiveRewards 均初始化为
            // 空 JSON，对应 proto map 默认空；LastRefreshTime=昨天（:26），保证首次 TryRefreshDaily 触发全量重建
            archive.TaskDailyLastRefreshTime = ((DateTimeOffset)yesterday).ToUnixTimeSeconds();
            // PvpSeasonInfo：Go 登录存档未下发该字段，保持一致（TODO(后续阶段): PvP 赛季接入）
        }

        // 对照 InitPlayerItems（application_player_archive.go:567-584）：InitItems 经统一道具入口发放
        private void InitItems()
        {
            // RewardAndNum 需限定 Config 命名空间（Cardwar 下也有同名 proto 类）
            List<Config.RewardAndNum> initItems = DataConfigManager.Tables.TbPlayerConst.InitItems;
            if (initItems.Count == 0)
            {
                return;
            }
            _ctx.ItemEntrance.Execute(_ctx.ItemEntrance.RewardsToItems(initItems));
        }

        // 对照 initNewPlayerCardsAndTeams（:508-518）→ CardService.TxInitCards + TeamService.InitTeams
        private void InitCardsAndTeams(PlayerArchive archive)
        {
            // TxAddInitCards（PlayerCardService.go:25-31）：Go 用 map 去重，后者覆盖；这里按配置顺序保序去重
            var guideCardIds = new List<int>();
            var guideCardNums = new Dictionary<int, int>();
            foreach (InitCardStruct c in DataConfigManager.Tables.TbPlayerConst.GuidesTeams)
            {
                if (!guideCardNums.ContainsKey(c.CardId))
                {
                    guideCardIds.Add(c.CardId);
                }
                guideCardNums[c.CardId] = c.Level; // Go 语义：map value 用 Level 字段（对奥术卡即初始数量）
            }
            foreach (int cardId in guideCardIds)
            {
                AddCard(archive, cardId, guideCardNums[cardId], true, guideCardIds);
            }

            // InitTeams 的 team.Cards 用原始配置列表（Go 不去重，PlayerTeamService.go:29-33）
            var rawGuideCardIds = new List<int>();
            foreach (InitCardStruct c in DataConfigManager.Tables.TbPlayerConst.GuidesTeams)
            {
                rawGuideCardIds.Add(c.CardId);
            }
            InitTeam(archive, rawGuideCardIds);
        }

        // 对照 TxAddCardsLogic 新建分支（PlayerCardService.go:97-208），新档不存在已持有卡
        private void AddCard(PlayerArchive archive, int cardId, int num, bool withSkills, List<int> batchCardIds)
        {
            CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
            if (cfg == null)
            {
                return; // 对照 :99-101 配置缺失跳过
            }
            if (_addedCardIds.Contains(cardId))
            {
                return; // 新档防重（Go 靠读库判重，等价）
            }
            _addedCardIds.Add(cardId);

            var card = new PlayerCard
            {
                Id = archive.Cards.Count + 1,
                UserId = LocalUserId,
                CardId = cardId,
                Lv = 1, // 对照 :144 初始 Lv 恒为 1
                Num = cfg.Type == CardType.GeneralSkill ? num : 1, // 对照 :148-150 非奥术卡数量恒 1
                Data = new PlayerCard.Types.CardData(), // 对照 CardModel.Data{}.ToJson() 空皮肤/未觉醒
            };
            archive.Cards.Add(card);

            // 获得英雄：附带其低解锁等级的技能卡（对照 :156-194）
            if (!withSkills || cfg.Type != CardType.Soldier)
            {
                return;
            }
            foreach (SpecialEffectsConfig spe in DataConfigManager.Tables.TbSpecialEffectsConfig.DataList)
            {
                CardBaseConfig skillCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(spe.Id);
                if (skillCfg == null)
                {
                    LogTool.LogError($"[NewPlayerInitializer] 未配置的CardBaseId: [{spe.Id}]错误"); // 对照 :161-163
                    continue;
                }
                if (skillCfg.UnLockLevel > 1)
                {
                    continue; // 解锁等级(0/1级表示初始可用)
                }
                if (spe.ActsOnCard != cardId || skillCfg.Type == CardType.SoldierNormalAtk)
                {
                    continue;
                }
                if (_addedCardIds.Contains(spe.Id) || batchCardIds.Contains(spe.Id))
                {
                    continue; // 对照 :178-181 已持有或本批已含则跳过
                }
                AddCard(archive, spe.Id, 1, false, batchCardIds);
            }
        }

        // 对照 TeamService.InitTeams（PlayerTeamService.go:21-61）：teamId=天赋表第一条 Id，含英雄/奥术编队
        private void InitTeam(PlayerArchive archive, List<int> guideCardIds)
        {
            int teamId = DataConfigManager.Tables.TbTalentConfig.DataList[0].Id;

            // genInitHeroTeam（:63-117）：基于全部初始卡（含附带技能卡）
            var heroTeams = new List<object>();
            var heroSkillMap = new Dictionary<int, List<int>>();
            foreach (PlayerCard card in archive.Cards)
            {
                CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.Get(card.CardId);
                if (cfg.Type == CardType.Soldier)
                {
                    heroSkillMap[card.CardId] = new List<int>();
                }
            }
            foreach (PlayerCard card in archive.Cards)
            {
                CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.Get(card.CardId);
                if (cfg.Type != CardType.Skill)
                {
                    continue;
                }
                SpecialEffectsConfig spe = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(card.CardId);
                if (spe == null)
                {
                    throw new LocalServerException("配置不存在"); // 对照 :93-97
                }
                if (heroSkillMap.ContainsKey(spe.ActsOnCard))
                {
                    heroSkillMap[spe.ActsOnCard].Add(card.CardId);
                }
            }
            var asCardIds = new List<int>();
            foreach (PlayerCard card in archive.Cards)
            {
                CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.Get(card.CardId);
                if (cfg.Type == CardType.Soldier)
                {
                    heroTeams.Add(new { heroCardId = card.CardId, skillCardIds = heroSkillMap[card.CardId] });
                }
                else if (cfg.Type == CardType.GeneralSkill)
                {
                    asCardIds.Add(card.CardId);
                }
            }

            archive.Teams.Add(new PlayerTeam
            {
                Id = 1,
                UserId = LocalUserId,
                TeamId = teamId,
                Name = teamId.ToString(),
                Cards = JsonConvert.SerializeObject(guideCardIds),
                CurrentUse = 1,
                CardsPosition = "[]", // 初始化编队站位
                HeroTeam = JsonConvert.SerializeObject(heroTeams),
                AsCardTeam = JsonConvert.SerializeObject(asCardIds),
            });
        }

        // 对照 initNewPlayerData 装备段（:529-543）→ EquipService.TxUniteAddItemsEntrance_ForEquip → EquipModel.TxAddEquip(:138-172)
        private void InitSoldierEquips(PlayerArchive archive)
        {
            TbPlayerConst constCfg = DataConfigManager.Tables.TbPlayerConst;
            foreach (int equipId in constCfg.InitSoldierEquips)
            {
                EquipConfig cfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equipId);
                if (cfg == null)
                {
                    throw new LocalServerException($"装备ID[{equipId}]错误"); // 对照 TxAddEquip:140-143 panic
                }
                var equip = new PlayerEquip
                {
                    Id = archive.Equips.Count + 1,
                    UserId = LocalUserId,
                    EquipId = equipId,
                    WearerId = constCfg.GlobalUserWearId, // 初始装备直接穿戴到全局穿戴位
                    PartType = (int)cfg.Part,
                    Phase = 1,
                };
                equip.Entrys.AddRange(RollEntrys(cfg.EntryNum, cfg.EntryGroup));
                archive.Equips.Add(equip);
            }
        }

        // 对照 EntryService.GetRandomEntrys（EntryService.go:13-44）：按词条组过滤、洗牌、取前 num 个
        private List<int> RollEntrys(int num, int entryGroup)
        {
            var pool = new List<int>();
            foreach (EquipEntryConfig entry in DataConfigManager.Tables.TbEquipEntryConfig.DataList)
            {
                if (entry.EntryGroup == entryGroup)
                {
                    pool.Add(entry.Id);
                }
            }
            if (pool.Count == 0 || num <= 0)
            {
                return new List<int>();
            }
            if (num > pool.Count)
            {
                num = pool.Count;
            }
            // Fisher-Yates，对照 rand.Shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _ctx.Rng.Next(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }
            return pool.GetRange(0, num);
        }

        // 对照 EndlessTowerModel.RandomTowerMapId（PlayerEndlessTowerModel.go:135-144）
        private int RandomTowerMapId()
        {
            List<EndlessTowerMapConfig> mapList = DataConfigManager.Tables.TbEndlessTowerMapConfig.DataList;
            if (mapList.Count == 0)
            {
                return 1;
            }
            return mapList[_ctx.Rng.Next(mapList.Count)].Id;
        }

        // 对照 chest_converter.go formatTime（:287-292）：RFC3339；客户端 mapper 用 DateTime.TryParse 解析
        private string FormatTime(DateTime t)
        {
            return t.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
    }
}
