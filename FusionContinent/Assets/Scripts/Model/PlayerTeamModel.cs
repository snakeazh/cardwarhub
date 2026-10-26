using System.Collections.Generic;
using System.Linq;
using Config;
using Game.Team;
using UIFramework.Services;

namespace FusionContinent.Model
{
    public class PlayerTeamModel
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int TeamId { get; set; } // 编队ID
        public string Name { get; set; } // 名称
        public string Cards { get; set; } // 卡组
        public int CurrentUse { get; set; } // 当前使用状态
        public string CardsPosition { get; set; } // 卡组
        public string HeroTeam{ get; set; } // 卡组json
        
        public string AsCardTeam   { get; set; } // 奥术卡卡组json
        
    }

    public class HeroTeam
    {
        public int HeroCardId;
        public List<int> SkillCardIds;
    }
    
    public class PlayerTeam
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int TeamId { get; set; } // 编队ID
        public string Name { get; set; } // 名称
        public List<int> Cards = new List<int>(); // 卡组
        public int CurrentUse { get; set; } // 当前使用状态
        public List<int> CardsPosition  = new List<int>(); // 卡组位置

        public string HeroTeam{ get; set; } // 卡组json

        public List<HeroTeam> HeroTeamList;
        public List<int> AsCardTeam;
        /**
         * 深度拷贝
         */
        public PlayerTeam Clone()
        {
            return new PlayerTeam
            {
                Id = this.Id,
                UserId = this.UserId,
                TeamId = this.TeamId,
                Name = this.Name,
                Cards = new List<int>(this.Cards),
                CurrentUse = this.CurrentUse,
                CardsPosition = new List<int>(this.CardsPosition),
            };
        }
        
        // 获取佩戴装备列表
        public List<PlayerEquip> WearEquipList()
        {
            var equipList = new List<PlayerEquip>();
            int cardId = DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
            // 已佩戴的装备列表
            var wearEquips = Global.PlayerEquips.Values.Where(v => v.WearerId == cardId).ToList();
            equipList.AddRange(wearEquips);            
            // // 当前编队
            // var currentTeam = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            // foreach (var cardId in currentTeam.Cards)
            // {
            //     var card = Global.GetCardById(cardId);
            //     if (card?.Config == null) continue;
            //     
            //     switch (card.Config.Type)
            //     {
            //         case CardType.Soldier:
            //             // 已佩戴的装备列表
            //             var wearEquips = Global.PlayerEquips.Values.Where(v => v.WearerId == cardId).ToList();
            //             equipList.AddRange(wearEquips);
            //             break;
            //     }
            // }

            return equipList;
        }
        
        // 获取佩戴装备列表
        public List<PlayerEquip> WearEquipListByCardId(int needCardId = 0)
        {
            var equipList = new List<PlayerEquip>();
            //获取轮语者装备穿戴
            var wearEquips = Global.PlayerEquips.Values.Where(v => v.WearerId == needCardId).ToList();
            equipList.AddRange(wearEquips);  
            // 当前编队
            // foreach (var cardData in Global.PlayerCards)
            // {
            //     int cardId = cardData.Value.CardId;
            //     if (needCardId != 0 && needCardId != cardId)
            //     {
            //         continue;
            //     }
            //     var card = Global.GetCardById(cardId);
            //     if (card?.Config == null) continue;
            //     
            //     switch (card.Config.Type)
            //     {
            //         case CardType.Soldier:
            //             // 已佩戴的装备列表
            //             var wearEquips = Global.PlayerEquips.Values.Where(v => v.WearerId == cardId).ToList();
            //             equipList.AddRange(wearEquips);
            //             break;
            //     }
            // }

            return equipList;
        }
    }

    public static class Team
    {
        private static TeamService Service => GameServiceManager.GetOrCreate<TeamService>();

        public static PlayerTeam GetTeamByTeamId(int teamId)
        {
            return Service.GetTeamByTeamId(teamId);
        }

        public static bool InitTeams()
        {
            return Service.BuildBattleTeamsForCurrentTalent();
        }
        
        // 获取卡组战力
        public static int GetFightingNumById(int talentId)
        {
            return Service.GetFightingNumById(talentId);
        }
    }
}