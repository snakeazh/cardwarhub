using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Config;
using Game.CombatPower;
using Game.Team;
using QFramework;
using UIFramework.Services;
using UnityEngine;

namespace FusionContinent.Model
{
    
    public class PlayerArchive
    {
        public UserPlayer Player { get; set; }
   
        public PlayerBattle Battle { get; set; }
        public PlayerAtlas Atlas { get; set; }
        public PlayerSignin Signin { get; set; }
        public PlayerResource Resource { get; set; }
        public List<PlayerStage> Stages { get; set; }
        public List<PlayerChapter> Chapters { get; set; }
        
        public List<PlayerItemModel> Items { get; set; }
        public List<PlayerCard> Cards { get; set; }
        public List<PlayerChestPool> ChestPools { get; set; }
        public List<PlayerTeamModel> Teams { get; set; }
        public List<PlayerEquipModel> Equips { get; set; }
        public List<PlayerTalent> Talents { get; set; }        
        public List<PlayerShop> Shops { get; set; }
        public Dictionary<int, PlayerTask> Tasks { get; set; }
        public List<PlayerModule> Modules { get; set; }
        public Dictionary<TaskType, PlayerTaskActiveRewards> TaskActives { get; set; }
        public PlayerExchangeGift ExchangeGift { get; set; }
        public List<PlayerGoods> ShopGoods { get; set; }
        public PlayerChallenge Challenge { get; set; }
        public PlayerActivityModel Activity { get; set; }
        public List<PlayerFusionGem> FusionGemsV2 { get; set; }
        public List<PlayerEndlessTower> EndlessTowers { get; set; }
        public List<PlayerHiddenRealmTower> HiddenRealmTowers { get; set; }
        public List<PlayerChallengeBoss> ChallengeBoss { get; set; }
        public List<UserCreatePlayerActivityModel> CreatePlayerActivitys { get; set; }
        
        public PlayerSpeaker PlayerSpeakerModule { get; set; }
    }
    
    public class ArchiveModel
    {
        // 新增：直接使用 Protobuf 的 PlayerArchive 结构应用到全局（无 JSON 反序列化）
        public static void ApplyPlayerArchive(Cardwar.PlayerArchive archive)
        {
            if (archive == null) return;
            // 初始道具类型ID
            var itemConfigs = DataConfigManager.Tables.TbItemConfig.DataList;
            foreach (var cfg in itemConfigs)
            {
                Global.ItemTypeIds[cfg.Type] = cfg.Id;
            }
            // 玩家信息
            if (archive.Player != null)
            {
                Global.Player = FusionContinent.Utils.ProtoMappers.MapUserPlayer(archive.Player);
            }

            if (archive.PlayerSpeakerModule != null)
            {
                Global.UserPlayerSpeaker = FusionContinent.Utils.ProtoMappers.MapPlayerSpeakerModule(archive.PlayerSpeakerModule);
            }
            else
            {
                Global.UserPlayerSpeaker = new PlayerSpeaker();
            }

            Global.PlayerCycleSpeakerSkins.Clear();
            foreach (var skin in archive.CycleSpeakerSkins)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerCycleSpeakerSkin(skin);
                if (local != null)
                {
                    Global.PlayerCycleSpeakerSkins[local.Id] = new PlayerCycleSpeakerSkin(local);
                }
            }

            Global.PlayerWeapons.Clear();
            foreach (var weapon in archive.Weapons)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerWeapon(weapon);
                if (local != null)
                {
                    Global.PlayerWeapons[local.Id] = new PlayerWeapon(local);
                }
            }

