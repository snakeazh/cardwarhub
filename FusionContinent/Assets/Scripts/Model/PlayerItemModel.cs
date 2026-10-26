using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Newtonsoft.Json;
using Cardwar;
using FusionContinent.Utils;
using Game.CombatPower;
using Game.RedDot;

namespace FusionContinent.Model
{
    public class Items
    {
        // 更新道具（统一接口）
        public static void Update(ItemGroup itemGroup,bool showNewCard = true)
        {
            if (itemGroup == null)
            {
                return;
            }
            if (itemGroup.Items != null)
            {
                foreach (var kv in itemGroup.Items)
                {
                    var itemId = kv.Key;
                    Global.PlayerItems[itemId] = kv.Value;
                    Global.Publisher.Publish($"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}", kv.Value.ItemNum);
                }

                if (itemGroup.Items.Count > 0)
                {
                    Global.Publisher.Publish($"{MessagePublisher.CHANGE_ITEM_CHANGE}", null);
                }
            }

            if (itemGroup.Cards != null)
            {
                bool checkNewCard = true;
                foreach (var kv in itemGroup.Cards)
                {
                    var newCard = new PlayerCard(kv.Value);
                    if (checkNewCard && showNewCard)
                    { 
                        var hadOwnedCard = Global.PlayerCards.TryGetValue(kv.Key, out var oldCard) && oldCard is { IsHave: true };
                        if (!hadOwnedCard)
                        {
                            if (newCard.Config.Type == CardType.Soldier)
                            {
                                checkNewCard = false;
                                LogTool.Log($"[Items.Update] 首次获得角色卡: CardId={newCard.CardId}");
                            }
                            else if (newCard.Config.Type == CardType.GeneralSkill)
                            {
                                LogTool.Log($"[Items.Update] 首次获得奥术卡: CardId={newCard.CardId}");
                                checkNewCard = false;
                            }
                        }
                        
                    }
                   

                    Global.PlayerCards[kv.Key] = newCard;
                    Global.Publisher.Publish($"{MessagePublisher.CHANGE_CARD_DATA}{kv.Key}", Global.PlayerCards[kv.Key]);
                }

                List<int> delKeys = new List<int>();
                foreach (var kv in Global.PlayerCards)
                {
                    if (kv.Value.Num <= 0)
                    {
                        delKeys.Add(kv.Key);
                    }
                }
                foreach (var cardsKey in delKeys)
                {
                    Global.PlayerCards.Remove(cardsKey);
                }
                if (itemGroup.Cards.Count > 0)
                {
                    Global.Publisher.Publish($"{MessagePublisher.CHANGE_CARD_CHANGE}", null);
                }
            }

            if (itemGroup.Equips != null)
            {
                foreach (var equip in itemGroup.Equips)
                {
                    Global.PlayerEquips.Add(equip.Id, new PlayerEquip(equip));
                }
            }
            
            if (itemGroup.SpeakerSkins is { Count: > 0 })
            {
                foreach (var skin in itemGroup.SpeakerSkins)
                {
                    Global.PlayerCycleSpeakerSkins[skin.Id] = new PlayerCycleSpeakerSkin(skin);
                }
            }
            
            if (itemGroup.Resource != null && itemGroup.Resource.Id != 0)
            {
                Global.PlayerResource = itemGroup.Resource;
                Global.Publisher.Publish(MessagePublisher.CHANGE_STAMINA, Global.PlayerResource.Stamina);
            }
            
            if (itemGroup.UserLevelInfo != null && itemGroup.UserLevelInfo.Level != 0)
            {
                Global.User.SetLevelInfo(itemGroup.UserLevelInfo);
            }
            
            if (itemGroup.Activity != null && itemGroup.Activity.Id != 0)
            {
                Global.PlayerActivity = itemGroup.Activity;
                
                Global.Publisher.Publish(MessagePublisher.CHANGE_ACTIVITY_INFO, Global.PlayerResource.Stamina);

            }
            
            // 任务活跃度
            if (itemGroup.TaskActiveRewards is { Count: > 0 })
            {
                foreach (var v in itemGroup.TaskActiveRewards)
                {
                    Global.PlayerTaskActives[v.Key] = new PlayerTaskActiveRewards(v.Key, v.Value);
                }
            }
            
            // 任务进度/状态（ResultItems.TaskDetails 增量同步）
            if (itemGroup.Tasks is { Count: > 0 })
            {
                foreach (var v in itemGroup.Tasks)
                {
                    Global.PlayerTasks[v.Key] = v.Value;
                }
                Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TaskPageDayBtn, null);
            }
            
            // 融合石
            if (itemGroup.FusionGemsV2 is { Count: > 0 })
            {
                foreach (var v in itemGroup.FusionGemsV2)
                {
                    Global.PlayerFusionGems.Add(v.Id, new PlayerFusionGem(v));
                }

                Global.Publisher.Publish(MessagePublisher.FUSION_GEM_WEAR_CHANGED, 0);
            }

