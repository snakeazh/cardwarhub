using System;
using System.Collections.Generic;
using System.Globalization;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 统一道具入口，对照 Go application_player_items.go UniteAddItemsEntranceLogic。
    /// 流程：奖励组展开 → 装备等级映射 → 正数项展示快照 → 分组 → 库存校验 → 分组分发。
    /// 事务/行锁（ResourceLockService）不迁移，各分组同步顺序执行。
    /// ResultItems.CombatPower 由调用方填充。
    /// </summary>
    public class ItemEntrance
    {
        // 对照 application_player_items.go:40 expandRewardGroups(items, 5)
        private const int MaxExpandDepth = 5;

        private readonly LocalServerContext _ctx;

        /// <summary>
        /// 最近一次 Execute 展开+映射后的正数项（对照 Go ResultItems.ExpandedItems 内部字段），
        /// 供调用方做奖励展示（对应 Go 端各 application 的 Rewards 字段）。
        /// </summary>
        public Dictionary<int, long> ExpandedItems { get; private set; } = new Dictionary<int, long>();

        public ItemEntrance(LocalServerContext ctx)
        {
            _ctx = ctx;
        }

        // 对照 application_player_items.go:27-125 UniteAddItemsEntranceLogic
        public ResultItems Execute(Dictionary<int, long> items)
        {
            var result = new ResultItems();
            ExpandedItems = new Dictionary<int, long>();

            if (items.Count == 0)
            {
                return result;
            }

            // 随机奖励组展开（application_player_items.go:40）
            items = ExpandRewardGroups(items, MaxExpandDepth);

            // 装备等级映射（application_player_items.go:43）
            items = RemapEquipRewardsToLevel(items, _ctx.UserLv);

            // 仅正向道具用于客户端展示（application_player_items.go:45 + filterPositiveItems:127-136）
            ExpandedItems = FilterPositiveItems(items);

            // 道具映射（application_player_items.go:48）
            ItemGroup group = HandlerDataGroup(items);

            // 是否足够（application_player_items.go:51-54）
            if (!CheckEnoughLogic(group))
            {
                LogTool.LogWarning($"userId={_ctx.UserId} 道具数量不足");
                throw new LocalServerException("道具数量不足");
            }

            // 卡牌（application_player_items.go:57-61）
            if (group.Cards.Count > 0)
            {
                Dictionary<int, long> extraAddItemMap = HandleCards(group.Cards, result.Cards);
                // lo.Assign 语义：覆盖同 key 而非累加
                foreach (var kv in extraAddItemMap)
                {
                    group.Items[kv.Key] = kv.Value;
                }
            }

            // 装备（application_player_items.go:64-66，wearerId=0 未穿戴）
            if (group.Equips.Count > 0)
            {
                HandleEquips(group.Equips, result.Equips);
            }

            // 道具（application_player_items.go:69-71）
            if (group.Items.Count > 0)
            {
                HandleItems(group.Items, result.Items);
            }

            // 资源（application_player_items.go:74-80）
            if (group.Resource.Count > 0)
            {
                HandleResource(group.Resource, result);
            }

            // 经验（application_player_items.go:83-85）
            if (group.Exps.Count > 0)
            {
                HandleExps(group.Exps, result);
            }

            // 任务活跃度（application_player_items.go:88-90）
            if (group.Tasks.Count > 0)
            {
                HandleTasks(group.Tasks, result.TaskActiveRewards);
            }

            // 任务进度增量下发（替代 Go 的 GeneralParam/notices 推送）：含 Execute 内消耗类钩子产生的变更
            Dictionary<int, TaskDetail> taskChanges = _ctx.TaskEngine.ConsumePendingTaskChanges();
            foreach (var kv in taskChanges)
            {
                result.TaskDetails[kv.Key] = kv.Value;
            }

            // 融合石（application_player_items.go:93-105）
            if (group.FusionStones.Count > 0)
            {
                HandleFusionStones(group.FusionStones, result.FusionGemsV2);
            }

            // 轮语者皮肤（application_player_items.go:108-110）
            if (group.SpeakerSkins.Count > 0)
            {
                HandleSpeakerSkins(group.SpeakerSkins, result.SpeakerSkins);
            }

            // 头像（application_player_items.go:113-115）
            if (group.Avatars.Count > 0)
            {
                HandleAvatars(group.Avatars, result.Avatars);
            }

            // 头像框（application_player_items.go:118-120）
            if (group.AvatarFrames.Count > 0)
            {
                HandleAvatarFrames(group.AvatarFrames, result.AvatarFrames);
            }

            return result;
        }

        // 对照 PlayerItemService.go:81-88 ConsumeToItems → ConsumeToItemsRide(rewards, 1)
        public Dictionary<int, long> ConsumeToItems(IList<Config.RewardAndNum> rewards)
        {
            return ConsumeToItemsRide(rewards, 1);
        }

        // 对照 PlayerItemService.go:86-88 ConsumeToItemsRide（倍率，取负）
        public Dictionary<int, long> ConsumeToItemsRide(IList<Config.RewardAndNum> rewards, int rideNum)
        {
            return RewardToItemsRide(rewards, rideNum, true);
        }

        // 对照 PlayerItemService.go:71-73 RewardsToItems → RewardToItemsRide(rewards, 1, false)
        public Dictionary<int, long> RewardsToItems(IList<Config.RewardAndNum> rewards)
        {
            return RewardToItemsRide(rewards, 1, false);
        }

        // 对照 PlayerItemService.go:90-105 RewardToItemsRide（ceil 倍率，isConsume 取负）
        public Dictionary<int, long> RewardToItemsRide(IList<Config.RewardAndNum> rewards, double rideNum, bool isConsume)
        {
            var items = new Dictionary<int, long>();
            foreach (Config.RewardAndNum v in rewards)
            {
                long itemNum = (long)Math.Ceiling(v.Num * rideNum);
                if (isConsume)
                {
                    itemNum = (long)Math.Ceiling(-v.Num * rideNum);
                }
                Accumulate(items, v.Id, itemNum);
            }
            return items;
        }

        // 对照 PlayerItemService.go:108-111 CheckEnough（负数消耗项的持有量校验）
        public bool CheckEnough(Dictionary<int, long> consumeItems)
        {
            return CheckEnoughLogic(HandlerDataGroup(consumeItems));
        }

        // 对照 application_player_items.go:127-136 filterPositiveItems
        private Dictionary<int, long> FilterPositiveItems(Dictionary<int, long> items)
        {
            var output = new Dictionary<int, long>();
            foreach (var kv in items)
            {
                if (kv.Value > 0)
                {
                    output[kv.Key] = kv.Value;
                }
            }
            return output;
        }

        // 对照 application_player_items.go:143-185 expandRewardGroups
        // GroupId > 0 表示奖励组，按 TbRewardGroupConfig.RewardGroupList 权重随机抽取；
        // 仅正数展开（负数=消耗），maxDepth 防循环引用。
        private Dictionary<int, long> ExpandRewardGroups(Dictionary<int, long> items, int maxDepth)
        {
            if (maxDepth <= 0)
            {
                return items;
            }

            Tables tables = DataConfigManager.Tables;
            var result = new Dictionary<int, long>();
            foreach (var kv in items)
            {
                int itemId = kv.Key;
                long count = kv.Value;
                if (count <= 0)
                {
                    Accumulate(result, itemId, count);
                    continue;
                }

                ItemConfig cfg = tables.TbItemConfig.GetOrDefault(itemId);
                if (cfg == null || cfg.GroupId <= 0)
                {
                    Accumulate(result, itemId, count);
                    continue;
                }

                RewardGroupConfig groupCfg = tables.TbRewardGroupConfig.GetOrDefault(cfg.GroupId);
                if (groupCfg == null || groupCfg.RewardGroupList.Count == 0)
                {
                    Accumulate(result, itemId, count);
                    continue;
                }

                int totalWeight = 0;
                foreach (RewardGroupListStruct entry in groupCfg.RewardGroupList)
                {
                    totalWeight += entry.Weight;
                }
                if (totalWeight <= 0)
                {
                    Accumulate(result, itemId, count);
                    continue;
                }

                for (long i = 0; i < count; i++)
                {
                    int r = _ctx.Rng.Next(totalWeight);
                    int acc = 0;
                    foreach (RewardGroupListStruct entry in groupCfg.RewardGroupList)
                    {
                        acc += entry.Weight;
                        if (r < acc)
                        {
                            Accumulate(result, entry.ItemId, entry.ItemNum);
                            break;
                        }
                    }
                }
            }
            return ExpandRewardGroups(result, maxDepth - 1);
        }

        // 对照 equip_level_mapping.go:13-40 RemapEquipRewardsToLevel
        private Dictionary<int, long> RemapEquipRewardsToLevel(Dictionary<int, long> items, int userLevel)
        {
            Tables tables = DataConfigManager.Tables;
            var result = new Dictionary<int, long>();
            foreach (var kv in items)
            {
                ItemConfig itemCfg = tables.TbItemConfig.GetOrDefault(kv.Key);
                if (itemCfg == null || itemCfg.ModuleType != ModuleType.Equip)
                {
                    Accumulate(result, kv.Key, kv.Value);
                    continue;
                }

                EquipConfig srcEquipCfg = tables.TbEquipConfig.GetOrDefault(itemCfg.EffectValue);
                if (srcEquipCfg == null)
                {
                    Accumulate(result, kv.Key, kv.Value);
                    continue;
                }

                int bestEquipId = FindBestEquipForLevel(srcEquipCfg.Part, srcEquipCfg.Quality, userLevel);
                int targetItemId = FindItemIdByEffectValue(bestEquipId);
                if (targetItemId == 0)
                {
                    Accumulate(result, kv.Key, kv.Value);
                    continue;
                }
                Accumulate(result, targetItemId, kv.Value);
            }
            return result;
        }

        // 对照 equip_level_mapping.go:44-76 findBestEquipForLevel
        private int FindBestEquipForLevel(EquipPartType part, QualityType quality, int userLevel)
        {
            Tables tables = DataConfigManager.Tables;
            PlayerLevelConfig levelCfg = tables.TbPlayerLevelConfig.GetOrDefault(userLevel);
            if (levelCfg == null)
            {
                return 0;
            }
            int targetLevel = levelCfg.GetEquipLevel;

            int exactId = 0;
            int closestId = 0;
            int minDiff = -1;
            foreach (EquipConfig cfg in tables.TbEquipConfig.DataList)
            {
                if (cfg.Part != part || cfg.Quality != quality)
                {
                    continue;
                }
                if (cfg.EquipLevel == targetLevel)
                {
                    exactId = cfg.Id;
                }
                int diff = cfg.EquipLevel - targetLevel;
                if (diff < 0)
                {
                    diff = -diff;
                }
                if (minDiff == -1 || diff < minDiff)
                {
                    minDiff = diff;
                    closestId = cfg.Id;
                }
            }
            return exactId != 0 ? exactId : closestId;
        }

        // 对照 equip_level_mapping.go:79-86 findItemIdByEffectValue
        private int FindItemIdByEffectValue(int equipId)
        {
            foreach (ItemConfig cfg in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                if (cfg.ModuleType == ModuleType.Equip && cfg.EffectValue == equipId)
                {
                    return cfg.Id;
                }
            }
            return 0;
        }

        // 对照 PlayerItemService.go:179-278 HandlerDataGroup（按 ItemConfig.ModuleType 分组）
        private ItemGroup HandlerDataGroup(Dictionary<int, long> items)
        {
            var group = new ItemGroup();
            foreach (var kv in items)
            {
                int itemId = kv.Key;
                long itemNum = kv.Value;
                ItemConfig cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                if (cfg == null)
                {
                    LogTool.LogWarning($"错误的道具Id[{itemId}]未配置");
                    throw new LocalServerException($"错误的道具Id[{itemId}]未配置");
                }

                switch (cfg.ModuleType)
                {
                    case ModuleType.Card: // 卡牌
                        Accumulate(group.Cards, cfg.EffectValue, itemNum);
                        break;
                    case ModuleType.Equip: // 装备
                        Accumulate(group.Equips, cfg.EffectValue, itemNum);
                        break;
                    case ModuleType.Resource:
                        Accumulate(group.Resource, itemId, itemNum);
                        break;
                    case ModuleType.Exp:
                        Accumulate(group.Exps, itemId, itemNum);
                        break;
                    case ModuleType.Task:
                        Accumulate(group.Tasks, itemId, itemNum);
                        break;
                    case ModuleType.FusionStone:
                        Accumulate(group.FusionStones, cfg.EffectValue, itemNum);
                        break;
                    case ModuleType.PlayerSkin: // 轮语者皮肤
                        Accumulate(group.SpeakerSkins, cfg.EffectValue, itemNum);
                        break;
                    default: // 道具
                        if (cfg.Type == ItemType.PlayerAvatar)
                        {
                            Accumulate(group.Avatars, cfg.EffectValue, itemNum);
                        }
                        else if (cfg.Type == ItemType.PlayerAvatarFrame)
                        {
                            Accumulate(group.AvatarFrames, cfg.EffectValue, itemNum);
                        }
                        else
                        {
                            Accumulate(group.Items, itemId, itemNum);
                        }
                        break;
                }
            }
            return group;
        }

        // 对照 PlayerItemService.go:281-314 CheckEnoughLogic（仅 Cards/Items 预校验）
        private bool CheckEnoughLogic(ItemGroup group)
        {
            // 卡牌
            if (group.Cards.Count > 0)
            {
                var ownedCardIds = new HashSet<int>();
                foreach (PlayerCard card in _ctx.Store.Archive.Cards)
                {
                    ownedCardIds.Add(card.CardId);
                }
                foreach (var kv in group.Cards)
                {
                    if (kv.Value > 0)
                    {
                        continue;
                    }
                    if (!ownedCardIds.Contains(kv.Key))
                    {
                        return false;
                    }
                }
            }

            // 道具
            if (group.Items.Count > 0)
            {
                var itemMap = new Dictionary<int, PlayerItem>();
                foreach (PlayerItem item in _ctx.Store.Archive.Items)
                {
                    itemMap[item.ItemId] = item;
                }
                foreach (var kv in group.Items)
                {
                    if (kv.Value > 0)
                    {
                        continue;
                    }
                    if (!itemMap.TryGetValue(kv.Key, out PlayerItem item) || item.ItemNum < Math.Abs(kv.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // 对照 PlayerItemService.go:113-149 TxUniteAddItemsEntrance_ForItem
        private void HandleItems(Dictionary<int, long> addItemMap, Google.Protobuf.Collections.MapField<int, PlayerItem> resultItems)
        {
            var itemMap = new Dictionary<int, PlayerItem>();
            foreach (PlayerItem item in _ctx.Store.Archive.Items)
            {
                itemMap[item.ItemId] = item;
            }

            foreach (var kv in addItemMap)
            {
                int itemId = kv.Key;
                long itemNum = kv.Value;
                if (!itemMap.TryGetValue(itemId, out PlayerItem item))
                {
                    if (itemNum < 0)
                    {
                        LogTool.LogWarning($"userId={_ctx.UserId} 数量不足");
                        throw new LocalServerException("数量不足");
                    }

                    item = new PlayerItem
                    {
                        Id = NextItemId(),
                        UserId = _ctx.UserId,
                        ItemId = itemId,
                        ItemNum = itemNum,
                    };
                    _ctx.Store.Archive.Items.Add(item);
                    itemMap[itemId] = item;
                }
                else
                {
                    item.ItemNum += itemNum;
                    if (item.ItemNum < 0)
                    {
                        LogTool.LogWarning($"userId={_ctx.UserId} 数量不足");
                        throw new LocalServerException("数量不足");
                    }
                }

                // 返回变动后的完整行（key = itemId）
                resultItems[itemId] = item;

                if (itemNum < 0)
                {
                    // 消耗道具：任务进度 ConsumeSpecifyItemId（对照 PlayerItemService.go:143）
                    _ctx.TaskEngine.SetProgress(TaskTargetType.ConsumeSpecifyItemId, (int)Math.Abs(itemNum), itemId, false);
                }
                // Go 端 AddItemLog 流水表不迁移
            }
            _ctx.Store.MarkDirty(PlayerArchive.ItemsFieldNumber);
        }

        // 对照 PlayerCardService.go:33-89 TxUniteAddItemsEntrance_ForCard（重复卡转碎片）
        private Dictionary<int, long> HandleCards(Dictionary<int, long> addCardMap, Google.Protobuf.Collections.MapField<int, PlayerCard> resultCards)
        {
            var cardMap = new Dictionary<int, PlayerCard>();
            foreach (PlayerCard card in _ctx.Store.Archive.Cards)
            {
                cardMap[card.CardId] = card;
            }
            var extraAddItemMap = new Dictionary<int, long>();
            var addCards = new Dictionary<int, int>();

            foreach (var kv in addCardMap)
            {
                int cardId = kv.Key;
                long cardNum = kv.Value;
                if (cardId <= 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 卡牌ID不能为0");
                    throw new LocalServerException("卡牌ID不能为0");
                }

                CardBaseConfig cfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cfg == null)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 卡牌ID[{cardId}]不存在");
                    throw new LocalServerException($"卡牌ID[{cardId}], 不存在");
                }

                // 判断卡牌是否重复，并转换为碎片（PlayerCardService.go:52-68）
                // 奥术卡/技能卡/被动技能卡不会转换成碎片
                bool convertFragment = false;
                if (cfg.Type != CardType.GeneralSkill && cfg.Type != CardType.Skill && cfg.Type != CardType.SkillPassive)
                {
                    if (cardMap.ContainsKey(cardId))
                    {
                        convertFragment = true;
                        // 英灵卡如果已经存在 不可以转换成碎片
                        if (cfg.Type == CardType.Soldier)
                        {
                            convertFragment = false;
                        }
                    }
                    else if (cardNum > 1)
                    {
                        convertFragment = true;
                        cardNum--;
                    }
                }

                // 转换为碎片（PlayerCardService.go:70-82）
                if (convertFragment && cfg.ConvertFragment.Count > 0)
                {
                    foreach (Config.RewardAndNum v in cfg.ConvertFragment)
                    {
                        Accumulate(extraAddItemMap, v.Id, v.Num * cardNum);
                    }
                    continue;
                }
                addCards[cfg.Id] = (int)cardNum;
            }

            List<PlayerCard> cards = AddCardsLogic(addCards, true, extraAddItemMap);
            // lo.KeyBy 语义：key = cardId，同 key 后覆盖前
            foreach (PlayerCard card in cards)
            {
                resultCards[card.CardId] = card;
            }
            _ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            return extraAddItemMap;
        }

        // 对照 PlayerCardService.go:91-238 TxAddCardsLogic
        // 有意偏差：奥术卡（GeneralSkill）不再叠加 Num（Num 恒 1），重复获得走 ArcCardUpgradeHelper 升级/满级分解
        private List<PlayerCard> AddCardsLogic(Dictionary<int, int> addCards, bool isAllCard, Dictionary<int, long> extraAddItemMap)
        {
            Tables tables = DataConfigManager.Tables;
            var cards = new List<PlayerCard>();
            var cardIds = new HashSet<int>(addCards.Keys);
            // 对照 Go 从事务前 DB 快照取卡：本批新加的卡不参与后续技能卡判重
            var cardMap = new Dictionary<int, PlayerCard>();
            foreach (PlayerCard card in _ctx.Store.Archive.Cards)
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
                    continue;
                }

                bool exists = cardMap.TryGetValue(cid, out PlayerCard oldCard);
                if (cfg.Type == CardType.GeneralSkill)
                {
                    // 奥术卡：Num 恒 1，重复获得 Lv+1、满级转分解产物（ArcCardUpgradeHelper，与抽奖 Handler 同一行为）
                    if (cidNums <= 0)
                    {
                        // 负数消耗沿用旧语义（当前无消费奥术卡的入口，防御保留）
                        if (exists)
                        {
                            oldCard.Num += cidNums;
                            changeExistAsCards.Add(oldCard);
                        }
                        continue;
                    }
                    for (int i = 0; i < cidNums; i++)
                    {
                        ArcCardUpgradeHelper.ObtainOutcome outcome = _ctx.ArcCardUpgrade.ApplyObtained(_ctx, cid);
                        if (outcome.Kind == ArcCardUpgradeHelper.ObtainKind.MaxLevelDecomposed)
                        {
                            // 满级分解产物并入 extraAddItemMap，随后汇入 group.Items 统一发放
                            if (outcome.DecomposeRewards != null)
                            {
                                foreach (Config.RewardAndNum v in outcome.DecomposeRewards)
                                {
                                    Accumulate(extraAddItemMap, v.Id, v.Num);
                                }
                            }
                            continue;
                        }
                        changeExistAsCards.Add(outcome.Card);
                    }
                    continue;
                }

                // 不是奥术卡，如果已经存在则不添加
                if (exists)
                {
                    continue;
                }

                // 实际要添加的卡（proto 无 CreateTime 字段，不迁移）
                // 走到这里不可能是奥术卡（上方已 continue），数量永远只能有一张
                var addCard = new PlayerCard
                {
                    Id = NextCardId(),
                    UserId = _ctx.UserId,
                    CardId = cid,
                    Data = new PlayerCard.Types.CardData(),
                    Lv = 1,
                    Num = 1,
                };
                _ctx.Store.Archive.Cards.Add(addCard);
                cards.Add(addCard);

                // 获得英雄：附带全部已解锁技能卡（PlayerCardService.go:156-194）
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
                            Id = NextCardId(),
                            UserId = _ctx.UserId,
                            CardId = skillId,
                            Lv = 1,
                            Data = new PlayerCard.Types.CardData(),
                            Num = 1,
                        };
                        _ctx.Store.Archive.Cards.Add(skillCardObj);
                        cards.Add(skillCardObj);
                    }
                }
            }

            cards.AddRange(changeExistAsCards);
            return cards;
        }

        // 对照 PlayerResourceService.go:77-105 TxUniteAddItemsEntrance_ForResource（仅体力）
        private void HandleResource(Dictionary<int, long> addResource, ResultItems result)
        {
            PlayerResource resource = GetOrCreateResource();
            PlayerActivity activity = null;
            foreach (var kv in addResource)
            {
                if (kv.Key <= 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 资源id不能为0");
                    throw new LocalServerException("资源id不能为0");
                }

                // Go 端此处 cfg 为 nil 会解引用 panic，本地 fail fast 同样直接抛
                ItemConfig itemCfg = DataConfigManager.Tables.TbItemConfig.Get(kv.Key);
                if (itemCfg.Type == ItemType.Stamina) // 体力：可扣负、不 clamp
                {
                    resource.Stamina += (int)kv.Value;
                    if (kv.Value < 0) // 消耗体力
                    {
                        activity = GetOrCreateActivity();
                        activity.PassCheckNum += (int)Math.Abs(kv.Value); // 增加通行证
                        _ctx.Store.MarkDirty(PlayerArchive.ActivityFieldNumber);
                        // 任务进度 ConsumeSpecifyItemId（对照 PlayerResourceService.go:96）
                        _ctx.TaskEngine.SetProgress(TaskTargetType.ConsumeSpecifyItemId, (int)Math.Abs(kv.Value), kv.Key, false);
                    }
                    // Go 端 AddItemLog 流水表不迁移
                }
            }
            _ctx.Store.MarkDirty(PlayerArchive.ResourceFieldNumber);
            result.Resource = resource;
            result.Activity = activity;
        }

        // 对照 PlayerResourceService.go:45-70 TxGetResourceById（含体力自动恢复）
        private PlayerResource GetOrCreateResource()
        {
            Tables tables = DataConfigManager.Tables;
            if (_ctx.Store.Archive.Resource == null)
            {
                _ctx.Store.Archive.Resource = new PlayerResource
                {
                    Id = _ctx.UserId,
                    Stamina = tables.TbPlayerConst.MaxStamina,
                    StaminaTime = NowRfc3339(),
                };
            }

            PlayerResource resource = _ctx.Store.Archive.Resource;
            int maxStamina = tables.TbPlayerConst.MaxStamina;
            if (resource.Stamina < maxStamina) // 当前体力小于最大体力时才会自动恢复
            {
                int recoveryTime = (int)tables.TbPlayerConst.StaminaRecoveryTime;
                // 对照 utils/time.go:33-35 GetBeginAndEndDiffSecond(now, staminaTime)
                long diffSecond = DateTimeOffset.Now.ToUnixTimeSeconds()
                    - DateTimeOffset.Parse(resource.StaminaTime, CultureInfo.InvariantCulture).ToUnixTimeSeconds();
                if (diffSecond >= recoveryTime)
                {
                    resource.Stamina += (int)(diffSecond / recoveryTime);
                    if (resource.Stamina > maxStamina)
                    {
                        resource.Stamina = maxStamina;
                    }
                    resource.StaminaTime = NowRfc3339();
                }
            }
            return resource;
        }

        // 对照 activityModel/PlayerActivityModel.go:35-43 TxGetActivity
        // 有意偏差：Go 查不到时返回零值行（Id=0）；本地无存档块则按当前 UserId 建档，
        // 保证客户端 Items.Update 的 Id != 0 校验通过。
        private PlayerActivity GetOrCreateActivity()
        {
            if (_ctx.Store.Archive.Activity == null)
            {
                _ctx.Store.Archive.Activity = new PlayerActivity { Id = _ctx.UserId };
            }
            return _ctx.Store.Archive.Activity;
        }

        // 对照 EquipService/PlayerEquipService.go:14-45 TxUniteAddItemsEntrance_ForEquip（wearId=0 未穿戴）
        private void HandleEquips(Dictionary<int, long> addEquipMap, Google.Protobuf.Collections.RepeatedField<PlayerEquip> resultEquips)
        {
            foreach (var kv in addEquipMap)
            {
                int equipId = kv.Key;
                int equipNum = (int)kv.Value;
                if (equipId <= 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 装备ID不能为0");
                    throw new LocalServerException("装备ID不能为0");
                }

                // 对照 EquipModel/PlayerEquipModel.go:138-172 TxAddEquip
                // 负数消耗装备：Go 的 for i < equipNum 循环不执行，等价无操作，保持一致
                EquipConfig cfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equipId);
                if (cfg == null)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 装备ID[{equipId}]错误");
                    throw new LocalServerException($"装备ID[{equipId}]错误");
                }

                for (int i = 0; i < equipNum; i++)
                {
                    var equip = new PlayerEquip
                    {
                        Id = NextEquipId(),
                        UserId = _ctx.UserId,
                        EquipId = equipId,
                        WearerId = 0,
                        PartType = (int)cfg.Part,
                        Phase = 1,
                    };
                    // 词条生成（EntryService/EntryService.go:13-44 GetRandomEntrys）
                    // Go 端存 JSON 字符串 Entrys/TempEntrys="[]"，proto 即 int 列表/空列表
                    equip.Entrys.AddRange(GetRandomEntrys(cfg.EntryNum, cfg.EntryGroup));
                    _ctx.Store.Archive.Equips.Add(equip);
                    resultEquips.Add(equip);
                }
                // Go 端 AddPlayerAssetLog 流水表不迁移
            }
            _ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);
        }

        // 对照 EntryService/EntryService.go:13-44 GetRandomEntrys（按词条组过滤后洗牌取前 num 个）
        private List<int> GetRandomEntrys(int num, int entryGroup)
        {
            var speEntryList = new List<EquipEntryConfig>();
            foreach (EquipEntryConfig entry in DataConfigManager.Tables.TbEquipEntryConfig.DataList)
            {
                if (entry.EntryGroup == entryGroup)
                {
                    speEntryList.Add(entry);
                }
            }

            if (speEntryList.Count == 0 || num <= 0)
            {
                return new List<int>();
            }
            if (num > speEntryList.Count)
            {
                num = speEntryList.Count;
            }

            // Fisher-Yates 洗牌，对照 Go rand.Shuffle
            for (int i = speEntryList.Count - 1; i > 0; i--)
            {
                int j = _ctx.Rng.Next(i + 1);
                EquipEntryConfig tmp = speEntryList[i];
                speEntryList[i] = speEntryList[j];
                speEntryList[j] = tmp;
            }

            var entrys = new List<int>(num);
            for (int i = 0; i < num; i++)
            {
                entrys.Add(speEntryList[i].Id);
            }
            return entrys;
        }

        // 对照 UserService/UserService.go:26-57 TxUniteAddItemsEntrance_ForExps（仅玩家经验）
        private void HandleExps(Dictionary<int, long> exps, ResultItems result)
        {
            foreach (var kv in exps)
            {
                int itemId = kv.Key;
                long itemNum = kv.Value;
                if (itemNum <= 0)
                {
                    continue;
                }

                // Go 端此处 cfg 为 nil 会解引用 panic，本地 fail fast 同样直接抛
                ItemConfig cfg = DataConfigManager.Tables.TbItemConfig.Get(itemId);
                if (cfg.Type == ItemType.PlayerExp) // 添加用户经验
                {
                    User user = _ctx.Store.User;
                    LogTool.Log($"TxUniteAddItemsEntrance_ForExps userId={_ctx.UserId} 添加经验: {itemNum}");
                    if (AddExp((int)itemNum))
                    {
                        // 对照 TransactionSave（userModel.go:99-106）
                        _ctx.Store.MarkUserDirty();
                    }
                    // 对照 userModel.go:129-134 GetLevelInfo
                    result.UserLevelInfo = new UserLevelInfo { Level = user.Level, Exp = user.Exp };
                    // Go 端 AddPlayerAssetLog 流水表不迁移
                }
            }
        }

        // 对照 userModel.go:108-127 AddExp（递归升级；配置缺失或升级经验为 0 时返回 false）
        private bool AddExp(int exp)
        {
            User user = _ctx.Store.User;
            PlayerLevelConfig lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(user.Level);
            if (lvCfg == null || lvCfg.Exp == 0)
            {
                return false;
            }

            if (user.Exp >= lvCfg.Exp)
            {
                user.Exp -= lvCfg.Exp;
                user.Level++;
                AddExp(0);
            }
            else
            {
                if (exp <= 0)
                {
                    return true;
                }
                user.Exp += exp;
                AddExp(0);
            }
            return true;
        }

        // 对照 TaskService/PlayerTaskService.go:135-180 TxUniteAddItemsEntrance_ForTask（仅活跃度发放）
        private void HandleTasks(Dictionary<int, long> taskMap, Google.Protobuf.Collections.MapField<int, ActiveDetail> resultRewards)
        {
            foreach (var kv in taskMap)
            {
                int itemId = kv.Key;
                long itemNum = kv.Value;
                if (itemNum <= 0)
                {
                    continue;
                }

                // Go 端此处 cfg 为 nil 会解引用 panic，本地 fail fast 同样直接抛
                ItemConfig cfg = DataConfigManager.Tables.TbItemConfig.Get(itemId);
                int taskType;
                switch (cfg.Type)
                {
                    case ItemType.DailyActiveNum: // 日常活跃度
                        taskType = (int)TaskType.Daily;
                        break;
                    case ItemType.AchievementNum: // 章节成就
                        taskType = (int)TaskType.ChapterAchievements;
                        break;
                    default:
                        continue;
                }

                // 对照 SetActiveProgress（PlayerTaskService.go:120-133），统一走 TaskEngine（含 TryRefreshDaily 懒刷新）
                ActiveDetail detail = _ctx.TaskEngine.AddActiveProgress((TaskType)taskType, (int)itemNum);
                resultRewards[taskType] = detail;
                // Go 端 AddPlayerAssetLog 流水表不迁移
            }
        }

        // 对照 FusionGemServiceV2/PlayerFusionGemServiceV2.go:50-82 TxUniteAddItemsEntrance_ForFusionGem
        private void HandleFusionStones(Dictionary<int, long> gemsMap, Google.Protobuf.Collections.RepeatedField<PlayerFusionGemToC> resultGems)
        {
            foreach (var kv in gemsMap)
            {
                int gemId = kv.Key;
                long gemNum = kv.Value;
                if (gemNum <= 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 添加融合石数量不能为0");
                    throw new LocalServerException("添加融合石数量不能为0");
                }

                for (long i = 0; i < gemNum; i++)
                {
                    var gem = new PlayerFusionGemToC
                    {
                        Id = NextFusionGemId(),
                        UserId = _ctx.UserId,
                        GemId = gemId,
                        WearerId = 0,
                        Level = 1,
                        Progress = 0,
                    };
                    _ctx.Store.Archive.FusionGemsV2.Add(gem);
                    // 对照 application_player_items.go:96-103：下发端只回填 Id/GemId/Level/Progress
                    resultGems.Add(new PlayerFusionGemToC
                    {
                        Id = gem.Id,
                        GemId = gem.GemId,
                        Level = gem.Level,
                        Progress = gem.Progress,
                    });
                    // Go 端 AddPlayerAssetLog 流水表不迁移
                }
            }
            _ctx.Store.MarkDirty(PlayerArchive.FusionGemsV2FieldNumber);
        }

        // 对照 CycleSpeakerService/cycle_speaker_skin_service.go:50-90 TxUniteAddItemsEntrance_ForSpeakerSkin
        private void HandleSpeakerSkins(Dictionary<int, long> addSkinMap, Google.Protobuf.Collections.RepeatedField<PlayerCycleSpeakerSkin> resultSkins)
        {
            var ownedSkinIds = new HashSet<int>();
            foreach (PlayerCycleSpeakerSkin skin in _ctx.Store.Archive.CycleSpeakerSkins)
            {
                ownedSkinIds.Add(skin.SkinId);
            }

            foreach (var kv in addSkinMap)
            {
                int skinId = kv.Key;
                long skinNum = kv.Value;
                // 检查皮肤是否存在
                PlayerFigureConfig conf = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinId);
                if (conf == null)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 皮肤配置不存在，skinId={skinId}");
                    throw new LocalServerException("皮肤配置不存在");
                }
                // 用户已经拥有这个皮肤
                if (ownedSkinIds.Contains(skinId))
                {
                    continue;
                }
                if (skinNum <= 0 || skinId == 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 皮肤数据错误，skinId={skinId}");
                    throw new LocalServerException("皮肤数据错误");
                }

                for (long i = 0; i < skinNum; i++)
                {
                    var skin = new PlayerCycleSpeakerSkin
                    {
                        Id = NextSpeakerSkinId(),
                        SkinId = skinId,
                        Level = 1,
                    };
                    _ctx.Store.Archive.CycleSpeakerSkins.Add(skin);
                    resultSkins.Add(skin);
                    // Go 端 AddPlayerAssetLog 流水表不迁移
                }
            }
            _ctx.Store.MarkDirty(PlayerArchive.CycleSpeakerSkinsFieldNumber);
        }

        // 对照 AvatarService/avatar_service.go:31-69 TxUniteAddItemsEntrance_ForAvatar
        private void HandleAvatars(Dictionary<int, long> avatarMap, Google.Protobuf.Collections.RepeatedField<PlayerAvatar> resultAvatars)
        {
            var ownedAvatarIds = new HashSet<int>();
            foreach (PlayerAvatar avatar in _ctx.Store.Archive.Avatars)
            {
                ownedAvatarIds.Add(avatar.AvatarId);
            }

            foreach (var kv in avatarMap)
            {
                int avatarId = kv.Key;
                long num = kv.Value;
                HeadConfig conf = DataConfigManager.Tables.TbHeadConfig.GetOrDefault(avatarId);
                if (conf == null)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 头像配置不存在，avatarId={avatarId}");
                    throw new LocalServerException("头像配置不存在");
                }
                if (ownedAvatarIds.Contains(avatarId))
                {
                    LogTool.Log($"userId={_ctx.UserId} 头像[{avatarId}]已拥有，跳过");
                    continue;
                }
                if (num <= 0 || avatarId == 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 头像数据错误，avatarId={avatarId}");
                    throw new LocalServerException("头像数据错误");
                }

                for (long i = 0; i < num; i++)
                {
                    var avatar = new PlayerAvatar
                    {
                        Id = NextAvatarId(),
                        AvatarId = avatarId,
                    };
                    _ctx.Store.Archive.Avatars.Add(avatar);
                    resultAvatars.Add(avatar);
                    // Go 端 AddPlayerAssetLog 流水表不迁移
                }
            }
            _ctx.Store.MarkDirty(PlayerArchive.AvatarsFieldNumber);
        }

        // 对照 AvatarService/avatar_service.go:71-109 TxUniteAddItemsEntrance_ForAvatarFrame
        private void HandleAvatarFrames(Dictionary<int, long> frameMap, Google.Protobuf.Collections.RepeatedField<PlayerAvatarFrame> resultFrames)
        {
            var ownedFrameIds = new HashSet<int>();
            foreach (PlayerAvatarFrame frame in _ctx.Store.Archive.AvatarFrames)
            {
                ownedFrameIds.Add(frame.AvatarFrameId);
            }

            foreach (var kv in frameMap)
            {
                int frameId = kv.Key;
                long num = kv.Value;
                HeadConfig conf = DataConfigManager.Tables.TbHeadConfig.GetOrDefault(frameId);
                if (conf == null)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 头像框配置不存在，frameId={frameId}");
                    throw new LocalServerException("头像框配置不存在");
                }
                if (ownedFrameIds.Contains(frameId))
                {
                    LogTool.Log($"userId={_ctx.UserId} 头像框[{frameId}]已拥有，跳过");
                    continue;
                }
                if (num <= 0 || frameId == 0)
                {
                    LogTool.LogWarning($"userId={_ctx.UserId} 头像框数据错误，frameId={frameId}");
                    throw new LocalServerException("头像框数据错误");
                }

                for (long i = 0; i < num; i++)
                {
                    var frame = new PlayerAvatarFrame
                    {
                        Id = NextAvatarFrameId(),
                        AvatarFrameId = frameId,
                    };
                    _ctx.Store.Archive.AvatarFrames.Add(frame);
                    resultFrames.Add(frame);
                    // Go 端 AddPlayerAssetLog 流水表不迁移
                }
            }
            _ctx.Store.MarkDirty(PlayerArchive.AvatarFramesFieldNumber);
        }

        // 本地替代 Go DB 自增主键：取现有最大 Id + 1
        private int NextItemId()
        {
            int max = 0;
            foreach (PlayerItem item in _ctx.Store.Archive.Items)
            {
                if (item.Id > max)
                {
                    max = item.Id;
                }
            }
            return max + 1;
        }

        private int NextCardId()
        {
            int max = 0;
            foreach (PlayerCard card in _ctx.Store.Archive.Cards)
            {
                if (card.Id > max)
                {
                    max = card.Id;
                }
            }
            return max + 1;
        }

        private int NextEquipId()
        {
            int max = 0;
            foreach (PlayerEquip equip in _ctx.Store.Archive.Equips)
            {
                if (equip.Id > max)
                {
                    max = equip.Id;
                }
            }
            return max + 1;
        }

        private int NextFusionGemId()
        {
            int max = 0;
            foreach (PlayerFusionGemToC gem in _ctx.Store.Archive.FusionGemsV2)
            {
                if (gem.Id > max)
                {
                    max = gem.Id;
                }
            }
            return max + 1;
        }

        private int NextSpeakerSkinId()
        {
            int max = 0;
            foreach (PlayerCycleSpeakerSkin skin in _ctx.Store.Archive.CycleSpeakerSkins)
            {
                if (skin.Id > max)
                {
                    max = skin.Id;
                }
            }
            return max + 1;
        }

        private int NextAvatarId()
        {
            int max = 0;
            foreach (PlayerAvatar avatar in _ctx.Store.Archive.Avatars)
            {
                if (avatar.Id > max)
                {
                    max = avatar.Id;
                }
            }
            return max + 1;
        }

        private int NextAvatarFrameId()
        {
            int max = 0;
            foreach (PlayerAvatarFrame frame in _ctx.Store.Archive.AvatarFrames)
            {
                if (frame.Id > max)
                {
                    max = frame.Id;
                }
            }
            return max + 1;
        }

        // 对照 chest_converter.go:287-292 formatTime（RFC3339）
        private string NowRfc3339()
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        }

        // map 累加，对照 Go result[key] += value 语义
        private void Accumulate(Dictionary<int, long> map, int key, long delta)
        {
            map.TryGetValue(key, out long cur);
            map[key] = cur + delta;
        }

        // 对照 PlayerItemService.go:27-38 ItemGroup
        private class ItemGroup
        {
            public readonly Dictionary<int, long> Exps = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Items = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Cards = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Tasks = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Equips = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Resource = new Dictionary<int, long>();
            public readonly Dictionary<int, long> FusionStones = new Dictionary<int, long>();
            public readonly Dictionary<int, long> SpeakerSkins = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Avatars = new Dictionary<int, long>();
            public readonly Dictionary<int, long> AvatarFrames = new Dictionary<int, long>();
        }
    }
}
