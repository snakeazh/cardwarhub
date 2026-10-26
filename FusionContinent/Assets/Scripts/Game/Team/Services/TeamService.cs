using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Newtonsoft.Json;
using Cardwar;
using FusionContinent.Utils;
using UIFramework.Services;
using PlayerTeam = FusionContinent.Model.PlayerTeam;

namespace Game.Team
{
    /// <summary>
    /// PVP 对侧编队构建结果：士兵卡与站位映射。
    /// </summary>
    public class OpponentBattleTeamBuildResult
    {
        public List<BattleCardGroup> SoldierTeams = new List<BattleCardGroup>();
        public List<int> CardsPosition = new List<int>();
    }

    /// <summary>
    /// 编队服务，统一管理编队缓存、出战队伍构建和编队保存。
    /// </summary>
    public class TeamService : GameServiceBase
    {
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[TeamService] 初始化完成");
        }

        public PlayerTeam GetTeamByTeamId(int teamId)
        {
            if (Global.PlayerTeames.TryGetValue(teamId, out var team))
            {
                return team;
            }

            return new PlayerTeam
            {
                TeamId = teamId
            };
        }

        public bool BuildBattleTeamsForCurrentTalent()
        {
            var talent = Talent.GetUseTalent();
            var team = GetTeamByTeamId(talent.TalentId);

            var isEmpty = true;
            var skillIdx = 0;
            var soldierIdx = 0;

            Global.SoldierTeams.Clear();
            Global.SoldierSkillTeams.Clear();

            foreach (var cardId in team.Cards)
            {
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                var cardInfo = Global.GetCardById(cardId);
                if (cardCfg == null || cardInfo == null)
                {
                    continue;
                }

                var cardGroup = new BattleCardGroup(cardId, cardInfo.Lv, cardCfg.Type, 0)
                {
                    IsUnlock =
                    {
                        Value = cardCfg.UnLockLevel == 0
                    }
                };

                switch (cardCfg.Type)
                {
                    case CardType.Soldier:
                        isEmpty = false;
                        cardGroup.CardSlotIndex = soldierIdx;
                        var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardId);
                        if (soldierCfg != null)
                        {
                            cardGroup.MainAttrType = soldierCfg.MainAttrType;
                        }

                        Global.SoldierTeams.Add(cardGroup);
                        soldierIdx++;
                        break;

                    case CardType.GeneralSkill:
                        // 去佩戴：奥术卡不再编队佩戴，旧存档 team.Cards 中残留的奥术卡直接跳过，
                        // 统一由下方「所有已拥有奥术卡直接生效」逻辑加入
                        break;

                    case CardType.Skill:
                    case CardType.SkillPassive:
                        cardGroup.CardSlotIndex = skillIdx;
                        if (cardCfg.UnLockLevel > 0)
                        {
                            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
                            if (skillCfg != null && skillCfg.ActsOnCard > 0)
                            {
                                var soldierData = Global.GetCardById(skillCfg.ActsOnCard);
                                cardGroup.IsUnlock.Value = soldierData?.Lv >= cardCfg.UnLockLevel;
                            }
                        }

                        Global.SoldierSkillTeams.Add(cardGroup);
                        skillIdx++;
                        break;
                }
            }

            // 去佩戴：所有已拥有奥术卡直接生效（IsUnlock 语义与原 GeneralSkill 分支一致）
            foreach (var kv in Global.PlayerCards)
            {
                var card = kv.Value;
                if (card == null || card.Config == null || card.Config.Type != CardType.GeneralSkill || card.Num <= 0)
                {
                    continue;
                }

                var cardGroup = new BattleCardGroup(card.CardId, card.Lv, CardType.GeneralSkill, 0)
                {
                    IsUnlock =
                    {
                        Value = card.Config.UnLockLevel == 0
                    }
                };

                cardGroup.CardSlotIndex = skillIdx;
                if (card.Config.UnLockLevel > 0)
                {
                    var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(card.CardId);
                    if (skillCfg != null && skillCfg.ActsOnCard > 0)
                    {
                        var soldierData = Global.GetCardById(skillCfg.ActsOnCard);
                        cardGroup.IsUnlock.Value = soldierData?.Lv >= card.Config.UnLockLevel;
                    }
                }

                Global.SoldierSkillTeams.Add(cardGroup);
                skillIdx++;
            }

