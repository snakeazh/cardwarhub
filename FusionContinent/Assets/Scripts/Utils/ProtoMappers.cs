using System;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Model;
using Cardwar;
using Newtonsoft.Json;
using Config;

namespace FusionContinent.Utils
{
    public static class ProtoMappers
    {
        // 统一：将 Protobuf 的 User 转为本地 UserData
        public static UserData MapUser(User u)
        {
            if (u == null) return null;

            DateTime loginDt;
            DateTime createDt;
            DateTime.TryParse(u.LoginTime, out loginDt);

            var result = new UserData();
            result.UserId = u.UserId;

            var userName = u.UserName;
            if (userName == null)
            {
                userName = string.Empty;
            }
            result.UserName = userName;
            result.Avatar = u.Avatar;
            result.AvatarFrame = u.AvatarFrame;

            result.Level = u.Level;
            result.Exp = u.Exp;
            result.Sex = u.Sex;
            result.FightingNum = u.CombatPower;
            result.CombatNum = u.CombatPower;
            result.LoginTime = loginDt;
            return result;
        }

        public static FusionContinent.Model.ChatPushModel MapChatPush(Cardwar.ChatPush push)
        {
            if (push == null) return null;

            var model = new FusionContinent.Model.ChatPushModel();
            model.MsgId = push.MsgId;
            model.Timestamp = push.Timestamp;
            model.Sender = MapPlayerBaseInfo(push.Sender);
            model.Content = push.Content == null ? string.Empty : push.Content;

            switch (push.TargetCase)
            {
                case ChatPush.TargetOneofCase.ToPlayerId:
                    model.TargetType = FusionContinent.Model.ChatPushTargetType.Player;
                    model.ToPlayerId = push.ToPlayerId;
                    break;
                case ChatPush.TargetOneofCase.Channel:
                    model.TargetType = FusionContinent.Model.ChatPushTargetType.Channel;
                    model.Channel = push.Channel;
                    break;
                default:
                    model.TargetType = FusionContinent.Model.ChatPushTargetType.None;
                    break;
            }

            return model;
        }

        // 统一：将 Protobuf 的 UserPlayer 转为本地 UserPlayer
        public static FusionContinent.Model.UserPlayer MapUserPlayer(Cardwar.UserPlayer p)
        {
            if (p == null) return null;

            var model = new FusionContinent.Model.UserPlayer();
            model.Id = p.Id;

            var figure = p.Figure;
            if (figure == null)
            {
                figure = "[]";
            }
            model.Figure = figure;

            model.ModuleUnlockLv = p.ModuleUnlockLv;

            var guideFlows = p.GuideFlows;
            if (guideFlows == null)
            {
                guideFlows = "{}";
            }
            model.GuideFlows = guideFlows;

            var figList = JsonConvert.DeserializeObject<List<int>>(model.Figure);
            if (figList == null)
            {
                figList = new List<int>();
            }
            model.FigureData = figList;

            var guideDict = JsonConvert.DeserializeObject<Dictionary<int, GuideFlowStatus>>(model.GuideFlows);
            if (guideDict == null)
            {
                guideDict = new Dictionary<int, GuideFlowStatus>();
            }
            model.GuideFlowMap = guideDict;

            return model;
        }

        // 统一：将 Protobuf 的 PlayerShopGoods 转为本地 PlayerGoods
        public static FusionContinent.Model.PlayerGoods MapPlayerShopGoods(Cardwar.PlayerShopGoods g)
        {
            if (g == null) return null;

            var local = new FusionContinent.Model.PlayerGoods();
            local.Id = g.Id;
            local.UserId = g.UserId;
            local.ShopId = g.ShopId;
            local.GoodsId = g.GoodsId;
            local.BuyNum = g.BuyNum;
            local.LockStatus = g.LockStatus;
            return local;
        }

        public static FusionContinent.Model.ChapterGiftPack MapChapterGiftPack(Cardwar.ChapterGiftPack pack)
        {
            if (pack == null) return null;

            return new FusionContinent.Model.ChapterGiftPack
            {
                ChapterId = pack.CharpterId,
                IsPurchase = pack.IsPurchase,
            };
        }