            if (itemGroup.Weapons is { Count: > 0 })
            {
                foreach (var weapon in itemGroup.Weapons)
                {
                    Global.PlayerWeapons[weapon.Id] = new PlayerWeapon(weapon);
                }
            }

            if (itemGroup.Avatars is { Count: > 0 })
            {
                foreach (var avatar in itemGroup.Avatars)
                {
                    Global.PlayerAvatars[avatar.Id] = new PlayerAvatar(avatar);
                }
            }

            if (itemGroup.AvatarFrames is { Count: > 0 })
            {
                foreach (var avatarFrame in itemGroup.AvatarFrames)
                {
                    Global.PlayerAvatarFrames[avatarFrame.Id] = new PlayerAvatarFrame(avatarFrame);
                }
            }

            if (itemGroup.CombatPower.HasValue)
            {
                CombatPowerChangeHelper.ApplyServerPower(itemGroup.CombatPower.Value, showTipIfIncreased: true);
            }

        }
        
        // 将 Proto 的 ResultItems 转为本地 ItemGroup
        public static ItemGroup ToItemGroup(ResultItems protoItems)
        {
            if (protoItems == null) return null;

            var group = new ItemGroup();

            if (protoItems.Items != null && protoItems.Items.Count > 0)
            {
                group.Items = protoItems.Items.ToDictionary(
                    kv => kv.Key,
                    kv => new FusionContinent.Model.PlayerItemModel
                    {
                        Id = kv.Value.Id,
                        UserId = kv.Value.UserId,
                        ItemId = kv.Value.ItemId,
                        ItemNum = kv.Value.ItemNum
                    });
            }

            if (protoItems.Cards != null && protoItems.Cards.Count > 0)
            {
                group.Cards = protoItems.Cards.ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        var card = ProtoMappers.MapPlayerCard(kv.Value);
                        card.IsHave = kv.Value.Num > 0;
                        return card;
                    });
            }

            if (protoItems.Equips != null && protoItems.Equips.Count > 0)
            {
                group.Equips = protoItems.Equips
                    .Select(ProtoMappers.MapPlayerEquip)
                    .Where(model => model != null)
                    .ToList();
            }

            if (protoItems.Resource != null)
            {
                group.Resource = new FusionContinent.Model.PlayerResource
                {
                    Id = protoItems.Resource.Id,
                    Stamina = protoItems.Resource.Stamina,
                    StaminaTime = DateTime.TryParse(protoItems.Resource.StaminaTime, out var dtR) ? dtR : DateTime.MinValue
                };
            }

            if (protoItems.Activity != null)
            {
                group.Activity = new FusionContinent.Model.PlayerActivityModel
                {
                    Id = protoItems.Activity.Id,
                    DailyCharge = protoItems.Activity.DailyCharge,
                    PassCheckNum = protoItems.Activity.PassCheckNum,
                    CreateSinginDate = DateTime.TryParse(protoItems.Activity.CreateSinginDate, out var dtA) ? dtA : DateTime.MinValue,
                    AccumulateCharge = protoItems.Activity.AccumulateCharge
                };
            }

            if (protoItems.UserLevelInfo != null)
            {
                group.UserLevelInfo = new FusionContinent.Model.UserLevelInfo(protoItems.UserLevelInfo.Level, protoItems.UserLevelInfo.Exp);
            }

            if (protoItems.SpeakerSkins != null && protoItems.SpeakerSkins.Count > 0)
            {
                group.SpeakerSkins = protoItems.SpeakerSkins
                    .Select(ProtoMappers.MapPlayerCycleSpeakerSkin)
                    .Where(skin => skin != null)
                    .ToList();
            }

            if (protoItems.FusionGemsV2 != null && protoItems.FusionGemsV2.Count > 0)
            {
                group.FusionGemsV2 = protoItems.FusionGemsV2.Select(g => new FusionContinent.Model.PlayerFusionGem
                {
                    Id = g.Id,
                    GemId = g.GemId,
                    WearerId = g.WearerId,
                    Level = g.Level,
                    Progress = (float)g.Progress
                }).ToList();
            }

            if (protoItems.Weapons != null && protoItems.Weapons.Count > 0)
            {
                group.Weapons = protoItems.Weapons
                    .Select(ProtoMappers.MapPlayerWeapon)
                    .Where(weapon => weapon != null)
                    .ToList();
            }

            if (protoItems.Avatars != null && protoItems.Avatars.Count > 0)
            {
                group.Avatars = protoItems.Avatars
                    .Select(ProtoMappers.MapPlayerAvatar)
                    .Where(avatar => avatar != null)
                    .ToList();
            }

            if (protoItems.AvatarFrames != null && protoItems.AvatarFrames.Count > 0)
            {
                group.AvatarFrames = protoItems.AvatarFrames
                    .Select(ProtoMappers.MapPlayerAvatarFrame)
                    .Where(avatarFrame => avatarFrame != null)
                    .ToList();
            }

            if (protoItems.TaskActiveRewards != null && protoItems.TaskActiveRewards.Count > 0)
            {
                group.TaskActiveRewards = new Dictionary<TaskType, FusionContinent.Model.PlayerTaskActiveRewards>();
                foreach (var kv in protoItems.TaskActiveRewards)
                {
                    var type = (TaskType)kv.Key;
                    var detail = kv.Value;
                    var tmp = new FusionContinent.Model.PlayerTaskActiveRewards(type, null);
                    tmp.Progress = detail.Progress;
                    tmp.ReceiveRews = detail.ReceiveRews.ToList();
                    group.TaskActiveRewards[type] = tmp;
                }
            }

            if (protoItems.TaskDetails != null && protoItems.TaskDetails.Count > 0)
            {
                group.Tasks = new Dictionary<int, FusionContinent.Model.PlayerTask>();
                foreach (var kv in protoItems.TaskDetails)
                {
                    try
                    {
                        var task = ProtoMappers.MapPlayerTask(kv.Key, kv.Value);
                        if (task != null)
                        {
                            group.Tasks[kv.Key] = task;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogTool.LogWarning($"[Items.ToItemGroup] 跳过无效任务 TaskId={kv.Key}: {ex.Message}");
                    }
                }
            }

            if (protoItems.CombatPower != null)
            {
                group.CombatPower = (int)protoItems.CombatPower.CombatPower_;
            }

            return group;
        }

        // 重载：直接处理 Proto 的 ResultItems
        public static void Update(ResultItems protoItems, bool showNewCard = true)
        {
            var group = ToItemGroup(protoItems);
            Update(group, showNewCard);
        }
        
        public static string GetRewardStr(int itemId, int itemNum)
        {
            var cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
            return $"{(itemNum > 0 ? itemNum : -itemNum)}{cfg.Name}";
        }
        
        public static string GetRewardsStr(Dictionary<int, int> items, string split = ", ")
        {
            var str = "";
            foreach (var v in items)
            {
                str += GetRewardStr(v.Key, v.Value) + split;
            }
            return str;
        }
        
        public static string GetRewardsStr(List<Config.RewardAndNum> rewardItems, string split = ", ")
        {
            var str = "";
            if (rewardItems.Count == 1)
            {
                str += GetRewardStr(rewardItems.First().Id, rewardItems.First().Num);
                return str;
            }
            foreach (var v in rewardItems)
            {
                str += GetRewardStr(v.Id, v.Num) + split;
            }
            return str;
        }
        
        

        /**
         * 检查是否足够
         * 
         * @return 
         * @author Zpp
         * @create 2025/10/31
         **/
        public static bool CheckEnough(Dictionary<int, int> items)
        {
            foreach (var v in items)
            {
                if (Global.GetItemNumById(v.Key) < Math.Abs(v.Value))
                {
                    return false;
                }
            }
            
            return true;
        }
        public static bool CheckEnough(List<Config.RewardAndNum> items)
        {
            foreach (var v in items)
            {
                if (Global.GetItemNumById(v.Id) < Math.Abs(v.Num))
                {
                    return false;
                }
            }
            
            return true;
        }
    }

    public class PlayerItemModel
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int ItemId { get; set; } // 道具 ID
        public long ItemNum { get; set; } // 道具 数量
        
    }

    public class ItemGroup
    {
        public Dictionary<int, PlayerItemModel> Items { get; set; }
        public Dictionary<int, PlayerCard> Cards { get; set; }
        public List<PlayerEquipModel> Equips { get; set; }
        public List<PlayerFusionGem> FusionGemsV2 { get; set; }
        public List<PlayerWeapon> Weapons { get; set; }
        public List<PlayerAvatar> Avatars { get; set; }
        public List<PlayerAvatarFrame> AvatarFrames { get; set; }
        public PlayerResource Resource { get; set; }
        public PlayerActivityModel Activity { get; set; }
        public UserLevelInfo UserLevelInfo { get; set; }
        public Dictionary<TaskType, PlayerTaskActiveRewards> TaskActiveRewards { get; set; }
        public List<PlayerCycleSpeakerSkin> SpeakerSkins { get; set; }
        /// <summary>服务端下发的最新账号战力；null 表示本包无战力变化。</summary>
        public int? CombatPower { get; set; }
        /// <summary>服务端下发的任务进度/状态变更（ResultItems.TaskDetails）。</summary>
        public Dictionary<int, PlayerTask> Tasks { get; set; }
    }

}
