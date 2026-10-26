using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 编队模块 Handler，逐行对照 Server/app/api/application/teamapplication/application_team.go。
    /// 存档读写对照 Server/app/model/TeamModel/PlayerTeamModel.go（内存 Archive.Teams 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案；事务不迁移。
    /// 注意：奥术卡已去佩戴化，SaveTeams 跳过奥术卡携带校验（Go :120-142），
    /// SaveAsCardTeamHandler（:56-81）整体不迁移（编队不再写入 AsCardTeam）。
    /// </summary>
    public class TeamHandler
    {
        // ============================================================
        // 保存队伍 api/team/saveTeams
        // 对照 application_team.go:105-214 SaveTeams
        // ============================================================
        public SaveTeamsResponse SaveTeams(LocalServerContext ctx, SaveTeamsRequest req)
        {
            Tables tables = DataConfigManager.Tables;

            List<int> teamIdRange = tables.TbGameBattleConst.TeamIdRange;
            if (req.Teams.Count > teamIdRange[1] || req.Teams.Count < teamIdRange[0])
            {
                LogTool.LogWarning($"userId={ctx.UserId} 队伍数量超出限制 count={req.Teams.Count}");
                throw new LocalServerException("队伍数量超出限制");
            }

            // Go 此处 levelConfig 缺失即 nil 解引用 panic，本地 fail fast 同样直接抛
            PlayerLevelConfig levelConfig = tables.TbPlayerLevelConfig.GetOrDefault(ctx.UserLv);

            // 用户当前等级已经能够开启的奥数槽位数量（cfgutil.GetPlayerMaxAsCardSlotIds card_utils.go:93-101）
            // Go 配置为 map[slotId]level，客户端 Luban 为 list（下标即 slotId），仅取数量用于最大人口计算
            int arcSlotCount = 0;
            foreach (int unlockLv in tables.TbPlayerConst.ArcCardUnlockLevel)
            {
                if (ctx.UserLv >= unlockLv)
                {
                    arcSlotCount++;
                }
            }
            // 最大人口数量（:118）
            int maxPopulationNum = levelConfig.HeroSlotNum + arcSlotCount;

            // 跳过 Go :120-142 的奥术卡携带数量/BringCount 校验：奥术卡已去佩戴化，编队不再包含奥术卡

            var resp = new SaveTeamsResponse();
            foreach (TeamParam v in req.Teams)
            {
                // 检验敏感词（:147-151 → utils/SensitiveWords.go:17-26）
                if (v.Name != "" && SensitiveWordsMatch(v.Name))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 名称存在敏感词");
                    throw new LocalServerException("名称存在敏感词");
                }

                // 检验最大人口限制（:152-159）：技能卡/被动技能卡不占人口
                // Go 此处 cardConfig 为 nil 时解引用 panic（如卡Id=0），本地 fail fast 同样直接抛
                int population = 0;
                foreach (int x in v.Cards)
                {
                    CardBaseConfig cardConfig = tables.TbCardBaseConfig.GetOrDefault(x);
                    if (cardConfig.Type != CardType.SkillPassive && cardConfig.Type != CardType.Skill)
                    {
                        population++;
                    }
                }
                if (population > maxPopulationNum)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 超出最大人口限制[{population}/{maxPopulationNum}]");
                    throw new LocalServerException($"超出最大人口限制[{population}/{maxPopulationNum}]");
                }

                // 对照 :161-168 TeamModel.GetByTeamId：查不到（Go 零值行 UserId==0）则建档
                PlayerTeam team = GetTeamByTeamId(ctx, v.TeamId);
                if (team == null)
                {
                    team = new PlayerTeam
                    {
                        Id = NextTeamId(ctx),
                        UserId = ctx.UserId,
                        TeamId = v.TeamId,
                        HeroTeam = "[]",
                        AsCardTeam = "[]",
                    };
                    ctx.Store.Archive.Teams.Add(team);
                }

                // 对照 :170-183：过滤 0 后序列化（Go json.Marshal 空切片为 "[]"，与 Newtonsoft 一致）
                var cards = new List<int>();
                foreach (int x in v.Cards)
                {
                    if (x != 0)
                    {
                        cards.Add(x);
                    }
                }
                string jsonStr = JsonConvert.SerializeObject(cards);
                var cardsPosition = new List<int>(v.CardsPosition);
                string cardsPositionJsonStr = JsonConvert.SerializeObject(cardsPosition);

                // 仅卡组/站位变化时才校验并保存（:185-209）
                if (jsonStr != team.Cards || team.CardsPosition != cardsPositionJsonStr)
                {
                    if (!CheckUserCardsIsOK(ctx, cards))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                        throw new LocalServerException("参数错误");
                    }
                    if (!CheckCardsPosition(cardsPosition))
                    {
                        LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                        throw new LocalServerException("参数错误");
                    }

                    team.Name = v.Name;
                    team.CurrentUse = v.CurrentUse;
                    // proto 无 UpdateTime 字段，不迁移

                    team.Cards = jsonStr;
                    if (team.Cards == "null" || team.Cards == "")
                    {
                        team.Cards = "[]";
                    }
                    team.CardsPosition = cardsPositionJsonStr;
                    if (team.CardsPosition == "null" || team.CardsPosition == "")
                    {
                        team.CardsPosition = "[]";
                    }
                    // 对照 :208 team.Save()
                    ctx.Store.MarkDirty(PlayerArchive.TeamsFieldNumber);
                }

                resp.Teams.Add(team);
            }
            return resp;
        }

        // ============================================================
        // 保存英雄编队 api/team/saveHeroTeam
        // 对照 application_team.go:29-54 SaveHeroTeamHandler + :83-99 checkSaveHeroTeamHandler
        // ============================================================
        public SaveHeroTeamResponse SaveHeroTeam(LocalServerContext ctx, SaveHeroTeamRequest req)
        {
            // 所有的卡是否在用户背包里（:83-99）
            var allCardIds = new List<int>();
            foreach (HeroTeamParam hero in req.HeroTeam)
            {
                if (hero.SkillCardIds.Count > 2)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 一个英雄最多带2个技能");
                    throw new LocalServerException("一个英雄最多带2个技能");
                }
                allCardIds.Add(hero.HeroCardId);
                foreach (int skillCardId in hero.SkillCardIds)
                {
                    allCardIds.Add(skillCardId);
                }
            }
            if (!CheckUserCardsIsOK(ctx, allCardIds))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 用户当前卡组不满足");
                throw new LocalServerException("用户当前卡组不满足");
            }

            // 对照 :38 TeamModel.TxGetByTeamId
            PlayerTeam team = GetTeamByTeamId(ctx, req.TeamId);
            if (team == null)
            {
                // 有意偏差：Go 对不存在的队伍零值行直接 Save，会插入 user_id=0/team_id=0 脏行（且之后仍查不到），
                // 此处按意图补上 UserId/TeamId 建档（同 NewPlayerInitializer/TalentHandler 的零值处理口径）
                team = new PlayerTeam
                {
                    Id = NextTeamId(ctx),
                    UserId = ctx.UserId,
                    TeamId = req.TeamId,
                    Cards = "[]",
                    CardsPosition = "[]",
                    AsCardTeam = "[]",
                };
                ctx.Store.Archive.Teams.Add(team);
            }

            // 对照 :39-44：序列化英雄编队（Go json tag 为 heroCardId/skillCardIds，与 NewPlayerInitializer 初始化格式一致）
            var heroTeams = new List<object>();
            foreach (HeroTeamParam hero in req.HeroTeam)
            {
                heroTeams.Add(new { heroCardId = hero.HeroCardId, skillCardIds = new List<int>(hero.SkillCardIds) });
            }
            team.HeroTeam = JsonConvert.SerializeObject(heroTeams);
            // 对照 :45 team.TransactionSave(tx)
            ctx.Store.MarkDirty(PlayerArchive.TeamsFieldNumber);

            // 对照 http/team_converter.go:15-19 toSaveHeroTeamResponse：单队包成列表返回
            var resp = new SaveHeroTeamResponse();
            resp.Teams.Add(team);
            return resp;
        }

        // 对照 TeamModel.CheckUserCardsIsOK（:119-130）+ CardModel.CheckUserCardsIsOK（PlayerCardModel.go:222-248）
        private bool CheckUserCardsIsOK(LocalServerContext ctx, List<int> cardIds)
        {
            if (cardIds.Count == 0)
            {
                return true;
            }
            Tables tables = DataConfigManager.Tables;
            foreach (int cardId in cardIds)
            {
                if (tables.TbCardBaseConfig.GetOrDefault(cardId) == null)
                {
                    return false;
                }
            }

            // 用户卡数量
            var userCardCountMap = new Dictionary<int, int>();
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                userCardCountMap[card.CardId] = card.Num;
            }
            // 编队卡数量（utils.SliceGroup4Count）
            var teamCardCountMap = new Dictionary<int, int>();
            foreach (int cid in cardIds)
            {
                teamCardCountMap.TryGetValue(cid, out int count);
                teamCardCountMap[cid] = count + 1;
            }
            // 检测编队要的卡，用户卡组是否满足
            foreach (var kv in teamCardCountMap)
            {
                if (!userCardCountMap.TryGetValue(kv.Key, out int have))
                {
                    return false;
                }
                if (have < kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        // 对照 TeamModel.CheckCardsPosition（:132-142）：站位值 >10 视为卡Id，必须是英灵卡
        // Go 此处 cfg 为 nil 时解引用 panic，本地 fail fast 同样直接抛
        private bool CheckCardsPosition(List<int> cards)
        {
            foreach (int cid in cards)
            {
                if (cid > 10)
                {
                    CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cid);
                    if (cfg.Type != CardType.Soldier)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // 对照 utils/SensitiveWords.go:17-26 SensitiveWordsMatch
        private bool SensitiveWordsMatch(string input)
        {
            foreach (SensitiveWordsConfig cfg in DataConfigManager.Tables.TbSensitiveWordsConfig.DataList)
            {
                if (input.Contains(cfg.Character))
                {
                    return true;
                }
            }
            return false;
        }

        // 对照 TeamModel.GetByTeamId（:99-107）：查不到返回 null（Go 返回 UserId=0 零值行）
        private PlayerTeam GetTeamByTeamId(LocalServerContext ctx, int teamId)
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

        // 本地替代 Go DB 自增主键：取现有最大 Id + 1
        private int NextTeamId(LocalServerContext ctx)
        {
            int max = 0;
            foreach (PlayerTeam team in ctx.Store.Archive.Teams)
            {
                if (team.Id > max)
                {
                    max = team.Id;
                }
            }
            return max + 1;
        }
    }
}