        // 统一：将 Protobuf 的 PlayerChallenge 转为本地 PlayerChallenge
        public static FusionContinent.Model.PlayerChallenge MapPlayerChallenge(Cardwar.PlayerChallenge c)
        {
            if (c == null) return null;
            var local = new FusionContinent.Model.PlayerChallenge();
            local.Id = c.Id;
            local.DailyGemFusionNum = c.DailyGemFusionNum;
            var rews = c.GemFusionRewards;
            if (rews == null)
            {
                rews = "[]";
            }
            local.GemFusionRewards = rews;
            var list = JsonConvert.DeserializeObject<List<int>>(rews);
            if (list == null)
            {
                list = new List<int>();
            }
            local.GemFusionRewardList = list;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerItem 转为本地 PlayerItemModel
        public static FusionContinent.Model.PlayerItemModel MapPlayerItem(Cardwar.PlayerItem it)
        {
            if (it == null) return null;
            var local = new FusionContinent.Model.PlayerItemModel();
            local.Id = it.Id;
            local.UserId = it.UserId;
            local.ItemId = it.ItemId;
            local.ItemNum = it.ItemNum;
            return local;
        }

        public static CardDataStruct MapCardData(Cardwar.PlayerCard.Types.CardData data)
        {
            if (data == null)
            {
                return new CardDataStruct();
            }

            return new CardDataStruct
            {
                Skin = data.Skin,
                IsAwaked = data.IsAwaked,
                ReceiveDevelops = data.ReceiveDevelops.ToList(),
            };
        }

        // 统一：将 Protobuf 的 PlayerCard 转为本地 PlayerCard
        public static FusionContinent.Model.PlayerCard MapPlayerCard(Cardwar.PlayerCard c)
        {
            if (c == null) return null;

            var tmp = new FusionContinent.Model.PlayerCard
            {
                Id = c.Id,
                UserId = c.UserId,
                CardId = c.CardId,
                Lv = c.Lv,
                Exp = c.Exp,
                Num = c.Num,
                IsHave = true,
                ObjData = MapCardData(c.Data),
            };

            return new FusionContinent.Model.PlayerCard(tmp);
        }

        // 统一：将 Protobuf 的 PlayerTeam 转为本地 PlayerTeamModel
        public static FusionContinent.Model.PlayerTeamModel MapPlayerTeam(Cardwar.PlayerTeam t)
        {
            if (t == null) return null;
            var local = new FusionContinent.Model.PlayerTeamModel();
            local.Id = t.Id;
            local.UserId = t.UserId;
            local.TeamId = t.TeamId;
            local.Name = t.Name == null ? string.Empty : t.Name;
            local.Cards = t.Cards == null ? "[]" : t.Cards;
            local.CurrentUse = t.CurrentUse;
            local.CardsPosition = t.CardsPosition == null ? "[]" : t.CardsPosition;
            local.HeroTeam = t.HeroTeam == null ? "[]" : t.HeroTeam;
            local.AsCardTeam = t.AsCardTeam == null ? "[]" : t.AsCardTeam;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerEquip 转为本地 PlayerEquipModel
        public static FusionContinent.Model.PlayerEquipModel MapPlayerEquip(Cardwar.PlayerEquip e)
        {
            if (e == null) return null;
            var model = new FusionContinent.Model.PlayerEquipModel();
            model.Id = e.Id;
            model.UserId = e.UserId;
            model.EquipId = e.EquipId;
            model.WearerId = e.WearerId;
            model.PartType = (EquipPartType)e.PartType;
            model.Phase = e.Phase;
            model.Entrys = e.Entrys != null ? e.Entrys.ToList() : new List<int>();
            model.TempEntrys = e.TempEntrys != null ? e.TempEntrys.ToList() : new List<int>();
            model.Effects = e.Effects != null ? e.Effects.ToList() : new List<int>();
            model.TempEffects = e.TempEffects != null ? e.TempEffects.ToList() : new List<int>();
            return model;
        }

        // 统一：将 Protobuf 的 TaskDetail 转为本地 PlayerTask
        public static FusionContinent.Model.PlayerTask MapPlayerTask(int taskId, Cardwar.TaskDetail detail)
        {
            if (detail == null) return null;
            var tmp = new FusionContinent.Model.PlayerTask();
            tmp.Progress = detail.Progress;
            tmp.State = (TaskState)detail.State;
            var local = new FusionContinent.Model.PlayerTask(taskId, tmp);
            return local;
        }

        // 统一：将 Protobuf 的 ActiveDetail 转为本地 PlayerTaskActiveRewards
        public static FusionContinent.Model.PlayerTaskActiveRewards MapPlayerTaskActiveRewards(int typeKey, Cardwar.ActiveDetail detail)
        {
            if (detail == null) return null;
            var inner = new FusionContinent.Model.PlayerTaskActiveRewards((TaskType)typeKey, null);
            inner.Progress = detail.Progress;
            var list = new List<int>();
            if (detail.ReceiveRews != null)
            {
                foreach (var v in detail.ReceiveRews)
                {
                    list.Add(v);
                }
            }
            inner.ReceiveRews = list;
            var local = new FusionContinent.Model.PlayerTaskActiveRewards((TaskType)typeKey, inner);
            return local;
        }

        // 统一：将 Protobuf 的 PlayerChestPool 转为本地 PlayerChestPool
        public static FusionContinent.Model.PlayerChestPool MapPlayerChestPool(Cardwar.PlayerChestPool cp)
        {
            if (cp == null) return null;
            var local = new FusionContinent.Model.PlayerChestPool();
            local.Id = cp.Id;
            local.UserId = cp.UserId;
            local.PoolId = cp.PoolId;
            local.PromiseNum = cp.PromiseNum;
            local.TotalChestNum = cp.TotalChestNum;
            local.DailyChestNum = cp.DailyChestNum;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerTalent 转为本地 PlayerTalent
        public static FusionContinent.Model.PlayerTalent MapPlayerTalent(Cardwar.PlayerTalent t)
        {
            if (t == null) return null;
            var local = new FusionContinent.Model.PlayerTalent();
            local.Id = t.Id;
            local.UserId = t.UserId;
            local.TalentId = t.TalentId;
            local.Lv = t.Lv;
            local.ExtraLv = t.ExtraLv;
            local.Status = t.Status;
            return local;
        }

        // 统一：将 Protobuf 的 UserCreatePlayerActivity 转为本地 UserCreatePlayerActivityModel
        public static FusionContinent.Model.UserCreatePlayerActivityModel MapCreatePlayerActivity(Cardwar.UserCreatePlayerActivity a)
        {
            if (a == null) return null;
            var model = new FusionContinent.Model.UserCreatePlayerActivityModel();
            model.Id = a.Id;
            model.UserId = a.UserId;
            model.ActivityId = a.ActivityId;
            var pr = a.PartReceives;
            if (pr == null)
            {
                pr = "{}";
            }
            model.PartReceives = pr;
            model.Status = (FusionContinent.Model.CreatePlayerActivityStatus)a.Status;
            System.DateTime dt;
            if (System.DateTime.TryParse(a.CreateTime, out dt))
            {
                model.CreateTime = dt;
            }
            else
            {
                model.CreateTime = System.DateTime.MinValue;
            }
            model.SetData();
            return model;
        }

        // 统一：将 Protobuf 的 PlayerFusionGemToC 转为本地 PlayerFusionGem
        public static FusionContinent.Model.PlayerFusionGem MapPlayerFusionGem(Cardwar.PlayerFusionGemToC g)
        {
            if (g == null) return null;
            var local = new FusionContinent.Model.PlayerFusionGem();
            local.Id = g.Id;
            local.GemId = g.GemId;
            local.WearerId = g.WearerId;
            local.Level = g.Level;
            local.Progress = (float)g.Progress;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerArcResonance 转为本地 PlayerArcResonance
        public static FusionContinent.Model.PlayerArcResonance MapPlayerArcResonance(Cardwar.PlayerArcResonance r)
        {
            if (r == null) return null;
            var local = new FusionContinent.Model.PlayerArcResonance();
            local.Id = r.Id;
            local.UserId = r.UserId;
            local.ResonanceId = r.ResonanceId;
            local.Slot = r.Slot;
            local.Level = r.Level;
            local.Stage = r.Stage;
            local.Progress = r.Progress;
            return local;
        }

        public static FusionContinent.Model.PlayerCityWall MapPlayerCityWall(Cardwar.PlayerCityWall wall)
        {
            if (wall == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerCityWall
            {
                Level = wall.Level,
            };
        }

        public static FusionContinent.Model.PlayerCityWallSkill MapPlayerCityWallSkill(Cardwar.PlayerCityWallSkill skill)
        {
            if (skill == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerCityWallSkill
            {
                SkillId = skill.SkillId,
                Level = skill.Level,
            };
        }

        public static FusionContinent.Model.PlayerCityWallArchive MapPlayerCityWallArchive(Cardwar.PlayerCityWallArchive archive)
        {
            if (archive == null)
            {
                return null;
            }

            var local = new FusionContinent.Model.PlayerCityWallArchive();
            if (archive.Wall != null)
            {
                local.Wall = MapPlayerCityWall(archive.Wall);
            }

            foreach (var skill in archive.Skills)
            {
                var mapped = MapPlayerCityWallSkill(skill);
                if (mapped != null)
                {
                    local.Skills[mapped.SkillId] = mapped;
                }
            }

            local.SlotSkills.AddRange(archive.SlotSkills);
            return local;
        }

        public static FusionContinent.Model.PlayerWeapon MapPlayerWeapon(Cardwar.PlayerWeapon weapon)
        {
            if (weapon == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerWeapon
            {
                Id = weapon.Id,
                WeaponId = weapon.WeaponId,
                Level = weapon.Level,
            };
        }

        public static FusionContinent.Model.PlayerAvatar MapPlayerAvatar(Cardwar.PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerAvatar
            {
                Id = avatar.Id,
                AvatarId = avatar.AvatarId,
            };
        }

        public static FusionContinent.Model.PlayerAvatarFrame MapPlayerAvatarFrame(Cardwar.PlayerAvatarFrame avatarFrame)
        {
            if (avatarFrame == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerAvatarFrame
            {
                Id = avatarFrame.Id,
                AvatarFrameId = avatarFrame.AvatarFrameId,
            };
        }

        public static FusionContinent.Model.PlayerEquipSlot MapPlayerEquipSlot(Cardwar.PlayerEquipSlot s)
        {
            if (s == null) return null;
            var local = new FusionContinent.Model.PlayerEquipSlot();
            local.Id = s.Id;
            local.UserId = s.UserId;
            local.PartType = (EquipPartType)s.Slot;
            local.Level = s.Level;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerEndlessTower 转为本地 PlayerEndlessTower
        public static FusionContinent.Model.PlayerEndlessTower MapPlayerEndlessTower(Cardwar.PlayerEndlessTower t)
        {
            if (t == null) return null;
            var local = new FusionContinent.Model.PlayerEndlessTower();
            local.Id = t.Id;
            local.UserId = t.UserId;
            local.TowerId = t.TowerId;
            local.Floor = t.Floor;
            local.MapId = t.MapId;
            local.DailyChallengeNum = t.DailyChallengeNum;
            local.TotalChallengeNum = t.TotalChallengeNum;
            local.ClearNodes = t.ClearNodes == null ? "{}" : t.ClearNodes;
            local.DailyRewards = t.DailyRewards == null ? "[]" : t.DailyRewards;
            local.FloorRewards = t.FloorRewards == null ? "[]" : t.FloorRewards;
            local.ClearNodeList = JsonConvert.DeserializeObject<Dictionary<int, int>>(local.ClearNodes);
            if (local.ClearNodeList == null) local.ClearNodeList = new Dictionary<int, int>();
            local.DailyRewardList = JsonConvert.DeserializeObject<List<int>>(local.DailyRewards) ?? new List<int>();
            local.FloorRewardList = JsonConvert.DeserializeObject<List<int>>(local.FloorRewards) ?? new List<int>();
            return local;
        }

        // 统一：将 Protobuf 的 PlayerHiddenRealmTower 转为本地 PlayerHiddenRealmTower
        public static FusionContinent.Model.PlayerHiddenRealmTower MapPlayerHiddenRealmTower(Cardwar.PlayerHiddenRealmTower t)
        {
            if (t == null) return null;
            var local = new FusionContinent.Model.PlayerHiddenRealmTower();
            local.Id = t.Id;
            local.UserId = t.UserId;
            local.TowerId = t.TowerId;
            local.Floor = t.Floor;
            local.DailyStageSweepingNum = t.DailyStageSweepingNum;
            local.DailyRewards = t.DailyRewards == null ? "[]" : t.DailyRewards;
            local.FloorRewards = t.FloorRewards == null ? "[]" : t.FloorRewards;
            local.DailyRewardList = JsonConvert.DeserializeObject<List<int>>(local.DailyRewards) ?? new List<int>();
            local.FloorRewardList = JsonConvert.DeserializeObject<List<int>>(local.FloorRewards) ?? new List<int>();

            var floors = new List<FusionContinent.Model.HiddenRealmFloorData>();
            foreach (var f in t.Floors)
            {
                var floorData = new FusionContinent.Model.HiddenRealmFloorData();
                floorData.Floor = f.Floor;
                floorData.Cleared = f.Cleared;
                foreach (var e in f.Entries)
                {
                    floorData.Entries.Add(new FusionContinent.Model.HiddenRealmFloorEntryData
                    {
                        EntryType = e.EntryType,
                        Level = e.Level
                    });
                }
                floors.Add(floorData);
            }
            local.Floors = floors;

            return local;
        }

        // 统一：将 Protobuf 的 PlayerChallengeBoss 转为本地 PlayerChallengeBoss
        public static FusionContinent.Model.PlayerChallengeBoss MapPlayerChallengeBoss(Cardwar.PlayerChallengeBoss c)
        {
            if (c == null) return null;
            var local = new FusionContinent.Model.PlayerChallengeBoss();
            local.Id = c.Id;
            local.UserId = c.UserId;
            local.BossId = c.BossId;
            local.TotalScore = c.TotalScore;
            local.TotalDamage = c.TotalDamage;
            local.DailyChallengeNum = c.DailyChallengeNum;
            local.TotalChallengeNum = c.TotalChallengeNum;
            local.ScoreRewards = c.ScoreRewards == null ? "[]" : c.ScoreRewards;
            local.DailyRewards = c.DailyRewards == null ? "[]" : c.DailyRewards;
            local.ScoreRewardList = JsonConvert.DeserializeObject<List<int>>(local.ScoreRewards) ?? new List<int>();
            local.DailyRewardList = JsonConvert.DeserializeObject<List<int>>(local.DailyRewards) ?? new List<int>();
            local.Config = DataConfigManager.Tables.TbChallengeBossConfig.GetOrDefault(local.BossId);
            return local;
        }

        // 统一：将 Protobuf 的 PlayerModule 转为本地 PlayerModule
        public static FusionContinent.Model.PlayerModule MapPlayerModule(Cardwar.PlayerModule m)
        {
            if (m == null) return null;
            var local = new FusionContinent.Model.PlayerModule();
            local.Id = m.Id;
            local.UserId = m.UserId;
            local.ModuleId = m.ModuleId;
            local.Status = (FusionContinent.Model.PlayerModuleStatus)m.Status;
            return local;
        }

        // 统一：将 Protobuf 的 PlayerExchangeGift 转为本地 PlayerExchangeGift
        public static FusionContinent.Model.PlayerExchangeGift MapPlayerExchangeGift(Cardwar.PlayerExchangeGift eg)
        {
            if (eg == null) return null;
            var local = new FusionContinent.Model.PlayerExchangeGift();
            local.Id = eg.Id;
            var codes = eg.CodeExchanges;
            if (codes == null)
            {
                codes = "{}";
            }
            local.CodeExchanges = codes;
            local.CodeExchangesMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(local.CodeExchanges) ?? new Dictionary<string, int>();
            return local;
        }

        // 统一：将 Protobuf 的 PlayerActivity 转为本地 PlayerActivityModel
        public static FusionContinent.Model.PlayerActivityModel MapPlayerActivity(Cardwar.PlayerActivity a)
        {
            if (a == null) return null;

            var local = new FusionContinent.Model.PlayerActivityModel();
            local.Id = a.Id;
            local.DailyCharge = a.DailyCharge;
            local.PassCheckNum = a.PassCheckNum;
            local.AccumulateCharge = a.AccumulateCharge;
            System.DateTime ad;
            if (System.DateTime.TryParse(a.CreateSinginDate, out ad))
            {
                local.CreateSinginDate = ad;
            }
            else
            {
                local.CreateSinginDate = System.DateTime.MinValue;
            }
            return local;
        }

        // 统一：将 Protobuf 的 PlayerShop 转为本地 PlayerShop
        public static FusionContinent.Model.PlayerShop MapPlayerShop(Cardwar.PlayerShop s)
        {
            if (s == null) return null;

            var local = new FusionContinent.Model.PlayerShop();
            local.Id = s.Id;
            local.UserId = s.UserId;
            local.ShopId = s.ShopId;
            local.RefreshNum = s.RefreshNum;
            local.DiscountRate = s.DiscountRate;
            System.DateTime rt;
            if (System.DateTime.TryParse(s.RefreshTime, out rt))
            {
                local.RefreshTime = rt;
            }
            else
            {
                local.RefreshTime = System.DateTime.MinValue;
            }
            return local;
        }

        // 统一：将 Protobuf 的 PlayerResource 转为本地 PlayerResource
        public static FusionContinent.Model.PlayerResource MapPlayerResource(Cardwar.PlayerResource r)
        {
            if (r == null) return null;

            var local = new FusionContinent.Model.PlayerResource();
            local.Id = r.Id;
            local.Stamina = r.Stamina;
            System.DateTime st;
            if (System.DateTime.TryParse(r.StaminaTime, out st))
            {
                local.StaminaTime = st;
            }
            else
            {
                local.StaminaTime = System.DateTime.MinValue;
            }
            return local;
        }

        public static FusionContinent.Model.PlayerCycleSpeakerSkin MapPlayerCycleSpeakerSkin(
            Cardwar.PlayerCycleSpeakerSkin skin)
        {
            if (skin == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerCycleSpeakerSkin
            {
                Id = skin.Id,
                SkinId = skin.SkinId,
                Level = skin.Level,
            };
        }

        public static FusionContinent.Model.PlayerSpeaker MapPlayerSpeakerModule(Cardwar.PlayerSpeakerModule module)
        {
            if (module == null)
            {
                return null;
            }

            return new FusionContinent.Model.PlayerSpeaker
            {
                UseSkinId = module.UseSkinId,
                EquippedWeaponId = module.EquippedWeaponId,
            };
        }

        public static void ApplySpeakerModule(
            FusionContinent.Model.PlayerSpeaker target,
            Cardwar.PlayerSpeakerModule module)
        {
            if (target == null || module == null)
            {
                return;
            }

            target.UseSkinId = module.UseSkinId;
            target.EquippedWeaponId = module.EquippedWeaponId;
        }

        // 统一：将 Protobuf 的 PlayerBattle 转为本地 PlayerBattle
        public static FusionContinent.Model.PlayerBattle MapPlayerBattle(Cardwar.PlayerBattle b)
        {
            if (b == null) return null;

            var local = new FusionContinent.Model.PlayerBattle();
            local.Id = b.Id;
            local.Score = b.Score;
            local.CurrentStageId = b.CurrentStageId;
            local.ClearStageId = b.ClearStageId;
            local.ClearStageTime = b.ClearStageTime;
            local.DailyDoubleRewardNum = b.DailyDoubleRewardNum;
            local.DailyStageSweepingNum = b.DailyStageSweepingNum;
            local.Status = b.Status;

            System.DateTime dt;
            if (System.DateTime.TryParse(b.AfkTime, out dt))
            {
                local.AfkTime = dt;
            }
            else
            {
                local.AfkTime = System.DateTime.MinValue;
            }

            return local;
        }

        // 统一：将 Protobuf 的 PlayerAtlas 转为本地 PlayerAtlas
        public static FusionContinent.Model.PlayerAtlas MapPlayerAtlas(Cardwar.PlayerAtlas a)
        {
            if (a == null) return null;

            return new FusionContinent.Model.PlayerAtlas
            {
                Id = a.Id,
                MonsterAtlasMap = a.MonsterAtlas != null && a.MonsterAtlas.Count > 0
                    ? new Dictionary<int, int>(a.MonsterAtlas)
                    : new Dictionary<int, int>()
            };
        }

        // 统一：将 Protobuf 的 PlayerStage 转为本地 PlayerStage
        public static FusionContinent.Model.PlayerStage MapPlayerStage(Cardwar.PlayerStage s)
        {
            if (s == null) return null;

            var local = new FusionContinent.Model.PlayerStage();
            local.Id = s.Id;
            local.UserId = s.UserId;
            local.StageId = s.StageId;
            local.Score = s.Score;
            local.Status = s.Status;
            local.Condition1 = s.Condition1;
            local.Condition2 = s.Condition2;
            local.Condition3 = s.Condition3;
            local.PlayCount = s.PlayCount;
            local.ClearStageTime = s.ClearStageTime;
            
            return local;
        }

        // 统一：将 Protobuf 的 PlayerChapter 转为本地 PlayerChapter
        public static FusionContinent.Model.PlayerChapter MapPlayerChapter(Cardwar.PlayerChapter c)
        {
            if (c == null) return null;

            var local = new FusionContinent.Model.PlayerChapter();
            local.Id = c.Id;
            local.UserId = c.UserId;
            local.ChapterId = c.ChapterId;
            local.UnlockStageId = c.UnlockStageId;
            local.Status = c.Status;
            return local;
        }

        public static FusionContinent.Model.PlayerBaseInfoModel MapPlayerBaseInfo(Cardwar.PlayerBaseInfo info)
        {
            if (info == null) return null;

            return new FusionContinent.Model.PlayerBaseInfoModel
            {
                PlayerId = info.PlayerId,
                PlayerName = info.PlayerName == null ? string.Empty : info.PlayerName,
                Avatar = info.Avatar,
                AvatarFrame = info.AvatarFrame,
                Level = info.Level,
                CombatPower = info.CombatPower,
            };
        }

        public static FusionContinent.Model.FriendInfoModel MapFriendInfo(
            Cardwar.FriendListResponse.Types.FriendInfo info)
        {
            if (info == null) return null;

            return new FusionContinent.Model.FriendInfoModel
            {
                PlayerInfo = MapPlayerBaseInfo(info.PlayerInfo),
                FriendTime = info.FriendTime
            };
        }

        public static FusionContinent.Model.SearchPlayerInfoModel MapSearchPlayerInfo(
            Cardwar.SearchPlayerResponse.Types.SearchPlayerInfo info)
        {
            if (info == null) return null;

            return new FusionContinent.Model.SearchPlayerInfoModel
            {
                PlayerInfo = MapPlayerBaseInfo(info.PlayerInfo),
                IsFriend = info.IsFriend
            };
        }

        public static FusionContinent.Model.FriendApplyInfoModel MapFriendApplyInfo(
            Cardwar.ApplyListResponse.Types.ApplyInfo info)
        {
            if (info == null) return null;

            return new FusionContinent.Model.FriendApplyInfoModel
            {
                PlayerInfo = MapPlayerBaseInfo(info.PlayerInfo),
                ApplyTime = info.ApplyTime,
                ApplyStatus = info.ApplyStatus,
                ApplyId = info.ApplyId
            };
        }

        public static FusionContinent.Model.PlayerTicket MapPlayerTicket(Cardwar.PlayerTicket ticket)
        {
            if (ticket == null) return null;

            return new FusionContinent.Model.PlayerTicket
            {
                TicketId = ticket.TicketId,
                NextClaimTime = ticket.NextClaimTime,
            };
        }

        // 统一：将 Protobuf 的 PlayerSignin 转为本地 PlayerSignin
        public static FusionContinent.Model.PlayerSignin MapPlayerSignin(Cardwar.PlayerSignin s)
        {
            if (s == null) return null;

            var local = new FusionContinent.Model.PlayerSignin();
            local.Id = s.Id;
            local.SigninNum = s.SigninNum;

            System.DateTime dt;
            if (System.DateTime.TryParse(s.LastSinginDate, out dt))
            {
                local.LastSinginDate = dt;
            }
            else
            {
                local.LastSinginDate = System.DateTime.MinValue;
            }

            return local;
        }

        public static FusionContinent.Model.PlayerMailModel MapPlayerMail(Cardwar.PlayerMail mail)
        {
            if (mail == null) return null;

            return new FusionContinent.Model.PlayerMailModel
            {
                Id = mail.Id,
                MailType = mail.MailType,
                Title = mail.Title == null ? string.Empty : mail.Title,
                Content = mail.Content == null ? string.Empty : mail.Content,
                Rewards = MapRewardItems(mail.Rewards),
                IsRead = mail.IsRead,
                IsClaimed = mail.IsClaimed,
                SenderId = mail.SenderId,
                SenderName = mail.SenderName == null ? string.Empty : mail.SenderName,
                CreateTime = mail.CreateTime == null ? string.Empty : mail.CreateTime,
            };
        }

        public static List<RewardItem> MapRewardItems(IEnumerable<Cardwar.RewardAndNum> rewards)
        {
            var list = new List<RewardItem>();
            if (rewards == null)
            {
                return list;
            }

            foreach (var reward in rewards)
            {
                if (reward == null)
                {
                    continue;
                }

                list.Add(new RewardItem(reward.Id, reward.Num));
            }

            return list;
        }
    }
}