            return isEmpty;
        }

        /// <summary>
        /// 从已绑定的 BattleSession.Opponent 取编队；若未绑定则从 detail 现场构建。
        /// 优先使用 opponentInfo 初始化后的上下文。
        /// </summary>
        public OpponentBattleTeamBuildResult BuildOpponentBattleTeams(Cardwar.PlayerProfile detail)
        {
            if (BattleSession.Opponent != null && BattleSession.Opponent.Raw == detail)
            {
                return BattleSession.Opponent.ToTeamBuildResult();
            }

            if (detail == null)
            {
                LogTool.LogError("[TeamService] OpponentDetail 为空，无法构建对侧编队");
                return new OpponentBattleTeamBuildResult();
            }

            var ctx = OpponentBattleContext.FromProfile(detail);
            return ctx.ToTeamBuildResult();
        }

        public bool ValidateBattleTeamReady()
        {
            return !BuildBattleTeamsForCurrentTalent();
        }

        public int GetFightingNumById(int talentId)
        {
            var team = GetTeamByTeamId(talentId);
            var fightNum = 0;

            foreach (var cardId in team.Cards)
            {
                var card = Global.GetCardById(cardId);
                var baseCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (card == null || baseCfg == null)
                {
                    continue;
                }

                switch (baseCfg.Type)
                {
                    case CardType.Soldier:
                        var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardId);
                        fightNum += soldierCfg?.LevelPropertyList[card.Lv - 1]?.FightNum ?? 0;
                        break;

                    case CardType.Skill:
                        var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
                        fightNum += skillCfg?.LevelPropertyList[card.Lv - 1]?.FightNum ?? 0;
                        break;
                }
            }

            return fightNum;
        }


        public bool CheckSkillWear(int soliderId,int skillId)
        {
            int teamId = GlobalFunc.GetCommonTeamId();
            var teamData =  GetTeamByTeamId(teamId);
            if (teamData.HeroTeamList == null)
            {
                return false;
            }
            var heroTeam = teamData.HeroTeamList.Find(ele=>ele.HeroCardId == soliderId);
            if (heroTeam == null)
            {
                return false;
            }

            return heroTeam.SkillCardIds.Contains(skillId);
        }

        /// <summary>
        /// 获取英灵技能卡槽
        /// </summary>
        /// <param name="soliderId">英灵id</param>
        /// <param name="index">第几个槽位</param>
        /// <returns></returns>
        public int GetSoliderSkillSlotIndex(int soliderId,int index)
        {
            int teamId = GlobalFunc.GetCommonTeamId();
            var teamData =  GetTeamByTeamId(teamId);
            if (teamData.HeroTeamList == null)
            {
                return 0;
            }
            var heroTeam = teamData.HeroTeamList.Find(ele=>ele.HeroCardId == soliderId);
            if (heroTeam == null)
            {
                return 0;
            }

            if (index < 0 || heroTeam.SkillCardIds == null || index >= heroTeam.SkillCardIds.Count)
            {
                return 0;
            }

            return heroTeam.SkillCardIds[index];
        }
        
        /// <summary>
        /// 获取英灵技能卡槽
        /// </summary>
        /// <param name="soliderId">英灵id</param>
        /// <param name="index">第几个槽位</param>
        /// <returns></returns>
        public List<int> GetSoliderSkillSlotList(int soliderId)
        {
            int teamId = GlobalFunc.GetCommonTeamId();
            var teamData =  GetTeamByTeamId(teamId);
            if (teamData.HeroTeamList == null)
            {
                return null;
            }
            var heroTeam = teamData.HeroTeamList.Find(ele=>ele.HeroCardId == soliderId);
            if (heroTeam == null)
            {
                return null;
            }

            return heroTeam.SkillCardIds;
        }

        /// <summary>
        /// 设置英灵技能穿戴状态
        /// </summary>
        /// <param name="soldierId">英灵ID</param>
        /// <param name="skillId">技能ID</param>
        /// <param name="isWear">是否穿戴</param>
        /// <returns>保存是否成功</returns>
        public async UniTask<bool> SetSkillWearStateAsync(int soldierId, int skillId, bool isWear)
        {
            if (soldierId <= 0 || skillId <= 0)
            {
                LogTool.LogError($"[TeamService] 技能穿戴参数无效 soldierId={soldierId}, skillId={skillId}");
                return false;
            }

            var teamId = GlobalFunc.GetCommonTeamId();
            var teamData = GetTeamByTeamId(teamId);
            var heroTeam = teamData.HeroTeamList?.Find(team => team.HeroCardId == soldierId);
            var skillCardIds = heroTeam?.SkillCardIds != null
                ? new List<int>(heroTeam.SkillCardIds)
                : new List<int>();

            if (isWear)
            {
                if (!skillCardIds.Contains(skillId))
                {
                    skillCardIds.Add(skillId);
                }
            }
            else
            {
                skillCardIds.Remove(skillId);
            }

            var saveTeam = new HeroTeam
            {
                HeroCardId = soldierId,
                SkillCardIds = skillCardIds
            };

            return await SaveTeamHeroCardAsync(saveTeam);
        }
        /// <summary>
        /// 保存技能卡
        /// </summary>
        /// <param name="talentId"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public async UniTask<bool> SaveTeamHeroCardAsync(HeroTeam team)
        {
            try
            {
                var talentId = GlobalFunc.GetCommonTeamId();
                var currentTeam = GetTeamByTeamId(talentId);
                var heroTeamList = new List<HeroTeam>();
                if (currentTeam.HeroTeamList != null)
                {
                    foreach (var item in currentTeam.HeroTeamList)
                    {
                        heroTeamList.Add(item);
                    }
                }
  
                
                var searchTeam = heroTeamList.FindIndex(ele => ele.HeroCardId == team.HeroCardId);

                if (searchTeam < 0)
                {
                    heroTeamList.Add(team);
                }
                else
                {
                    heroTeamList[searchTeam] = team;
                }
                // var targetTeam = currentTeam.Find(ele => ele.HeroCardId == team.HeroCardId);
                // if (targetTeam != null)
                // {
                //     
                // }
                // 使用 Protobuf SaveHeroTeamRequest/SaveHeroTeamResponse
                var request = new SaveHeroTeamRequest
                {
                    TeamId = talentId
                };
                foreach (var ht in heroTeamList)
                {
                    var param = new HeroTeamParam { HeroCardId = ht.HeroCardId };
                    if (ht.SkillCardIds != null)
                    {
                        param.SkillCardIds.Add(ht.SkillCardIds);
                    }
                    request.HeroTeam.Add(param);
                }

                var proto = await WebDataFun.SendProtoPostRequest<SaveHeroTeamResponse>(ApiKeys.ApiTeamSaveHeroTeams, request);
                if (proto == null)
                {
                    return false;
                }

                var result = new List<PlayerTeamModel>();
                foreach (var pt in proto.Teams)
                {
                    var model = ProtoMappers.MapPlayerTeam(pt);
                    if (model != null)
                    {
                        result.Add(model);
                    }
                }

                ApplyTeamModels(result);
                Global.Publisher.Publish(MessagePublisher.TEAM_HERO_SKILL_WEAR_CHANGED, team);

                return true;
            }
            catch (Exception e)
            {
                LogTool.E(e);

                return false;
            }
     
        }

        public async UniTask<bool> SaveTeamAsync(PlayerTeam team, bool showToast = true)
        {
            try
            {
                if (team == null)
                {
                    throw new Exception("编队数据为空");
                }

                if (!HasSoldierCard(team.Cards))
                {
                    throw new Exception("请配置上阵英灵");
                }

                // 使用 Protobuf 协议 SaveTeamsRequest/SaveTeamsResponse
                var request = new SaveTeamsRequest();
                var param = new TeamParam
                {
                    TeamId = team.TeamId,
                    Name = team.Name == null ? string.Empty : team.Name,
                    CurrentUse = team.CurrentUse
                };
                if (team.Cards != null)
                {
                    foreach (var id in team.Cards)
                    {
                        param.Cards.Add(id);
                    }
                }
                if (team.CardsPosition != null)
                {
                    foreach (var pos in team.CardsPosition)
                    {
                        param.CardsPosition.Add(pos);
                    }
                }
                request.Teams.Add(param);

                var proto = await WebDataFun.SendProtoPostRequest<SaveTeamsResponse>(ApiKeys.ApiTeamSaveTeams, request);
                if (proto == null)
                {
                    return false;
                }

                var result = new List<PlayerTeamModel>();
                foreach (var pt in proto.Teams)
                {
                    var model = ProtoMappers.MapPlayerTeam(pt);
                    if (model != null)
                    {
                        result.Add(model);
                    }
                }

                ApplyTeamModels(result);

                if (showToast)
                {
                    FusionContinent.Toast.Show("保存卡组成功", 0.5f);
                }

                NotifyDataChanged();
                return true;
            }
            catch (Exception ex)
            {
                FusionContinent.Toast.Show(ex.Message);
                LogTool.LogError($"保存编队失败：{ex.Message}");
                return false;
            }
        }

        public void ApplyTeamModels(IEnumerable<PlayerTeamModel> models, bool clearExisting = true)
        {
 

            if (models == null)
            {
                NotifyDataChanged();
                return;
            }
            if (clearExisting)
            {
                Global.PlayerTeames.Clear();
            }
            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }

                var team = new PlayerTeam
                {
                    Id = model.Id,
                    UserId = model.UserId,
                    TeamId = model.TeamId,
                    Name = $"{model.TeamId}",
                    CurrentUse = model.CurrentUse,
                    Cards = DeserializeIntList(model.Cards),
                    CardsPosition = DeserializeIntList(model.CardsPosition),
                    HeroTeamList = JsonConvert.DeserializeObject<List<HeroTeam>>(model.HeroTeam),
                    AsCardTeam =model.AsCardTeam == null ? null:  JsonConvert.DeserializeObject<List<int>>(model.AsCardTeam),
                };

                Global.PlayerTeames[team.TeamId] = team;
            }

            NotifyDataChanged();
        }
        

        private static bool HasSoldierCard(IEnumerable<int> cards)
        {
            if (cards == null)
            {
                return false;
            }

            foreach (var cardId in cards)
            {
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg != null && cardCfg.Type == CardType.Soldier)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> DeserializeIntList(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<int>();
            }

            return JsonConvert.DeserializeObject<List<int>>(json) ?? new List<int>();
        }
    }
}