            Global.PlayerAvatars.Clear();
            foreach (var avatar in archive.Avatars)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerAvatar(avatar);
                if (local != null)
                {
                    Global.PlayerAvatars[local.Id] = new PlayerAvatar(local);
                }
            }

            Global.PlayerAvatarFrames.Clear();
            foreach (var avatarFrame in archive.AvatarFrames)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerAvatarFrame(avatarFrame);
                if (local != null)
                {
                    Global.PlayerAvatarFrames[local.Id] = new PlayerAvatarFrame(local);
                }
            }

            // 战斗数据
            if (archive.Battle != null)
            {
                Global.PlayerBattle = FusionContinent.Utils.ProtoMappers.MapPlayerBattle(archive.Battle);
            }

            // 图鉴
            if (archive.Atlas != null)
            {
                Global.PlayerAtlas = FusionContinent.Utils.ProtoMappers.MapPlayerAtlas(archive.Atlas);
                Game.Challenge.RedDot.MonsterAtlasRedDotHelper.MarkDirty();
            }

            // 关卡/章节
            Global.PlayerStages.Clear();
            foreach (var s in archive.Stages)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerStage(s);
                if (local != null)
                {
                    Global.PlayerStages[s.StageId] = local;
                }
            }

            Global.PlayerChapters.Clear();
            foreach (var c in archive.Chapters)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerChapter(c);
                if (local != null)
                {
                    Global.PlayerChapters[c.ChapterId] = local;
                }
            }

            // 签到
            if (archive.Signin != null)
            {
                Global.PlayerSignin = FusionContinent.Utils.ProtoMappers.MapPlayerSignin(archive.Signin);
            }

            // 挑战数据
            if (archive.Challenge != null)
            {
                Global.PlayerChallenge = FusionContinent.Utils.ProtoMappers.MapPlayerChallenge(archive.Challenge);
            }

            // 道具
            Global.PlayerItems.Clear();
            foreach (var it in archive.Items)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerItem(it);
                if (local != null)
                {
                    Global.PlayerItems[local.ItemId] = local;
                }
            }

            // 卡牌
            Global.PlayerCards.Clear();
            foreach (var card in archive.Cards)
            {
                if (card.Num > 0)
                {
                    var local = FusionContinent.Utils.ProtoMappers.MapPlayerCard(card);
                    if (local != null)
                    {
                        Global.PlayerCards[card.CardId] = local;
                    }
                }
            }

            // 编队
            var teams = new List<PlayerTeamModel>();
            foreach (var t in archive.Teams)
            {
                var tm = FusionContinent.Utils.ProtoMappers.MapPlayerTeam(t);
                if (tm != null) teams.Add(tm);
            }
            Game.Team.TeamService service = GameServiceManager.GetOrCreate<Game.Team.TeamService>();
            service.ApplyTeamModels(teams);

            // 装备
            Global.PlayerEquips.Clear();
            foreach (var e in archive.Equips)
            {
                var model = FusionContinent.Utils.ProtoMappers.MapPlayerEquip(e);
                if (model != null)
                {
                    Global.PlayerEquips.Add(model.Id, new PlayerEquip(model));
                }
            }

            // 装备槽位
            Global.PlayerEquipSlots.Clear();
            foreach (var s in archive.EquipSlots)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerEquipSlot(s);
                if (local != null)
                {
                    Global.PlayerEquipSlots[local.PartType] = new PlayerEquipSlot(local);
                }
            }

            // 任务
            Global.PlayerTasks.Clear();
            foreach (var kv in archive.Tasks)
            {
                var task = FusionContinent.Utils.ProtoMappers.MapPlayerTask(kv.Key, kv.Value);
                if (task != null)
                {
                    Global.PlayerTasks[kv.Key] = task;
                }
            }

            // 任务活跃度
            Global.PlayerTaskActives.Clear();
            foreach (var kv in archive.TaskActives)
            {
                var ar = FusionContinent.Utils.ProtoMappers.MapPlayerTaskActiveRewards(kv.Key, kv.Value);
                if (ar != null)
                {
                    Global.PlayerTaskActives[(TaskType)kv.Key] = ar;
                }
            }

            // 商店与商品
            Global.PlayerShops.Clear();
            foreach (var s in archive.Shops)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerShop(s);
                if (local != null)
                {
                    Global.PlayerShops[local.ShopId] = local;
                }
            }

            Global.PlayerGoods.Clear();
            foreach (var g in archive.ShopGoods)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerShopGoods(g);
                if (local != null)
                {
                    Global.PlayerGoods[local.GoodsId] = local;
                }
            }

            // 关卡礼包
            Global.ChapterGiftPacks.Clear();
            foreach (var pack in archive.ChapterGiftPacks)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapChapterGiftPack(pack);
                if (local != null)
                {
                    Global.ChapterGiftPacks[local.ChapterId] = local;
                }
            }

            // 天赋
            Global.PlayerTalents.Clear();
            foreach (var t in archive.Talents)
            {
                Global.PlayerTalents[t.TalentId] = new PlayerTalent
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    TalentId = t.TalentId,
                    Lv = t.Lv,
                    ExtraLv = t.ExtraLv,
                    Status = t.Status
                };
            }

            // 资源
            if (archive.Resource != null)
            {
                Global.PlayerResource = FusionContinent.Utils.ProtoMappers.MapPlayerResource(archive.Resource);
                Global.Publisher.Publish(MessagePublisher.CHANGE_STAMINA, Global.PlayerResource.Stamina);
            }

            // 抽卡池
            Global.PlayerChestPools.Clear();
            foreach (var cp in archive.ChestPools)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerChestPool(cp);
                if (local != null)
                {
                    Global.PlayerChestPools[local.PoolId] = local;
                }
            }

            // 活动
            if (archive.Activity != null)
            {
                Global.PlayerActivity = FusionContinent.Utils.ProtoMappers.MapPlayerActivity(archive.Activity);
            }

            // 创角活动
            Global.CreatePlayerActivitys.Clear();
            foreach (var a in archive.CreatePlayerActivitys)
            {
                var model = FusionContinent.Utils.ProtoMappers.MapCreatePlayerActivity(a);
                if (model != null)
                {
                    Global.CreatePlayerActivitys[model.ActivityId] = model;
                }
            }

            // 融合石
            Global.PlayerFusionGems.Clear();
            foreach (var v in archive.FusionGemsV2)
            {
                var gem = FusionContinent.Utils.ProtoMappers.MapPlayerFusionGem(v);
                if (gem != null)
                {
                    Global.PlayerFusionGems.Add(gem.Id,new PlayerFusionGem(gem));
                }
            }

            // 奥数共鸣
            Global.PlayerArcResonances.Clear();
            foreach (var r in archive.ArcResonances)
            {
                var resonance = FusionContinent.Utils.ProtoMappers.MapPlayerArcResonance(r);
                if (resonance != null)
                {
                    Global.PlayerArcResonances[resonance.Id] = new PlayerArcResonance(resonance);
                }
            }

            // 城墙
            var seatService = GameServiceManager.GetOrCreate<Game.HeroSeat.SeatService>();
            seatService.ApplyArchive(
                archive.CityWall != null
                    ? FusionContinent.Utils.ProtoMappers.MapPlayerCityWallArchive(archive.CityWall)
                    : null);

            // 无尽塔
            Global.PlayerEndlessTowers.Clear();
            foreach (var t in archive.EndlessTowers)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerEndlessTower(t);
                if (local != null)
                {
                    Global.PlayerEndlessTowers[t.TowerId] = local;
                }
            }

            // 挑战Boss
            Global.PlayerChallengeBoss.Clear();
            foreach (var c in archive.ChallengeBoss)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerChallengeBoss(c);
                if (local != null)
                {
                    Global.PlayerChallengeBoss[c.BossId] = local;
                }
            }

            // 秘境塔
            Global.PlayerHiddenRealmTowers.Clear();
            foreach (var h in archive.HiddenRealmTowers)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerHiddenRealmTower(h);
                if (local != null)
                {
                    Global.PlayerHiddenRealmTowers[h.TowerId] = local;
                }
            }
            GameServiceManager.GetOrCreate<Game.Realm.RealmService>().SyncTowerData();

            // 模块
            Global.PlayerModules.Clear();
            foreach (var m in archive.Modules)
            {
                var local = FusionContinent.Utils.ProtoMappers.MapPlayerModule(m);
                if (local != null)
                {
                    Global.PlayerModules[m.ModuleId] = local;
                }
            }

            // 兑换礼包码
            if (archive.ExchangeGift != null)
            {
                Global.PlayerExchangeGift = FusionContinent.Utils.ProtoMappers.MapPlayerExchangeGift(archive.ExchangeGift);
            }

            GameServiceManager.GetOrCreate<Game.Ticket.TicketService>().ApplyArchive(archive.Tickets);

            // 账号战力以登录 User.combat_power 为准，此处仅同步服务缓存
            CombatPowerChangeHelper.SyncFromUserFightingNum();

            LogTool.Log("存档应用完成！（Proto）");
        }
    }
}